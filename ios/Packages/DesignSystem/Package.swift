// swift-tools-version: 6.2
import PackageDescription

let package = Package(
    name: "DesignSystem",
    platforms: [
        .iOS(.v26),
    ],
    products: [
        .library(name: "DesignSystem", targets: ["DesignSystem"]),
    ],
    dependencies: [

    ],
    targets: [
        .target(
            name: "DesignSystem",
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
