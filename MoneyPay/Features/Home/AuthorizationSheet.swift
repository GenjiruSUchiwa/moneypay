import SwiftUI

/// Le moment clé du montage : le processeur demande par webhook si on approuve
/// un paiement, et on a quelques secondes pour répondre. Voici cette décision
/// vue par l'utilisateur.
struct AuthorizationSheet: View {
    var merchant: String
    var category: TxCategory
    var amountUSDCents: Int
    var card: VirtualCard

    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    @State private var remaining = 20.0
    @State private var outcome: Outcome?

    private enum Outcome { case approved, declined, expired }

    private var amountXAF: Int { store.fx.xaf(fromUSDCents: amountUSDCents) }
    private var after: Int { store.balanceXAF - amountXAF }
    private var enough: Bool { after >= 0 }

    var body: some View {
        Group { if let outcome { result(outcome) } else { request } }
            .frame(maxWidth: .infinity, maxHeight: .infinity)
            .page()
    }

    private var request: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack(spacing: 7) {
                Circle().fill(Brand.pending).frame(width: 6, height: 6)
                Eyebrow(text: "Autorisation en attente")
                Spacer()
                Text("\(Int(remaining.rounded())) s")
                    .font(.eyebrow).monospacedDigit()
                    .foregroundStyle(remaining < 6 ? Brand.debit : Brand.inkMuted)
            }
            .padding(.top, 22)

            // Compte à rebours : un trait qui se vide, pas un anneau décoratif.
            GeometryReader { geo in
                ZStack(alignment: .leading) {
                    Rectangle().fill(Brand.hairline)
                    Rectangle().fill(remaining < 6 ? Brand.debit : Brand.ink)
                        .frame(width: geo.size.width * (remaining / 20))
                }
            }
            .frame(height: 2)
            .padding(.top, 10)

            IconTile(symbol: category.symbol, tint: category.tint, size: 44).padding(.top, 26)

            Text(merchant).font(.title3).foregroundStyle(Brand.inkMuted).padding(.top, 14)
            MoneyText.usd(amountUSDCents, size: 40).padding(.top, 2)
            Text("soit \(Fmt.xaf(amountXAF)) au taux du jour")
                .font(.sub).foregroundStyle(Brand.inkMuted).padding(.top, 6)

            Rule().padding(.top, 26)
            kv("Carte", "\(card.label) · •• \(card.last4)")
            Rule()
            kv("Solde actuel", Fmt.xaf(store.balanceXAF))
            Rule()
            kv("Solde après paiement", enough ? Fmt.xaf(after) : "Insuffisant",
               tint: enough ? Brand.ink : Brand.debit)
            Rule()

            Spacer(minLength: 16)

            VStack(spacing: 9) {
                MPButton(title: enough ? "Approuver le paiement" : "Solde insuffisant",
                         enabled: enough) {
                    withAnimation(.easeOut(duration: 0.25)) { outcome = .approved }
                }
                MPButton(title: "Refuser", tone: .danger) {
                    Haptic.warning()
                    withAnimation(.easeOut(duration: 0.25)) { outcome = .declined }
                }
            }

            Text("Un refus est facturé 220 FCFA par le processeur.")
                .font(.micro).foregroundStyle(Brand.inkFaint).padding(.top, 10)
        }
        .gutter()
        .padding(.bottom, 18)
        .task {
            while remaining > 0, outcome == nil, !Task.isCancelled {
                try? await Task.sleep(for: .milliseconds(100))
                remaining -= 0.1
            }
            if outcome == nil {
                withAnimation(.easeOut(duration: 0.25)) { outcome = .expired }
            }
        }
    }

    private func kv(_ l: String, _ v: String, tint: Color = Brand.ink) -> some View {
        HStack {
            Text(l).font(.body).foregroundStyle(Brand.inkMuted)
            Spacer()
            Text(v).font(.body).foregroundStyle(tint).monospacedDigit()
        }
        .padding(.vertical, 13)
    }

    @ViewBuilder
    private func result(_ o: Outcome) -> some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer()
            if o == .approved {
                SuccessMark()
            } else {
                Image(systemName: o == .expired ? "clock" : "xmark")
                    .font(.system(size: 24, weight: .semibold))
                    .foregroundStyle(Brand.onInk)
                    .frame(width: 56, height: 56)
                    .background(Brand.debit, in: .circle)
            }

            Text(o == .approved ? "Paiement approuvé"
                 : o == .expired ? "Autorisation expirée" : "Paiement refusé")
                .font(.system(size: 26, weight: .semibold)).tight(-0.6)
                .foregroundStyle(Brand.ink).padding(.top, 24)

            Text(o == .approved
                 ? "\(Fmt.xaf(amountXAF)) débités de votre wallet et versés à \(merchant)."
                 : o == .expired
                 ? "Vous n'avez pas répondu à temps. Le marchand a reçu un refus automatique."
                 : "\(merchant) a reçu un refus. Aucun montant n'a été débité.")
                .font(.body).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true).padding(.top, 8)

            if o != .approved {
                HStack(alignment: .top, spacing: 10) {
                    Image(systemName: "exclamationmark.triangle.fill")
                        .font(.system(size: 12)).foregroundStyle(Brand.pending).padding(.top, 2)
                    Text("\(card.declineCount + 1) refus sur cette carte ce mois. Elle se bloque automatiquement à 3.")
                        .font(.sub).foregroundStyle(Brand.inkMuted)
                        .fixedSize(horizontal: false, vertical: true)
                }
                .padding(.top, 22)
            }

            Spacer()
            MPButton(title: "Fermer") { dismiss() }
        }
        .gutter()
        .padding(.bottom, 18)
    }
}

// MARK: - Abonnements

struct SubscriptionsView: View {
    @Environment(Store.self) private var store

    private struct Sub: Identifiable {
        let id = UUID()
        var name: String, category: TxCategory, usdCents: Int, day: String, active: Bool
    }

    private let subs: [Sub] = [
        .init(name: "Netflix", category: .streaming, usdCents: 1_099, day: "le 3 de chaque mois", active: true),
        .init(name: "Spotify", category: .streaming, usdCents: 1_199, day: "le 12 de chaque mois", active: true),
        .init(name: "OpenAI", category: .software, usdCents: 2_000, day: "le 1er de chaque mois", active: true),
        .init(name: "Figma", category: .software, usdCents: 1_500, day: "le 18 de chaque mois", active: true),
        .init(name: "DigitalOcean", category: .software, usdCents: 2_400, day: "le 24 de chaque mois", active: false)
    ]

    private var total: Int { subs.filter(\.active).reduce(0) { $0 + $1.usdCents } }

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                Eyebrow(text: "Total mensuel").gutter().padding(.top, 18)
                HStack(alignment: .firstTextBaseline, spacing: 8) {
                    MoneyText.usd(total, size: 32)
                    Text("≈ \(Fmt.xaf(store.fx.xaf(fromUSDCents: total)))")
                        .font(.sub).foregroundStyle(Brand.inkMuted)
                }
                .gutter().padding(.top, 8)
                Text("\(subs.filter(\.active).count) abonnements actifs sur \(subs.count)")
                    .font(.micro).foregroundStyle(Brand.inkFaint).gutter().padding(.top, 6)

                Rule().padding(.top, 24)

                HStack(alignment: .top, spacing: 10) {
                    Image(systemName: "lightbulb").font(.system(size: 13))
                        .foregroundStyle(Brand.inkMuted).padding(.top, 2)
                    Text("Gardez une carte dédiée aux abonnements : la geler suspend tous les prélèvements d'un coup.")
                        .font(.sub).foregroundStyle(Brand.inkMuted)
                        .fixedSize(horizontal: false, vertical: true)
                }
                .gutter().padding(.vertical, 18)

                Rule()

                Eyebrow(text: "Prélèvements récurrents").gutter().padding(.top, 22).padding(.bottom, 4)
                VStack(spacing: 0) {
                    ForEach(Array(subs.enumerated()), id: \.element.id) { i, s in
                        HStack(spacing: 12) {
                            IconTile(symbol: s.category.symbol,
                                     tint: s.active ? s.category.tint : Brand.inkFaint)
                            VStack(alignment: .leading, spacing: 2) {
                                HStack(spacing: 6) {
                                    Text(s.name).font(.body)
                                        .foregroundStyle(s.active ? Brand.ink : Brand.inkMuted)
                                    if !s.active {
                                        StatusPill(text: "En pause", symbol: "pause",
                                                   tint: Brand.inkMuted, soft: Brand.well)
                                    }
                                }
                                Text(s.day).font(.sub).foregroundStyle(Brand.inkMuted)
                            }
                            Spacer(minLength: 8)
                            Text(Fmt.usd(s.usdCents)).font(.subMed).monospacedDigit()
                                .foregroundStyle(s.active ? Brand.ink : Brand.inkFaint)
                            Image(systemName: "chevron.right")
                                .font(.system(size: 13, weight: .semibold))
                                .foregroundStyle(Brand.inkFaint)
                        }
                        .padding(.vertical, Metric.rowVertical)
                        if i < subs.count - 1 { Rule(inset: 51) }
                    }
                }
                .gutter()
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle("Abonnements")
        .navigationBarTitleDisplayMode(.inline)
    }
}

// MARK: - Déverrouillage

struct LockScreenView: View {
    var onUnlock: () -> Void = {}
    @Environment(Store.self) private var store
    @State private var code = ""
    @State private var error = false

    var body: some View {
        VStack(spacing: 0) {
            Spacer()
            LogoMark(size: 44)
            Text("Bon retour, \(store.user.firstName)")
                .font(.title3).foregroundStyle(Brand.ink).padding(.top, 18)
            Text(error ? "Code incorrect · 2 essais restants" : "Entrez votre code secret")
                .font(.sub)
                .foregroundStyle(error ? Brand.debit : Brand.inkMuted)
                .padding(.top, 6)

            PasscodeDots(filled: code.count, error: error).padding(.top, 32)

            Spacer()

            Keypad(side: .biometric,
                   onDigit: { d in
                       error = false
                       guard code.count < 4 else { return }
                       code.append("\(d)")
                       if code.count == 4 { check() }
                   },
                   onDelete: { if !code.isEmpty { code.removeLast() } },
                   onSide: { Haptic.success(); onUnlock() })

            Button { Haptic.tap() } label: {
                Text("Code oublié ?").font(.subMed).foregroundStyle(Brand.mark)
            }
            .padding(.top, 12)
        }
        .gutter()
        .padding(.bottom, 20)
        .page()
    }

    private func check() {
        if code == "1234" { Haptic.success(); onUnlock() }
        else {
            Haptic.warning()
            withAnimation { error = true }
            code = ""
        }
    }
}
