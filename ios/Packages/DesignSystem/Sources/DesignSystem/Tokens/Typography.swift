import SwiftUI

public extension Font {
    static func display(_ size: CGFloat) -> Font { .system(size: size, weight: .semibold) }

    static let heading1 = Font.system(size: 28, weight: .semibold)
    static let titleLarge = Font.system(size: 24, weight: .semibold)
    static let heading2 = Font.system(size: 20, weight: .semibold)
    static let input = Font.system(size: 20, weight: .medium)
    static let heading3 = Font.system(size: 17, weight: .semibold)
    static let bodyReg = Font.system(size: 16, weight: .regular)
    static let bodyMed = Font.system(size: 16, weight: .medium)
    static let sub = Font.system(size: 14, weight: .regular)
    static let subMed = Font.system(size: 14, weight: .medium)
    static let micro = Font.system(size: 12, weight: .regular)
    static let microMed = Font.system(size: 12, weight: .medium)

    /// Eyebrow: capitals are allowed only in monospace, with widened
    /// tracking. That is the type rule, and it reads like a bank statement.
    static let eyebrow = Font.system(size: 11, weight: .medium, design: .monospaced)
    /// Raw data: PAN, references, codes.
    static let dataMono = Font.system(size: 15, weight: .regular, design: .monospaced)
}

public extension View {
    /// Titles above 20 pt: tightened tracking.
    func tight(_ amount: CGFloat = -0.4) -> some View { tracking(amount) }
}
