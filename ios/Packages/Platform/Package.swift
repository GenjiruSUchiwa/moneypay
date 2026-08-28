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
                .defaultIsolation(MainActor.self),
                .treatAllWarnings(as: .error),
            ]
        ),
        .target(
            name: "PlatformTestSupport",
            dependencies: [
                "Platform",
            ],
            swiftSettings: [
                .defaultIsolation(MainActor.self),
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
                .defaultIsolation(MainActor.self),
                .treatAllWarnings(as: .error),
            ]
        ),
    ]
)
