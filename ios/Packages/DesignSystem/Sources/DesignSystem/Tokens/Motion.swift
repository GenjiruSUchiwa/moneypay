import SwiftUI

// Motion tokens. Three durations, no more: anything slower than `.screen`
// reads as lag, anything faster than `.quick` is not perceived as motion.
// Money moving is the only place this app animates with a spring.

public enum Motion {
    /// State flips inside a control: toggles, pressed states, chips.
    public static let quick = Animation.easeOut(duration: 0.2)

    /// Screen and phase transitions.
    public static let screen = Animation.easeInOut(duration: 0.35)

    /// Confirmation of a value that changed: a balance, a success mark.
    public static let settle = Animation.spring(response: 0.4, dampingFraction: 0.7)

    /// A toast arriving or leaving.
    public static let toast = Animation.spring(response: 0.32, dampingFraction: 0.86)
}
