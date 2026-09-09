import SwiftUI

public enum CardTheme: String, CaseIterable, Identifiable, Codable, Sendable {
    case ink, bone, pine, clay, slate, cobalt, ndop
    public var id: String { rawValue }

    public var fill: Color {
        switch self {
        case .ink:    Color(rgb: 0x15181B)
        case .bone:   Color(rgb: 0xE7E9EC)
        case .pine:   Color(rgb: 0x053826)
        case .clay:   Color(rgb: 0x9E4A2E)
        case .slate:  Color(rgb: 0x44525A)
        case .cobalt: Color(rgb: 0x1E3C72)
        case .ndop:   CardArtworkTokens.ndopFill
        }
    }

    public var ink: Color {
        switch self {
        case .ink:    Color(rgb: 0xF2F3F1)
        case .bone:   Color(rgb: 0x171C22)
        case .pine:   Color(rgb: 0xFFFFFF)
        case .clay:   Color(rgb: 0xF7EDE4)
        case .slate:  Color(rgb: 0xEEF2F1)
        case .cobalt: Color(rgb: 0xEAEFF8)
        case .ndop:   CardArtworkTokens.ndopInk
        }
    }

    public var accent: Color? { self == .pine ? Color(rgb: 0x3CDD9B) : nil }

    public var isLight: Bool { self == .bone }

    public var label: String {
        switch self {
        case .ink: "Ink"; case .bone: "Bone"; case .pine: "Pine"
        case .clay: "Clay"; case .slate: "Slate"; case .cobalt: "Cobalt"
        case .ndop: "Ndop"
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
                Text(verbatim: theme.label).font(.bodyReg).foregroundStyle(Brand.ink)
                Spacer()
            }
        }
    }
    .gutter()
    .page()
}
