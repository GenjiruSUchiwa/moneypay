// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "WalletStore",
    defaultLocalization: "en",
    platforms: [
        .iOS(.v26),
    ],
    products: [
        .library(name: "WalletStore", targets: ["WalletStore"]),
    ],
    dependencies: [
        .package(path: "../DesignSystem"),
        .package(path: "../Money"),
    ],
    targets: [
        .target(
            name: "WalletStore",
            dependencies: [
                .product(name: "DesignSystem", package: "DesignSystem"),
                .product(name: "Money", package: "Money"),
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
