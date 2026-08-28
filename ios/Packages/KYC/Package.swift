// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "KYC",
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
