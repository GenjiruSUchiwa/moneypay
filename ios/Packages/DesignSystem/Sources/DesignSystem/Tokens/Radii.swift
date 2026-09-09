import SwiftUI

public extension Metric {
    static let card: CGFloat = 18

    static let control: CGFloat = 13

    static func inner(_ outer: CGFloat, padding: CGFloat) -> CGFloat {
        max(4, outer - padding)
    }
}
