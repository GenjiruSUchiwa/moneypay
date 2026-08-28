import SwiftUI

struct CardsListView: View {
    var onNewCard: () -> Void
    @Environment(Store.self) private var store
    @State private var scope = 0

    private var shown: [VirtualCard] {
        switch scope {
        case 1: store.cards.filter { !$0.isFrozen }
        case 2: store.cards.filter(\.isFrozen)
        default: store.cards
        }
    }

    var body: some View {
        NavigationStack {
            ScrollView {
                LazyVStack(alignment: .leading, spacing: 0) {
                    if store.cards.isEmpty {
                        EmptyNote(title: "Aucune carte",
                                  message: "Créez une carte dédiée à chaque usage. Geler l'une d'elles suspend ses prélèvements sans toucher aux autres.",
                                  actionTitle: "Créer ma première carte", action: onNewCard)
                            .gutter().padding(.top, 20)
                    }

                    ForEach(Array(shown.enumerated()), id: \.element.id) { i, card in
                        NavigationLink { CardDetailView(card: card) } label: {
                            VStack(alignment: .leading, spacing: 14) {
                                VirtualCardView(card: card).frame(maxWidth: 300)
                                meta(card)
                            }
                            .padding(.vertical, 22)
                            .contentShape(.rect)
                        }
                        .buttonStyle(Press())
                        .gutter()
                        if i < shown.count - 1 { Rule() }
                    }

                    if !store.cards.isEmpty {
                        Rule()
                        Button { Haptic.tap(); onNewCard() } label: {
                            Row(icon: "plus", title: "Créer une nouvelle carte", chevron: true)
                        }
                        .buttonStyle(.plain)
                        .gutter()
                        Rule()
                    }

                    Text("Cartes émises par notre banque partenaire agréée, sous licence Visa et Mastercard International.")
                        .font(.micro).foregroundStyle(Brand.inkFaint)
                        .fixedSize(horizontal: false, vertical: true)
                        .gutter().padding(.top, 20)
                }
                .padding(.bottom, 28)
            }
            .scrollIndicators(.hidden)
            .page()
            .safeAreaInset(edge: .top, spacing: 0) {
                VStack(alignment: .leading, spacing: 14) {
                    HStack {
                        Text("Cartes").font(.title1).tight(-0.6).foregroundStyle(Brand.ink)
                        Spacer()
                        Button { Haptic.tap(); onNewCard() } label: {
                            Image(systemName: "plus").font(.system(size: 17, weight: .medium))
                                .foregroundStyle(Brand.ink).frame(width: 34, height: 34)
                        }
                    }
                    Segments(items: ["Toutes", "Actives", "Gelées"], selection: $scope)
                }
                .gutter()
                .padding(.top, 4)
                .background(Brand.bg)
                .overlay(alignment: .bottom) { Rule() }
            }
        }
    }

    private func meta(_ card: VirtualCard) -> some View {
        VStack(alignment: .leading, spacing: 9) {
            HStack(spacing: 7) {
                Text(card.label).font(.bodyMed).foregroundStyle(Brand.ink)
                if card.isFrozen {
                    StatusPill(text: "Gelée", symbol: "snowflake", tint: Brand.mark, soft: Brand.markSoft)
                }
                if card.singleUse {
                    StatusPill(text: "Usage unique", symbol: "1.circle", tint: Brand.inkMuted, soft: Brand.well)
                }
                Spacer(minLength: 0)
            }
            if let limit = card.monthlyLimitUSDCents {
                VStack(alignment: .leading, spacing: 6) {
                    HStack {
                        Text("\(Fmt.usd(card.spentUSDCents)) dépensés")
                            .font(.sub).foregroundStyle(Brand.inkMuted)
                        Spacer()
                        Text("plafond \(Fmt.usd(limit))")
                            .font(.sub).foregroundStyle(Brand.inkFaint)
                    }
                    Meter(value: card.usage,
                          tint: card.usage > 0.85 ? Brand.debit : Brand.ink)
                }
            } else {
                Text("\(Fmt.usd(card.spentUSDCents)) dépensés · sans plafond")
                    .font(.sub).foregroundStyle(Brand.inkMuted)
            }
        }
        .frame(maxWidth: 300, alignment: .leading)
    }
}

/// Jauge fine. Deux pixels : elle informe sans peser.
struct Meter: View {
    var value: Double
    var tint: Color = Brand.ink
    var height: CGFloat = 4

    var body: some View {
        GeometryReader { geo in
            ZStack(alignment: .leading) {
                Capsule().fill(Brand.hairline)
                Capsule().fill(tint)
                    .frame(width: max(0.02, min(1, value)) * geo.size.width)
            }
        }
        .frame(height: height)
    }
}
