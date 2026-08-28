import SwiftUI

public enum CardTheme: String, CaseIterable, Identifiable, Codable, Sendable {
    case ink, bone, pine, clay, slate, cobalt
    public var id: String { rawValue }

    public var fill: Color {
        switch self {
        case .ink:    Color(rgb: 0x15191B)
        case .bone:   Color(rgb: 0xE6E2D8)
        case .pine:   Color(rgb: 0x0E4F41)
        case .clay:   Color(rgb: 0xA34A32)
        case .slate:  Color(rgb: 0x3C4A55)
        case .cobalt: Color(rgb: 0x1C3C86)
        }
    }

    /// Ink that stays legible on this fill.
    public var ink: Color { self == .bone ? Color(rgb: 0x1A1712) : Color(rgb: 0xFAFAF8) }

    public var label: String {
        switch self {
        case .ink: "Encre"; case .bone: "Ivoire"; case .pine: "Pin"
        case .clay: "Terre"; case .slate: "Ardoise"; case .cobalt: "Cobalt"
        }
    }
}

public enum CardNetwork: String, Codable, Sendable { case visa, mastercard }

#Preview("CardTheme swatches") {
    VStack(spacing: 10) {
        ForEach(CardTheme.allCases) { theme in
            HStack(spacing: 12) {
                RoundedRectangle(cornerRadius: 8, style: .continuous)
                    .fill(theme.fill)
                    .frame(width: 56, height: 36)
                Text(theme.rawValue).font(.bodyReg).foregroundStyle(Brand.ink)
                Spacer()
            }
        }
    }
    .gutter()
    .page()
}
