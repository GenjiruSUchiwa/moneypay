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

    ],
    targets: [
        .target(
            name: "ApiClient",
            swiftSettings: [
                .defaultIsolation(MainActor.self),
                .treatAllWarnings(as: .error),
            ]
        ),
        .testTarget(
            name: "ApiClientTests",
            dependencies: [
                "ApiClient",
            ],
            resources: [.copy("Fixtures")],
            swiftSettings: [
                .defaultIsolation(MainActor.self),
                .treatAllWarnings(as: .error),
            ]
        ),
    ]
)
