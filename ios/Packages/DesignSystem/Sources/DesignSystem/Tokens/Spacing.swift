import SwiftUI

public enum Metric {
    /// The single horizontal margin used across the whole app.
    public static let gutter: CGFloat = 20
    /// Breathing room between sections. Generous: it is what replaces cards.
    public static let section: CGFloat = 34
    public static let rowVertical: CGFloat = 14
    /// `{spacing.sm}`: the gap under a navbar or between a bar and its content.
    public static let small: CGFloat = 12
    /// `{spacing.lg}`: the gap between a progress bar and the title it precedes.
    public static let large: CGFloat = 24
    /// Widest comfortable line of body copy (prototype `.we-body max-width`).
    public static let measure: CGFloat = 320
    /// Height of a segmented progress bar (prototype `.we-segs`).
    public static let progressHeight: CGFloat = 3
    /// Width of the welcome card deck (prototype `.we-stage`).
    public static let deckWidth: CGFloat = 296
    /// Horizontal gap between two segments of a progress bar (prototype `gap: 5px`).
    public static let progressGap: CGFloat = 5
    /// Vertical gap between two stacked buttons (prototype `margin-top: 10px`).
    public static let stack: CGFloat = 10
}
