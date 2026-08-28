// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "Home",
    defaultLocalization: "en",
    platforms: [
        .iOS(.v26),
    ],
    products: [
        .library(name: "Home", targets: ["Home"]),
    ],
    dependencies: [
        .package(path: "../Cards"),
        .package(path: "../Convert"),
        .package(path: "../DesignSystem"),
        .package(path: "../Money"),
        .package(path: "../Settings"),
        .package(path: "../Transactions"),
        .package(path: "../WalletStore"),
    ],
    targets: [
        .target(
            name: "Home",
            dependencies: [
                .product(name: "Cards", package: "Cards"),
                .product(name: "Convert", package: "Convert"),
                .product(name: "DesignSystem", package: "DesignSystem"),
                .product(name: "Money", package: "Money"),
                .product(name: "Settings", package: "Settings"),
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
            name: "HomeTests",
            dependencies: [
                "Home",
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
