#!/usr/bin/env python3
"""Regenerates every ios/Packages/*/Package.swift from the imports its sources
actually use. Run it after moving files between packages."""
import os, re, glob

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PKGS = os.path.join(ROOT, 'Packages')
ALL = sorted(d for d in os.listdir(PKGS) if os.path.isdir(os.path.join(PKGS, d)))
# Packages/<P>/Sources/<T> may vend more than one product (Platform + TestSupport).
PRODUCT_OWNER = {}
for p in ALL:
    for t in sorted(os.listdir(os.path.join(PKGS, p, 'Sources'))):
        PRODUCT_OWNER[t] = p

# Every target carries the same two settings, because ios/project.yml does NOT
# reach them: Xcode builds each local package as its own project and project
# settings do not cross that boundary. Without these lines the app target would
# be strict and the packages, which hold nearly all the code, would not be.
SWIFT_SETTINGS = """,
            swiftSettings: [
                // Same default isolation as the app target: unannotated code is
                // @MainActor, and what must run off it says so explicitly.
                .defaultIsolation(MainActor.self),
                // Warnings are errors, front and back.
                .treatAllWarnings(as: .error),
            ]"""
TEMPLATE = '''// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "{name}",{localization}
    platforms: [
{platforms}
    ],
    products: [
{products}
    ],
    dependencies: [
{deps}
    ],
    targets: [
{targets}
    ]
)
'''

def imports(paths):
    found = set()
    for path in paths:
        for line in open(path):
            m = re.match(r'^(?:@testable )?import (\w+)$', line)
            if m and m.group(1) in PRODUCT_OWNER:
                found.add(m.group(1))
    return found

def has_resources(pkg, target):
    return os.path.isdir(os.path.join(PKGS, pkg, 'Sources', target, 'Resources'))


def sources(pkg, target):
    base = os.path.join(PKGS, pkg, 'Sources', target)
    return glob.glob(base + '/**/*.swift', recursive=True)

def tests(pkg, target):
    base = os.path.join(PKGS, pkg, 'Tests', target)
    return glob.glob(base + '/**/*.swift', recursive=True)

for pkg in ALL:
    targets = sorted(os.listdir(os.path.join(PKGS, pkg, 'Sources')))
    test_dir = os.path.join(PKGS, pkg, 'Tests')
    test_targets = sorted(t for t in (os.listdir(test_dir) if os.path.isdir(test_dir) else [])
                          if tests(pkg, t))
    used = set()
    for t in targets:
        used |= imports(sources(pkg, t))
    for t in test_targets:
        used |= imports(tests(pkg, t))
    dep_pkgs = sorted({PRODUCT_OWNER[p] for p in used} - {pkg})

    products = '\n'.join(f'        .library(name: "{t}", targets: ["{t}"]),' for t in targets)
    deps = '\n'.join(f'        .package(path: "../{d}"),' for d in dep_pkgs)

    blocks = []
    for t in targets:
        tdeps = sorted(used & set(PRODUCT_OWNER) - {t})
        tdeps = [d for d in imports(sources(pkg, t)) if d != t]
        lines = ''.join(f'\n                .product(name: "{d}", package: "{PRODUCT_OWNER[d]}"),'
                        if PRODUCT_OWNER[d] != pkg else f'\n                "{d}",'
                        for d in sorted(tdeps))
        # A target that owns UI copy ships its own Localizable.xcstrings;
        # `defaultLocalization` below is what makes Bundle.module resolve it.
        res = ',\n            resources: [\n                .process("Resources"),\n            ]' \
            if has_resources(pkg, t) else ''
        blocks.append(f'        .target(\n            name: "{t}"'
                      + (f',\n            dependencies: [{lines}\n            ]' if tdeps else '')
                      + res + SWIFT_SETTINGS + '\n        ),')
    for t in test_targets:
        tdeps = [d for d in imports(tests(pkg, t)) if d != t]
        lines = ''.join(f'\n                .product(name: "{d}", package: "{PRODUCT_OWNER[d]}"),'
                        if PRODUCT_OWNER[d] != pkg else f'\n                "{d}",'
                        for d in sorted(tdeps))
        blocks.append(f'        .testTarget(\n            name: "{t}"'
                      + (f',\n            dependencies: [{lines}\n            ]' if tdeps else '')
                      + SWIFT_SETTINGS + '\n        ),')

    # UI-free packages also build on macOS, so `swift test` runs them without
    # a simulator, which gives a fast local test loop.
    all_src = [f for t in targets for f in sources(pkg, t)]
    ui = any('import SwiftUI' in open(f).read() or 'import UIKit' in open(f).read()
             for f in all_src)
    platforms = '        .iOS(.v26),' + ('' if ui else '\n        .macOS(.v15),')
    # English source keys, French as a translation: every catalog-bearing
    # package declares the same development language as ios/project.yml.
    localization = ('\n    defaultLocalization: "en",'
                    if any(has_resources(pkg, t) for t in targets) else '')
    out = TEMPLATE.format(name=pkg, products=products, deps=deps, localization=localization,
                          platforms=platforms, targets='\n'.join(blocks))
    open(os.path.join(PKGS, pkg, 'Package.swift'), 'w').write(out)
    print(f'{pkg:14s} deps: {" ".join(dep_pkgs) or "-"}')
