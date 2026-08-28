#!/usr/bin/env bash
# Generates the project and builds the app for the simulator.
#
# Local workaround: on a machine whose installed simulator runtime is older than
# Xcode's SDK (for example an iOS 26.4 runtime with Xcode 26.6 / SDK 26.5),
# Xcode reports the platform as "not installed" and a project containing local
# SwiftPM packages ends up with NO eligible destination at all:
# `xcodebuild -scheme ... -destination 'platform=iOS Simulator,...'` fails with
# "Unable to find a destination matching the provided destination specifier".
# So we build per target against the simulator SDK with a shared SYMROOT
# (without it each package builds into its own build/ and cannot see the other
# packages' modules). On CI the runtime matches the SDK, so `-scheme` plus
# `-destination` works normally (see .github/workflows/ci.yml).
set -euo pipefail
cd "$(dirname "$0")/.."

xcodegen generate

targets=("$@")
[ ${#targets[@]} -eq 0 ] && targets=(MoniPay MoniPayTests)

for target in "${targets[@]}"; do
    xcodebuild \
        -project MoniPay.xcodeproj \
        -target "$target" \
        -sdk iphonesimulator \
        -configuration Debug \
        ARCHS=arm64 ONLY_ACTIVE_ARCH=NO \
        SYMROOT="$PWD/build/Products" \
        OBJROOT="$PWD/build/Intermediates" \
        build
done
