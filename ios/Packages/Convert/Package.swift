// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "Convert",
    defaultLocalization: "en",
    platforms: [
        .iOS(.v26),
    ],
    products: [
        .library(name: "Convert", targets: ["Convert"]),
    ],
    dependencies: [
        .package(path: "../DesignSystem"),
        .package(path: "../WalletStore"),
    ],
    targets: [
        .target(
            name: "Convert",
            dependencies: [
                .product(name: "DesignSystem", package: "DesignSystem"),
                .product(name: "WalletStore", package: "WalletStore"),
            ],
            resources: [
                .process("Resources"),
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
            name: "ConvertTests",
            dependencies: [
                "Convert",
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
