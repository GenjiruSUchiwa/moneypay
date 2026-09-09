import SwiftUI

public struct Row<Trailing: View>: View {
    public init(
        icon: String? = nil,
        iconTint: Color = Brand.ink,
        glyph: String? = nil,
        title: Text,
        subtitle: Text? = nil,
        chevron: Bool = false,
        destructive: Bool = false,
        @ViewBuilder trailing: () -> Trailing
    ) {
        self.icon = icon
        self.iconTint = iconTint
        self.glyph = glyph
        self.title = title
        self.subtitle = subtitle
        self.chevron = chevron
        self.destructive = destructive
        self.trailing = trailing()
    }

    public var icon: String? = nil
    public var iconTint: Color = Brand.ink
    public var glyph: String? = nil
    public var title: Text
    public var subtitle: Text? = nil
    public var chevron = false
    public var destructive = false
    @ViewBuilder public var trailing: Trailing

    public var body: some View {
        HStack(spacing: 13) {
            if let glyph {
                Text(glyph).font(.system(size: 20))
                    .frame(width: 38, height: 38)
                    .background(Brand.well, in: .rect(cornerRadius: 11, style: .continuous))
            } else if let icon {
                IconTile(symbol: icon, tint: destructive ? Brand.debit : iconTint)
            }
            VStack(alignment: .leading, spacing: 2) {
                title.font(.bodyReg)
                    .foregroundStyle(destructive ? Brand.debit : Brand.ink)
                if let subtitle {
                    subtitle.font(.sub).foregroundStyle(Brand.inkMuted).lineLimit(1)
                }
            }
            Spacer(minLength: 10)
            trailing
            if chevron {
                Image(systemName: "chevron.forward")
                    .font(.system(size: 13, weight: .semibold))
                    .foregroundStyle(Brand.inkFaint)
            }
        }
        .padding(.vertical, Metric.rowVertical)
        .contentShape(.rect)
    }
}

public extension Row where Trailing == EmptyView {
    init(icon: String? = nil, iconTint: Color = Brand.ink, glyph: String? = nil,
         title: Text, subtitle: Text? = nil, chevron: Bool = false,
         destructive: Bool = false) {
        self.init(icon: icon, iconTint: iconTint, glyph: glyph, title: title,
                  subtitle: subtitle, chevron: chevron, destructive: destructive) { EmptyView() }
    }
}

#Preview("Row") {
    VStack(spacing: 0) {
        Row(icon: "person.text.rectangle", title: Text(verbatim: "Personal details"), chevron: true)
        Rule(inset: 51)
        Row(icon: "gauge.with.dots.needle.50percent",
            title: Text(verbatim: "Limits"),
            subtitle: Text(verbatim: "Monthly cap"),
            chevron: true) { RowValue(text: Text(verbatim: "500 000")) }
        Rule(inset: 51)
        Row(glyph: "🇨🇲", title: Text(verbatim: "Cameroon")) { RowValue(text: Text(verbatim: "+237")) }
        Rule(inset: 51)
        Row(icon: "rectangle.portrait.and.arrow.right", title: Text(verbatim: "Sign out"), destructive: true)
    }
    .gutter()
    .page()
}
