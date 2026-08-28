import DesignSystem
import Money
import SwiftUI
import WalletStore

/// The pivotal moment of the whole setup: the processor asks over a webhook
/// whether we approve a payment, and we have seconds to answer. This is that
/// decision as the cardholder sees it.
public struct AuthorizationSheet: View {
    public init(merchant: String, category: TxCategory, amountUSDCents: Int, card: VirtualCard) {
        self.merchant = merchant
        self.category = category
        self.amountUSDCents = amountUSDCents
        self.card = card
    }

    public var merchant: String
    public var category: TxCategory
    public var amountUSDCents: Int
    public var card: VirtualCard

    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    @State private var remaining = 20.0
    @State private var outcome: Outcome?

    private enum Outcome { case approved, declined, expired }

    private var amountXAF: Int { store.fx.xaf(fromUSDCents: amountUSDCents) }
    private var after: Int { store.balanceXAF - amountXAF }
    private var enough: Bool { after >= 0 }

    public var body: some View {
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

            // Countdown: a bar that drains, not a decorative ring.
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

            Text(merchant).font(.heading3).foregroundStyle(Brand.inkMuted).padding(.top, 14)
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
            Text(l).font(.bodyReg).foregroundStyle(Brand.inkMuted)
            Spacer()
            Text(v).font(.bodyReg).foregroundStyle(tint).monospacedDigit()
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
                .font(.bodyReg).foregroundStyle(Brand.inkMuted)
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

// MARK: - Unlock

public struct LockScreenView: View {
    public init(onUnlock: @escaping () -> Void = {}) {
        self.onUnlock = onUnlock
    }

    public var onUnlock: () -> Void = {}
    @Environment(Store.self) private var store
    @State private var code = ""
    @State private var error = false

    public var body: some View {
        VStack(spacing: 0) {
            Spacer()
            LogoMark(size: 44)
            Text("Bon retour, \(store.user.firstName)")
                .font(.heading3).foregroundStyle(Brand.ink).padding(.top, 18)
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
