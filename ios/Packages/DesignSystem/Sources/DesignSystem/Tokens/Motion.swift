import SwiftUI

// Motion tokens. `quick`/`screen`/`settle`/`toast` are the working set for
// controls and screens; `deck`/`float` are reserved for the hero object of a
// screen — a card is a physical object and may swing, a control never does.
// Durations stay bounded: anything slower than `.screen` reads as lag,
// anything faster than `.quick` is not perceived as motion.

public enum Motion {
    /// State flips inside a control: toggles, pressed states, chips.
    public static let quick = Animation.easeOut(duration: 0.2)

    /// Screen and phase transitions.
    public static let screen = Animation.easeInOut(duration: 0.35)

    /// Confirmation of a value that changed: a balance, a success mark.
    public static let settle = Animation.spring(response: 0.4, dampingFraction: 0.7)

    /// A toast arriving or leaving.
    public static let toast = Animation.spring(response: 0.32, dampingFraction: 0.86)

    /// A card taking its place in a deck. The only spring outside money
    /// movement: a card is a physical object, not a value.
    public static let deck = Animation.spring(response: 0.55, dampingFraction: 0.72)

    /// Idle drift of a hero object, e.g. the welcome deck. Ambient only;
    /// never on a control.
    public static let float = Animation.easeInOut(duration: 3.2).repeatForever(autoreverses: true)

    /// Vertical drift amplitude of `float`.
    public static let floatAmplitude: CGFloat = 4.5

    /// Blur applied to text leaving or entering during a cross-fade.
    public static let blur: CGFloat = 5

    /// Delay between two elements of a cascaded entrance.
    public static let stagger: TimeInterval = 0.07

    /// Vertical travel of content entering a screen; zero when Reduce Motion is on.
    public static let rise: CGFloat = 8
}

/// Resting pose of a card in a deck, by depth (prototype `.we-card[data-depth]`).
/// Depth 0 is the front card; deeper cards are smaller, higher and fanned out.
public struct DeckPose: Sendable {
    public let offset: CGSize
    public let scale: CGFloat
    public let rotation: Angle

    /// The pose for `depth`; anything past the last fanned card sits at the front pose.
    public static func at(depth: Int) -> DeckPose {
        switch depth {
        case 1: DeckPose(offset: CGSize(width: 12, height: -32), scale: 0.92, rotation: .degrees(4.5))
        case 2: DeckPose(offset: CGSize(width: -13, height: -58), scale: 0.84, rotation: .degrees(-4))
        default: DeckPose(offset: .zero, scale: 1, rotation: .zero)
        }
    }

    /// Degrees a dragged front card tilts per point of horizontal travel (prototype `dx / 18`).
    public static let dragTiltPerPoint: CGFloat = 1 / 18
}
