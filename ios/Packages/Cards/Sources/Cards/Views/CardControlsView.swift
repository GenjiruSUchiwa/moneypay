import DesignSystem
import Money
import SwiftUI
import WalletStore

public struct CardControlsView: View {
    public var card: VirtualCard
    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    @State private var draft: VirtualCard
    @State private var limitIndex: Int

    private static let presets: [Int?] = [5_000, 15_000, 50_000, 100_000, nil]

    public init(card: VirtualCard) {
        self.card = card
        _draft = State(initialValue: card)
        _limitIndex = State(initialValue: Self.presets.firstIndex { $0 == card.monthlyLimitUSDCents } ?? 4)
    }

    public var body: some View {
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 0) {
                    VirtualCardView(card: draft, compact: true)
                        .frame(maxWidth: 210)
                        .frame(maxWidth: .infinity, alignment: .center)
                        .padding(.top, 6)

                    Rule().padding(.top, 26)
                    limitSection
                    Rule()
                    permissions
                    Rule()
                    security
                }
                .padding(.bottom, 26)
            }
            .scrollIndicators(.hidden)
            .page()
            .navigationTitle("Contrôles")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .topBarLeading) {
                    Button("Annuler") { dismiss() }.foregroundStyle(Brand.inkMuted)
                }
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Enregistrer") {
                        draft.monthlyLimitUSDCents = Self.presets[limitIndex]
                        store.update(draft); Haptic.success(); dismiss()
                    }
                    .font(.bodyMed).foregroundStyle(Brand.ink)
                }
            }
        }
    }

    private var limitSection: some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: "Plafond mensuel").gutter().padding(.top, 22)

            Group {
                if let v = Self.presets[limitIndex] { MoneyText.usd(v, size: 30) }
                else { Text("Sans plafond").font(.system(size: 30, weight: .semibold)).tight(-0.6) }
            }
            .foregroundStyle(Brand.ink)
            .gutter().padding(.top, 10)

            ScrollView(.horizontal) {
                HStack(spacing: 7) {
                    ForEach(Self.presets.indices, id: \.self) { i in
                        Chip(text: Self.presets[i].map { Fmt.usd($0) } ?? "Illimité",
                             selected: i == limitIndex) {
                            withAnimation(.easeOut(duration: 0.18)) { limitIndex = i }
                        }
                    }
                }
                .gutter()
            }
            .scrollIndicators(.hidden)
            .padding(.top, 16)

            Text("Au-delà, chaque autorisation est refusée automatiquement. Un refus coûte 220 FCFA.")
                .font(.micro).foregroundStyle(Brand.inkFaint)
                .fixedSize(horizontal: false, vertical: true)
                .gutter().padding(.top, 12).padding(.bottom, 22)
        }
    }

    private var permissions: some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: "Autorisations").gutter().padding(.top, 22).padding(.bottom, 2)
            VStack(spacing: 0) {
                toggleRow("Paiements en ligne", "globe", $draft.onlineAllowed)
                Rule(inset: 51)
                toggleRow("Abonnements récurrents", "arrow.triangle.2.circlepath", $draft.subscriptionsAllowed)
                Rule(inset: 51)
                toggleRow("Usage unique", "1.circle", $draft.singleUse)
            }
            .gutter()
            Text("Une carte à usage unique s'auto-supprime après le premier paiement réussi.")
                .font(.micro).foregroundStyle(Brand.inkFaint)
                .fixedSize(horizontal: false, vertical: true)
                .gutter().padding(.top, 10).padding(.bottom, 22)
        }
    }

    private var security: some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: "Sécurité").gutter().padding(.top, 22).padding(.bottom, 2)
            VStack(spacing: 0) {
                toggleRow("Geler la carte", "snowflake", $draft.isFrozen)
                Rule(inset: 51)
                Row(icon: "globe.europe.africa", title: "Pays autorisés", chevron: true) {
                    RowValue(text: "Tous")
                }
                Rule(inset: 51)
                Row(icon: "bell.badge", title: "Alerte à chaque paiement", chevron: true) {
                    RowValue(text: "Activée")
                }
            }
            .gutter()
        }
    }

    private func toggleRow(_ title: String, _ icon: String, _ value: Binding<Bool>) -> some View {
        HStack(spacing: 13) {
            IconTile(symbol: icon)
            Text(title).font(.bodyReg).foregroundStyle(Brand.ink)
            Spacer()
            Toggle("", isOn: value).labelsHidden().tint(Brand.inkFill)
        }
        .padding(.vertical, 11)
    }
}
