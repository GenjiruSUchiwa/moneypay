import SwiftUI

public enum Motion {
    public static let quick = Animation.easeOut(duration: 0.2)

    public static let screen = Animation.easeInOut(duration: 0.35)

    public static let settle = Animation.spring(response: 0.4, dampingFraction: 0.7)

    public static let toast = Animation.spring(response: 0.32, dampingFraction: 0.86)

    public static let deck = Animation.spring(response: 0.55, dampingFraction: 0.72)

    public static let float = Animation.easeInOut(duration: 3.2).repeatForever(autoreverses: true)

    public static let floatAmplitude: CGFloat = 4.5

    public static let blur: CGFloat = 5

    public static let stagger: TimeInterval = 0.07

    public static let rise: CGFloat = 8
}

public struct DeckPose: Sendable {
    public let offset: CGSize
    public let scale: CGFloat
    public let rotation: Angle

    public static func at(depth: Int) -> DeckPose {
        switch depth {
        case 1: DeckPose(offset: CGSize(width: 12, height: -32), scale: 0.92, rotation: .degrees(4.5))
        case 2: DeckPose(offset: CGSize(width: -13, height: -58), scale: 0.84, rotation: .degrees(-4))
        default: DeckPose(offset: .zero, scale: 1, rotation: .zero)
        }
    }

    public static let dragTiltPerPoint: CGFloat = 1 / 18
}
