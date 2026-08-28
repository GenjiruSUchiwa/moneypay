import DesignSystem
import SwiftUI
import WalletStore

public struct ConvertView: View {
    public init() {
    }

    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    @State private var digits = ""
    @State private var done = false

    private var xaf: Int { Int(digits) ?? 0 }
    private var usdCents: Int {
        Int(Double(xaf) / (store.fx.usdToXAF * (1 + store.fx.marginPct)) * 100)
    }
    private var marketCents: Int { Int(Double(xaf) / store.fx.usdToXAF * 100) }
    private var marginXAF: Int { Int(Double(xaf) * store.fx.marginPct / (1 + store.fx.marginPct)) }
    private var valid: Bool { xaf >= 1_000 && xaf <= store.balanceXAF }

    public var body: some View {
        NavigationStack { if done { success } else { form } }
    }

    private var form: some View {
        VStack(spacing: 0) {
            NavBar(title: Text("Convert", bundle: .module), onClose: { dismiss() })

            VStack(spacing: 0) {
                leg(flag: "🇨🇲", code: "FCFA",
                    note: Text("Available: \(Fmt.xaf(store.balanceXAF))", bundle: .module),
                    value: digits.isEmpty ? "0" : Fmt.group(xaf), active: true)
                ZStack {
                    Rule()
                    Image(systemName: "arrow.down")
                        .font(.system(size: 11, weight: .bold))
                        .foregroundStyle(Brand.onInk)
                        .frame(width: 26, height: 26)
                        .background(Brand.inkFill, in: .circle)
                }
                leg(flag: "🇺🇸", code: "USD",
                    note: Text("Spendable on all your cards", bundle: .module),
                    value: Fmt.usd(usdCents, symbol: false), active: false)
            }
            .gutter()

            Rule()

            // The margin is shown, not buried in the rate. That is the whole
            // argument against the apps that hide it.
            VStack(spacing: 0) {
                kv(Text("Interbank rate", bundle: .module),
                   Text(verbatim: "1 USD = \(Fmt.xaf(Int(store.fx.usdToXAF)))"))
                Rule()
                kv(Text("MoneyPay margin · \(store.fx.marginPct, format: .percent)", bundle: .module),
                   marginXAF > 0 ? Text(verbatim: "− \(Fmt.xaf(marginXAF))") : Text(verbatim: "—"),
                   tint: Brand.pending)
                Rule()
                kv(Text("You receive", bundle: .module), Text(verbatim: Fmt.usd(usdCents)), strong: true)
            }
            .gutter()
            Rule()

            if marketCents > 0 {
                Text("At the raw interbank rate you would get \(Fmt.usd(marketCents)).", bundle: .module)
                    .font(.micro).foregroundStyle(Brand.inkFaint)
                    .gutter().padding(.top, 12)
            }

            Spacer()

            Keypad(onDigit: { d in if digits.count < 8 { digits.append("\(d)") } },
                   onDelete: { if !digits.isEmpty { digits.removeLast() } })
                .gutter()

            MPButton(title: Text(xaf > store.balanceXAF ? "Insufficient balance" : "Convert",
                                 bundle: .module),
                     enabled: valid) {
                Haptic.success()
                withAnimation(.easeOut(duration: 0.25)) { done = true }
            }
            .gutter().padding(.bottom, 10)
        }
        .page()
    }

    private func leg(flag: String, code: String, note: Text, value: String, active: Bool) -> some View {
        HStack(spacing: 12) {
            Text(verbatim: flag).font(.system(size: 26))
            VStack(alignment: .leading, spacing: 2) {
                Text(verbatim: code).font(.bodyMed).foregroundStyle(Brand.ink)
                note.font(.micro).foregroundStyle(Brand.inkMuted).lineLimit(1)
            }
            Spacer(minLength: 8)
            Text(verbatim: value)
                .font(.system(size: 26, weight: .semibold))
                .monospacedDigit()
                .foregroundStyle(active ? Brand.ink : Brand.inkMuted)
                .contentTransition(.numericText())
        }
        .padding(.vertical, 18)
    }

    private func kv(_ label: Text, _ value: Text, tint: Color = Brand.ink,
                    strong: Bool = false) -> some View {
        HStack {
            label.font(.bodyReg).foregroundStyle(Brand.inkMuted)
            Spacer()
            value.font(strong ? .bodyMed : .bodyReg).foregroundStyle(tint).monospacedDigit()
        }
        .padding(.vertical, 13)
    }

    private var success: some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer()
            SuccessMark()
            Text("Conversion complete", bundle: .module)
                .font(.system(size: 26, weight: .semibold)).tight(-0.6)
                .foregroundStyle(Brand.ink).padding(.top, 24)
            Text("\(Fmt.xaf(xaf)) converted into \(Fmt.usd(usdCents)).", bundle: .module)
                .font(.bodyReg).foregroundStyle(Brand.inkMuted).padding(.top, 8)
            Spacer()
            MPButton(title: Text("Done", bundle: .module)) { dismiss() }.padding(.bottom, 14)
        }
        .gutter()
        .page()
    }
}

#Preview("Convert — fr") {
    ConvertView()
        .environment(Store())
        .environment(\.locale, Locale(identifier: "fr"))
}
