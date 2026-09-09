import SwiftUI

public extension Font {
    static func sans(_ size: CGFloat, weight: Weight = .regular) -> Font {
        _ = SansFace.registration
        return .custom(SansFace.postScriptName(for: weight), size: size)
    }

    static func display(_ size: CGFloat) -> Font { .sans(size, weight: .semibold) }

    static let heading1 = Font.sans(28, weight: .semibold)
    static let titleLarge = Font.sans(24, weight: .semibold)
    static let heading2 = Font.sans(20, weight: .semibold)
    static let input = Font.sans(20, weight: .medium)
    static let heading3 = Font.sans(17, weight: .semibold)
    static let bodyReg = Font.sans(16)
    static let bodyMed = Font.sans(16, weight: .medium)
    static let sub = Font.sans(14)
    static let subMed = Font.sans(14, weight: .medium)
    static let micro = Font.sans(12)
    static let microMed = Font.sans(12, weight: .medium)

    static let eyebrow = Font.sans(13, weight: .medium)
    static let dataMono = Font.system(size: 15, weight: .regular, design: .monospaced)
}

public extension View {
    func tight(_ amount: CGFloat = -0.4) -> some View { tracking(amount) }
}
