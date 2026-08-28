// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "Transactions",
    defaultLocalization: "en",
    platforms: [
        .iOS(.v26),
    ],
    products: [
        .library(name: "Transactions", targets: ["Transactions"]),
    ],
    dependencies: [
        .package(path: "../DesignSystem"),
        .package(path: "../Money"),
        .package(path: "../WalletStore"),
    ],
    targets: [
        .target(
            name: "Transactions",
            dependencies: [
                .product(name: "DesignSystem", package: "DesignSystem"),
                .product(name: "Money", package: "Money"),
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
            name: "TransactionsTests",
            dependencies: [
                "Transactions",
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
