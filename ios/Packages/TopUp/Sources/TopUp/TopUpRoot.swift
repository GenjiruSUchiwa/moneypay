import DesignSystem
import Foundation
import SwiftUI
import WalletStore

public struct TopUpFlow: View {
    public init() {
    }

    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    @State private var digits = ""
    @State private var method = SampleData.methods[0]
    @State private var showMethods = false
    @State private var step: Step = .amount

    private enum Step { case amount, confirm, done }

    private var amount: Int { Int(digits) ?? 0 }
    private var fee: Int { Int(Double(amount) * method.feePct) }
    private var credited: Int { amount - fee }
    private var valid: Bool { amount >= 1_000 }
    private var usdCents: Int {
        Int(Double(credited) / (store.fx.usdToXAF * (1 + store.fx.marginPct)) * 100)
    }

    public var body: some View {
        NavigationStack {
            Group {
                switch step {
                case .amount: amountStep
                case .confirm: confirmStep
                case .done: receipt
                }
            }
            .page()
        }
        .sensoryFeedback(.success, trigger: step) { _, new in new == .done }
    }

    private var amountStep: some View {
        VStack(spacing: 0) {
            Spacer()
            AmountEntry(digits: $digits, display: Fmt.group(amount), currency: "FCFA")
            Group {
                if amount > 0 {
                    Text("≈ \(Fmt.usd(usdCents)) to spend on a card", bundle: .module)
                } else {
                    Text("Minimum \(Fmt.xaf(1_000))", bundle: .module)
                }
            }
                .font(.sub).foregroundStyle(Brand.inkMuted).padding(.top, 10)
            Spacer()

            HStack(spacing: 7) {
                ForEach([10_000, 25_000, 50_000, 100_000], id: \.self) { v in
                    Chip(text: Text(verbatim: Fmt.group(v)), selected: amount == v) { digits = "\(v)" }
                }
            }
            .gutter()

            Rule().padding(.top, 18)
            Button { showMethods = true } label: {
                HStack(spacing: 12) {
                    IconTile(symbol: method.symbol, tint: method.tint)
                    VStack(alignment: .leading, spacing: 2) {
                        Text(verbatim: method.name).font(.bodyReg).foregroundStyle(Brand.ink)
                        Text(verbatim: method.detail).font(.sub).foregroundStyle(Brand.inkMuted)
                    }
                    Spacer()
                    Text("Change", bundle: .module).font(.subMed).foregroundStyle(Brand.mark)
                }
                .padding(.vertical, Metric.rowVertical)
                .contentShape(.rect)
            }
            .buttonStyle(.plain)
            .gutter()
            Rule()

            MPButton(title: Text("Continue", bundle: .module), enabled: valid) {
                withAnimation(.easeOut(duration: 0.22)) { step = .confirm }
            }
            .gutter().padding(.top, Metric.small).padding(.bottom, 10)
        }
        .navigationTitle(Text("Top up", bundle: .module))
        .toolbarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Button { dismiss() } label: {
                    Label { Text("Close", bundle: .module) } icon: { Image(systemName: "xmark") }
                }
            }
        }
        .sheet(isPresented: $showMethods) {
            MethodSheet(selection: $method)
                .presentationDetents([.medium]).presentationBackground(Brand.bg)
        }
    }

    private var confirmStep: some View {
        VStack(spacing: 0) {
            ScrollView {
                VStack(alignment: .leading, spacing: 0) {
                    MoneyText.xaf(amount, size: 36).gutter().padding(.top, 8)
                    Text("from \(method.name)", bundle: .module).font(.sub)
                        .foregroundStyle(Brand.inkMuted)
                        .gutter().padding(.top, 4)

                    Rule().padding(.top, 26)
                    VStack(spacing: 0) {
                        kv("Amount", Text(verbatim: Fmt.xaf(amount)))
                        Rule()
                        kv("Fee",
                           fee == 0 ? Text("Waived", bundle: .module)
                                    : Text(verbatim: "− \(Fmt.xaf(fee))"),
                           tint: fee == 0 ? Brand.credit : Brand.pending)
                        Rule()
                        kv("Credited to the wallet", Text(verbatim: Fmt.xaf(credited)), strong: true)
                        Rule()
                        kv("New balance", Text(verbatim: Fmt.xaf(store.balanceXAF + credited)))
                        Rule()
                        kv("Delay", method.instant ? Text("Immediate", bundle: .module)
                                                   : Text("1 to 2 business days", bundle: .module))
                    }
                    .gutter()
                    Rule()

                    HStack(alignment: .top, spacing: 10) {
                        Image(systemName: "iphone.gen3.radiowaves.left.and.right")
                            .font(.system(size: 13)).foregroundStyle(Brand.inkMuted).padding(.top, 2)
                        Text("You will get a confirmation request on your phone. Approve it with your \(codeName) code.",
                             bundle: .module)
                            .font(.sub).foregroundStyle(Brand.inkMuted)
                            .fixedSize(horizontal: false, vertical: true)
                    }
                    .gutter().padding(.top, 18)
                }
            }
            .scrollIndicators(.hidden)

            MPButton(title: Text("Confirm the top-up", bundle: .module)) {
                store.topUp(xaf: amount, method: method)
                withAnimation(.easeOut(duration: 0.25)) { step = .done }
            }
            .gutter().padding(.bottom, 10)
        }
        .navigationTitle(Text("Confirm", bundle: .module))
        .toolbarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .topBarLeading) {
                Button { withAnimation(Motion.quick) { step = .amount } } label: {
                    Label { Text("Back", bundle: .module) } icon: { Image(systemName: "chevron.backward") }
                }
            }
        }
    }

    private var codeName: String {
        method.id == "mtn" ? "MoMo" : String(localized: "carrier", bundle: .module)
    }

    private func kv(_ label: LocalizedStringKey, _ value: Text,
                    tint: Color = Brand.ink, strong: Bool = false) -> some View {
        HStack {
            Text(label, bundle: .module).font(.bodyReg).foregroundStyle(Brand.inkMuted)
            Spacer()
            value.font(strong ? .bodyMed : .bodyReg).foregroundStyle(tint).monospacedDigit()
        }
        .padding(.vertical, Metric.rowVertical)
    }

    private var receipt: some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer()
            SuccessMark()
            Text("Top-up complete", bundle: .module)
                .font(.system(size: 26, weight: .semibold)).tight(-0.6)
                .foregroundStyle(Brand.ink).padding(.top, 24)
            Text("\(Fmt.xaf(credited)) added to your balance.", bundle: .module)
                .font(.bodyReg).foregroundStyle(Brand.inkMuted).padding(.top, 8)

            Rule().padding(.top, 26)
            kv("New balance", Text(verbatim: Fmt.xaf(store.balanceXAF)), strong: true)
            Rule()
            kv("Reference", Text(verbatim: "MP-\(Int.random(in: 100_000...999_999))"))
            Rule()
            kv("Date", Text(verbatim: Fmt.fullDate(.now)))
            Rule()

            Spacer()
            VStack(spacing: 9) {
                MPButton(title: Text("Share the receipt", bundle: .module),
                         icon: "square.and.arrow.up", tone: .quiet) {}
                MPButton(title: Text("Done", bundle: .module)) { dismiss() }
            }
            .padding(.bottom, 14)
        }
        .gutter()
        .toolbar(.hidden, for: .navigationBar)
    }
}

#Preview("Top up — fr") {
    TopUpFlow()
        .environment(Store())
        .environment(\.locale, Locale(identifier: "fr"))
}
