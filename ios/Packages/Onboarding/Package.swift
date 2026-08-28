// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "Onboarding",
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
