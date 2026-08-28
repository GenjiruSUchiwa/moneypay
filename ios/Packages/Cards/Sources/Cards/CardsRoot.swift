import DesignSystem
import Money
import SwiftUI
import WalletStore

public struct CardsListView: View {
    public init(onNewCard: @escaping () -> Void) {
        self.onNewCard = onNewCard
    }

    public var onNewCard: () -> Void
    @Environment(Store.self) private var store
    @State private var scope = 0

    private var shown: [VirtualCard] {
        switch scope {
        case 1: store.cards.filter { !$0.isFrozen }
        case 2: store.cards.filter(\.isFrozen)
        default: store.cards
        }
    }

    public var body: some View {
        NavigationStack {
            ScrollView {
                LazyVStack(alignment: .leading, spacing: 0) {
                    if store.cards.isEmpty {
                        EmptyNote(title: Text("No card yet", bundle: .module),
                                  message: Text("""
                                      Create a card for each use. Freezing one suspends its charges \
                                      without touching the others.
                                      """, bundle: .module),
                                  actionTitle: Text("Create my first card", bundle: .module),
                                  action: onNewCard)
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
                            Row(icon: "plus", title: Text("Create a new card", bundle: .module), chevron: true)
                        }
                        .buttonStyle(.plain)
                        .gutter()
                        Rule()
                    }

                    Text("Cards issued by our licensed partner bank, under Visa and Mastercard International licence.",
                         bundle: .module)
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
                        Text("Cards", bundle: .module).font(.heading1).tight(-0.6).foregroundStyle(Brand.ink)
                        Spacer()
                        Button { Haptic.tap(); onNewCard() } label: {
                            Image(systemName: "plus").font(.system(size: 17, weight: .medium))
                                .foregroundStyle(Brand.ink).frame(width: 34, height: 34)
                        }
                    }
                    Segments(items: [Text("All", bundle: .module),
                                     Text("Active", bundle: .module),
                                     Text("Frozen", bundle: .module)],
                             selection: $scope)
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
                Text(verbatim: card.label).font(.bodyMed).foregroundStyle(Brand.ink)
                if card.isFrozen {
                    StatusPill(text: Text("Frozen", bundle: .module), symbol: "snowflake",
                               tint: Brand.mark, soft: Brand.markSoft)
                }
                if card.singleUse {
                    StatusPill(text: Text("Single use", bundle: .module), symbol: "1.circle",
                               tint: Brand.inkMuted, soft: Brand.well)
                }
                Spacer(minLength: 0)
            }
            if let limit = card.monthlyLimitUSDCents {
                VStack(alignment: .leading, spacing: 6) {
                    HStack {
                        Text("\(Fmt.usd(card.spentUSDCents)) spent", bundle: .module)
                            .font(.sub).foregroundStyle(Brand.inkMuted)
                        Spacer()
                        Text("cap \(Fmt.usd(limit))", bundle: .module)
                            .font(.sub).foregroundStyle(Brand.inkFaint)
                    }
                    Meter(value: card.usage,
                          tint: card.usage > 0.85 ? Brand.debit : Brand.ink)
                }
            } else {
                Text("\(Fmt.usd(card.spentUSDCents)) spent · no cap", bundle: .module)
                    .font(.sub).foregroundStyle(Brand.inkMuted)
            }
        }
        .frame(maxWidth: 300, alignment: .leading)
    }
}

#Preview("Cards — fr") {
    CardsListView(onNewCard: {})
        .environment(Store())
        .environment(\.locale, Locale(identifier: "fr"))
}
