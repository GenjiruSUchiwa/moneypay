import SwiftUI

struct TopUpFlow: View {
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

    var body: some View {
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
    }

    // MARK: Montant

    private var amountStep: some View {
        VStack(spacing: 0) {
            NavBar(title: "Recharger", onClose: { dismiss() })

            Spacer()
            AmountEntry(digits: digits.isEmpty ? "" : Fmt.group(amount), currency: "FCFA")
            Text(amount > 0 ? "≈ \(Fmt.usd(usdCents)) dépensables en carte" : "Minimum 1 000 FCFA")
                .font(.sub).foregroundStyle(Brand.inkMuted).padding(.top, 10)
            Spacer()

            HStack(spacing: 7) {
                ForEach([10_000, 25_000, 50_000, 100_000], id: \.self) { v in
                    Chip(text: Fmt.group(v), selected: amount == v) { digits = "\(v)" }
                }
            }
            .gutter()

            Rule().padding(.top, 18)
            Button { Haptic.tap(); showMethods = true } label: {
                HStack(spacing: 12) {
                    IconTile(symbol: method.symbol, tint: method.tint)
                    VStack(alignment: .leading, spacing: 2) {
                        Text(method.name).font(.body).foregroundStyle(Brand.ink)
                        Text(method.detail).font(.sub).foregroundStyle(Brand.inkMuted)
                    }
                    Spacer()
                    Text("Changer").font(.subMed).foregroundStyle(Brand.mark)
                }
                .padding(.vertical, Metric.rowVertical)
                .contentShape(.rect)
            }
            .buttonStyle(.plain)
            .gutter()
            Rule()

            Keypad(onDigit: { d in if digits.count < 8 { digits.append("\(d)") } },
                   onDelete: { if !digits.isEmpty { digits.removeLast() } })
                .gutter()
                .padding(.top, 6)

            MPButton(title: "Continuer", enabled: valid) {
                withAnimation(.easeOut(duration: 0.22)) { step = .confirm }
            }
            .gutter().padding(.bottom, 10)
        }
        .sheet(isPresented: $showMethods) {
            MethodSheet(selection: $method)
                .presentationDetents([.medium]).presentationBackground(Brand.bg)
        }
    }

    // MARK: Confirmation

    private var confirmStep: some View {
        VStack(spacing: 0) {
            NavBar(title: "Confirmer", onBack: {
                withAnimation(.easeOut(duration: 0.22)) { step = .amount }
            })

            ScrollView {
                VStack(alignment: .leading, spacing: 0) {
                    MoneyText.xaf(amount, size: 36).gutter().padding(.top, 8)
                    Text("depuis \(method.name)").font(.sub).foregroundStyle(Brand.inkMuted)
                        .gutter().padding(.top, 4)

                    Rule().padding(.top, 26)
                    VStack(spacing: 0) {
                        kv("Montant", Fmt.xaf(amount))
                        Rule()
                        kv("Frais", fee == 0 ? "Offerts" : "− \(Fmt.xaf(fee))",
                           tint: fee == 0 ? Brand.credit : Brand.pending)
                        Rule()
                        kv("Crédité sur le wallet", Fmt.xaf(credited), strong: true)
                        Rule()
                        kv("Nouveau solde", Fmt.xaf(store.balanceXAF + credited))
                        Rule()
                        kv("Délai", method.instant ? "Immédiat" : "1 à 2 jours ouvrés")
                    }
                    .gutter()
                    Rule()

                    HStack(alignment: .top, spacing: 10) {
                        Image(systemName: "iphone.gen3.radiowaves.left.and.right")
                            .font(.system(size: 13)).foregroundStyle(Brand.inkMuted).padding(.top, 2)
                        Text("Vous allez recevoir une demande de confirmation sur votre téléphone. Validez-la avec votre code \(method.name.contains("MTN") ? "MoMo" : "opérateur").")
                            .font(.sub).foregroundStyle(Brand.inkMuted)
                            .fixedSize(horizontal: false, vertical: true)
                    }
                    .gutter().padding(.top, 18)
                }
            }
            .scrollIndicators(.hidden)

            MPButton(title: "Confirmer le rechargement") {
                store.topUp(xaf: amount, method: method)
                Haptic.success()
                withAnimation(.easeOut(duration: 0.25)) { step = .done }
            }
            .gutter().padding(.bottom, 10)
        }
    }

    private func kv(_ l: String, _ v: String, tint: Color = Brand.ink, strong: Bool = false) -> some View {
        HStack {
            Text(l).font(.body).foregroundStyle(Brand.inkMuted)
            Spacer()
            Text(v).font(strong ? .bodyMed : .body).foregroundStyle(tint).monospacedDigit()
        }
        .padding(.vertical, Metric.rowVertical)
    }

    // MARK: Reçu

    private var receipt: some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer()
            SuccessMark()
            Text("Rechargement effectué")
                .font(.system(size: 26, weight: .semibold)).tight(-0.6)
                .foregroundStyle(Brand.ink).padding(.top, 24)
            Text("\(Fmt.xaf(credited)) ajoutés à votre solde.")
                .font(.body).foregroundStyle(Brand.inkMuted).padding(.top, 8)

            Rule().padding(.top, 26)
            kv("Nouveau solde", Fmt.xaf(store.balanceXAF), strong: true)
            Rule()
            kv("Référence", "MP-\(Int.random(in: 100000...999999))")
            Rule()
            kv("Date", Fmt.fullDate(.now))
            Rule()

            Spacer()
            VStack(spacing: 9) {
                MPButton(title: "Partager le reçu", icon: "square.and.arrow.up", tone: .quiet) {}
                MPButton(title: "Terminé") { dismiss() }
            }
            .padding(.bottom, 14)
        }
        .gutter()
    }
}

struct MethodSheet: View {
    @Binding var selection: TopUpMethod
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Recharger depuis").font(.title2).tight().foregroundStyle(Brand.ink)
                .gutter().padding(.top, 20).padding(.bottom, 12)
            Rule()
            ScrollView {
                VStack(spacing: 0) {
                    ForEach(Array(SampleData.methods.enumerated()), id: \.element.id) { i, m in
                        Button { Haptic.tap(); selection = m; dismiss() } label: {
                            HStack(spacing: 12) {
                                IconTile(symbol: m.symbol, tint: m.tint)
                                VStack(alignment: .leading, spacing: 3) {
                                    HStack(spacing: 6) {
                                        Text(m.name).font(.body).foregroundStyle(Brand.ink)
                                        if m.feePct == 0 {
                                            StatusPill(text: "Sans frais", symbol: "checkmark",
                                                       tint: Brand.credit, soft: Brand.creditSoft)
                                        }
                                    }
                                    Text(m.detail).font(.sub).foregroundStyle(Brand.inkMuted)
                                }
                                Spacer(minLength: 8)
                                Image(systemName: "checkmark")
                                    .font(.system(size: 13, weight: .bold)).foregroundStyle(Brand.ink)
                                    .opacity(m.id == selection.id ? 1 : 0)
                            }
                            .padding(.vertical, Metric.rowVertical)
                            .contentShape(.rect)
                        }
                        .buttonStyle(.plain)
                        if i < SampleData.methods.count - 1 { Rule(inset: 51) }
                    }
                }
                .gutter()
            }
        }
    }
}

/// Coche de succès. Un disque encre, pas une explosion de cercles concentriques.
struct SuccessMark: View {
    @State private var on = false
    var body: some View {
        Image(systemName: "checkmark")
            .font(.system(size: 26, weight: .semibold))
            .foregroundStyle(Brand.onInk)
            .frame(width: 56, height: 56)
            .background(Brand.credit, in: .circle)
            .scaleEffect(on ? 1 : 0.7)
            .opacity(on ? 1 : 0)
            .task { withAnimation(.spring(response: 0.4, dampingFraction: 0.7)) { on = true } }
    }
}
