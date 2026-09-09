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
            .sensoryFeedback(.success, trigger: done)
    }

    private var form: some View {
        VStack(spacing: 0) {
            VStack(spacing: 0) {
                leg(flag: "🇨🇲", code: "FCFA",
                    note: Text("Available: \(Fmt.xaf(store.balanceXAF))", bundle: .module)) {
                    AmountEntry(digits: $digits, display: Fmt.group(xaf), currency: "", size: 26)
                }
                ZStack {
                    Rule()
                    Image(systemName: "arrow.down")
                        .font(.system(size: 11, weight: .bold))
                        .foregroundStyle(Brand.onAction)
                        .frame(width: 26, height: 26)
                        .background(Brand.action, in: .circle)
                }
                leg(flag: "🇺🇸", code: "USD",
                    note: Text("Spendable on all your cards", bundle: .module)) {
                    Text(verbatim: Fmt.usd(usdCents, symbol: false))
                        .font(.system(size: 26, weight: .semibold))
                        .monospacedDigit()
                        .foregroundStyle(Brand.inkMuted)
                        .contentTransition(.numericText())
                }
            }
            .gutter()

            Rule()

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

            MPButton(title: Text(xaf > store.balanceXAF ? "Insufficient balance" : "Convert",
                                 bundle: .module),
                     enabled: valid) {
                withAnimation(.easeOut(duration: 0.25)) { done = true }
            }
            .gutter().padding(.bottom, 10)
        }
        .page()
        .navigationTitle(Text("Convert", bundle: .module))
        .toolbarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Button { dismiss() } label: {
                    Label { Text("Close", bundle: .module) } icon: { Image(systemName: "xmark") }
                }
            }
        }
    }

    private func leg(flag: String, code: String, note: Text,
                     @ViewBuilder value: () -> some View) -> some View {
        HStack(spacing: 12) {
            Text(verbatim: flag).font(.system(size: 26))
            VStack(alignment: .leading, spacing: 2) {
                Text(verbatim: code).font(.bodyMed).foregroundStyle(Brand.ink)
                note.font(.micro).foregroundStyle(Brand.inkMuted).lineLimit(1)
            }
            Spacer(minLength: 8)
            value()
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
        .toolbar(.hidden, for: .navigationBar)
    }
}

#Preview("Convert — fr") {
    ConvertView()
        .environment(Store())
        .environment(\.locale, Locale(identifier: "fr"))
}
