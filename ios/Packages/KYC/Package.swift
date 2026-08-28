// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "KYC",
    defaultLocalization: "en",
    platforms: [
        .iOS(.v26),
    ],
    products: [
        .library(name: "KYC", targets: ["KYC"]),
    ],
    dependencies: [
        .package(path: "../DesignSystem"),
    ],
    targets: [
        .target(
            name: "KYC",
            dependencies: [
                .product(name: "DesignSystem", package: "DesignSystem"),
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
