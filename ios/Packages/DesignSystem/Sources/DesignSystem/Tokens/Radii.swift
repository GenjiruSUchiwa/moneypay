import SwiftUI

// Corner radii. Members of `Metric` so call sites read `Metric.card`, declared
// here so the radius scale can be reviewed on its own.

public extension Metric {
    /// Outer radius of a card-like surface.
    static let card: CGFloat = 18

    /// Radius of a control (button, field, chip).
    static let control: CGFloat = 13

    /// Concentric radius: an inner corner is the outer one minus its padding.
    /// Nesting two equal radii is what makes a rounded rectangle look wrong.
    static func inner(_ outer: CGFloat, padding: CGFloat) -> CGFloat {
        max(4, outer - padding)
    }
}
