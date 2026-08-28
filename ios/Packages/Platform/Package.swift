// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "Platform",
    platforms: [
        .iOS(.v26),
        .macOS(.v15),
    ],
    products: [
        .library(name: "Platform", targets: ["Platform"]),
        .library(name: "PlatformTestSupport", targets: ["PlatformTestSupport"]),
    ],
    dependencies: [

    ],
    targets: [
        .target(
            name: "Platform",
            swiftSettings: [
                // Same default isolation as the app target: unannotated code is
                // @MainActor, and what must run off it says so explicitly.
                .defaultIsolation(MainActor.self),
                // Warnings are errors, front and back.
                .treatAllWarnings(as: .error),
            ]
        ),
        .target(
            name: "PlatformTestSupport",
            dependencies: [
                "Platform",
            ],
            swiftSettings: [
                // Same default isolation as the app target: unannotated code is
                // @MainActor, and what must run off it says so explicitly.
                .defaultIsolation(MainActor.self),
                // Warnings are errors, front and back.
                .treatAllWarnings(as: .error),
            ]
        ),
        .testTarget(
            name: "PlatformTests",
            dependencies: [
                "Platform",
                "PlatformTestSupport",
            ],
            swiftSettings: [
                // Same default isolation as the app target: unannotated code is
                // @MainActor, and what must run off it says so explicitly.
                .defaultIsolation(MainActor.self),
                // Warnings are errors, front and back.
                .treatAllWarnings(as: .error),
            ]
        ),
    ]
)
