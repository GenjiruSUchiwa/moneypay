// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "Onboarding",
    defaultLocalization: "en",
    platforms: [
        .iOS(.v26),
    ],
    products: [
        .library(name: "Onboarding", targets: ["Onboarding"]),
    ],
    dependencies: [
        .package(path: "../DesignSystem"),
        .package(path: "../Money"),
    ],
    targets: [
        .target(
            name: "Onboarding",
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
        .testTarget(
            name: "OnboardingTests",
            dependencies: [
                "Onboarding",
            ],
            swiftSettings: [
                .defaultIsolation(MainActor.self),
                .treatAllWarnings(as: .error),
            ]
        ),
    ]
)
