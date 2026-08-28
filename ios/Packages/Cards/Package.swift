// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "Cards",
    defaultLocalization: "en",
    platforms: [
        .iOS(.v26),
    ],
    products: [
        .library(name: "Cards", targets: ["Cards"]),
    ],
    dependencies: [
        .package(path: "../DesignSystem"),
        .package(path: "../Money"),
        .package(path: "../Transactions"),
        .package(path: "../WalletStore"),
    ],
    targets: [
        .target(
            name: "Cards",
            dependencies: [
                .product(name: "DesignSystem", package: "DesignSystem"),
                .product(name: "Money", package: "Money"),
                .product(name: "Transactions", package: "Transactions"),
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
            name: "CardsTests",
            dependencies: [
                "Cards",
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
