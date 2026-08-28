// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "Gallery",
    platforms: [
        .iOS(.v26),
    ],
    products: [
        .library(name: "Gallery", targets: ["Gallery"]),
    ],
    dependencies: [
        .package(path: "../Cards"),
        .package(path: "../Convert"),
        .package(path: "../DesignSystem"),
        .package(path: "../Home"),
        .package(path: "../KYC"),
        .package(path: "../Money"),
        .package(path: "../Onboarding"),
        .package(path: "../Settings"),
        .package(path: "../TopUp"),
        .package(path: "../Transactions"),
        .package(path: "../WalletStore"),
    ],
    targets: [
        .target(
            name: "Gallery",
            dependencies: [
                .product(name: "Cards", package: "Cards"),
                .product(name: "Convert", package: "Convert"),
                .product(name: "DesignSystem", package: "DesignSystem"),
                .product(name: "Home", package: "Home"),
                .product(name: "KYC", package: "KYC"),
                .product(name: "Money", package: "Money"),
                .product(name: "Onboarding", package: "Onboarding"),
                .product(name: "Settings", package: "Settings"),
                .product(name: "TopUp", package: "TopUp"),
                .product(name: "Transactions", package: "Transactions"),
                .product(name: "WalletStore", package: "WalletStore"),
            ],
            swiftSettings: [
                .defaultIsolation(MainActor.self),
                .treatAllWarnings(as: .error),
            ]
        ),
    ]
)
