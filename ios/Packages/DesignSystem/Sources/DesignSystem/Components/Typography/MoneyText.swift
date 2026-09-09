import SwiftUI

public struct MoneyText: View {
    public init(
        whole: String,
        frac: String? = nil,
        prefix: String? = nil,
        suffix: String? = nil,
        size: CGFloat = 34,
        weight: Font.Weight = .semibold,
        color: Color = Brand.ink
    ) {
        self.whole = whole
        self.frac = frac
        self.prefix = prefix
        self.suffix = suffix
        self.size = size
        self.weight = weight
        self.color = color
    }

    public var whole: String
    public var frac: String? = nil
    public var prefix: String? = nil
    public var suffix: String? = nil
    public var size: CGFloat = 34
    public var weight: Font.Weight = .semibold
    public var color: Color = Brand.ink

    public static func usd(_ cents: Int, size: CGFloat = 34, weight: Font.Weight = .semibold,
                           color: Color = Brand.ink, signed: Bool = false) -> MoneyText {
        let p = Fmt.parts(usdCents: cents)
        let sign = signed && cents > 0 ? "+" : (cents < 0 ? "−" : "")
        return MoneyText(whole: p.0, frac: p.1, prefix: "\(sign)$",
                         size: size, weight: weight, color: color)
    }

    public static func xaf(_ amount: Int, size: CGFloat = 34, weight: Font.Weight = .semibold,
                           color: Color = Brand.ink, signed: Bool = false,
                           unit: Bool = true) -> MoneyText {
        let sign = signed && amount > 0 ? "+" : (amount < 0 ? "−" : "")
        return MoneyText(whole: sign + Fmt.group(amount), suffix: unit ? "FCFA" : nil,
                         size: size, weight: weight, color: color)
    }

    public var body: some View {
        HStack(alignment: .firstTextBaseline, spacing: 0) {
            if let prefix {
                Text(verbatim: prefix).font(.system(size: size * 0.72, weight: weight))
            }
            Text(verbatim: whole)
                .font(.system(size: size, weight: weight))
                .tracking(size > 24 ? -0.8 : 0)
            if let frac {
                Text(verbatim: frac)
                    .font(.system(size: size * 0.52, weight: weight))
                    .baselineOffset(size * 0.34)
            }
            if let suffix {
                Text(verbatim: suffix)
                    .font(.system(size: max(11, size * 0.42), weight: .medium))
                    .foregroundStyle(Brand.inkMuted)
                    .padding(.leading, max(3, size * 0.09))
            }
        }
        .monospacedDigit()
        .foregroundStyle(color)
        .contentTransition(.numericText())
    }
}

#Preview("MoneyText") {
    VStack(alignment: .leading, spacing: 20) {
        MoneyText.xaf(428_500)
        MoneyText.xaf(-12_400, size: 20, signed: true)
        MoneyText.usd(1_099)
        MoneyText.usd(2_450, size: 20, color: Brand.credit, signed: true)
    }
    .padding()
    .page()
}
