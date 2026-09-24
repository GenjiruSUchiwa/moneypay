// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "ApiClient",
    platforms: [
        .iOS(.v26),
        .macOS(.v15),
    ],
    products: [
        .library(name: "ApiClient", targets: ["ApiClient"]),
    ],
    dependencies: [
        .package(path: "../Platform"),
    ],
    targets: [
        .target(
            name: "ApiClient",
            dependencies: [
                .product(name: "Platform", package: "Platform"),
            ],
            swiftSettings: [
                .defaultIsolation(MainActor.self),
                .treatAllWarnings(as: .error),
            ]
        ),
        .testTarget(
            name: "ApiClientTests",
            dependencies: [
                "ApiClient",
                .product(name: "PlatformTestSupport", package: "Platform"),
            ],
            resources: [.copy("Fixtures")],
            swiftSettings: [
                .defaultIsolation(MainActor.self),
                .treatAllWarnings(as: .error),
            ]
        ),
    ]
)
