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
    public static let float = Animation.easeInOut(duration: 1.6).repeatForever(autoreverses: true)

    /// Vertical drift amplitude of `float`.
    public static let floatAmplitude: CGFloat = 4.5

    /// Blur applied to text leaving or entering during a cross-fade.
    public static let blur: CGFloat = 5

    /// Delay between two elements of a cascaded entrance.
    public static let stagger: TimeInterval = 0.07

    /// Vertical travel of content entering a screen; zero when Reduce Motion is on.
    public static let rise: CGFloat = 8
}
