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
                .defaultIsolation(MainActor.self),
                .treatAllWarnings(as: .error),
            ]
        ),
    ]
)
