import SwiftUI

// Light/dark resolution. Every colour token is declared as a light/dark pair
// and resolved per trait collection, so a token is never a single hex that a
// dark screen then has to fight.

public extension Color {
    static func adaptive(light: UInt32, dark: UInt32) -> Color {
        Color(uiColor: UIColor { $0.userInterfaceStyle == .dark ? UIColor(rgb: dark) : UIColor(rgb: light) })
    }
    init(rgb: UInt32) { self = Color(uiColor: UIColor(rgb: rgb)) }
}

public extension UIColor {
    convenience init(rgb: UInt32) {
        self.init(red: Double((rgb >> 16) & 0xFF) / 255,
                  green: Double((rgb >> 8) & 0xFF) / 255,
                  blue: Double(rgb & 0xFF) / 255, alpha: 1)
    }
}
