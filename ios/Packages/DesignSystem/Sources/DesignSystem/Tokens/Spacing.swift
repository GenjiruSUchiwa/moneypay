import SwiftUI

public enum Metric {
    /// The single horizontal margin used across the whole app.
    public static let gutter: CGFloat = 20
    /// Breathing room between sections. Generous: it is what replaces cards.
    public static let section: CGFloat = 34
    public static let rowVertical: CGFloat = 14
    /// Height of a segmented progress bar (prototype `.we-segs`).
    public static let progressHeight: CGFloat = 3
    /// Horizontal gap between two segments of a progress bar (prototype `gap: 5px`).
    public static let progressGap: CGFloat = 5
    /// Vertical gap between two stacked buttons (prototype `margin-top: 10px`).
    public static let stack: CGFloat = 10
}
