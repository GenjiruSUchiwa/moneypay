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
                // Same default isolation as the app target: unannotated code is
                // @MainActor, and what must run off it says so explicitly.
                .defaultIsolation(MainActor.self),
                // Warnings are errors, front and back.
                .treatAllWarnings(as: .error),
            ]
        ),
        .testTarget(
            name: "ApiClientTests",
            dependencies: [
                "ApiClient",
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
