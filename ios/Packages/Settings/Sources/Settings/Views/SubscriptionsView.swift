import DesignSystem
import Foundation
import Money
import SwiftUI
import WalletStore

public struct SubscriptionsView: View {
    public init() {
    }

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

    public var body: some View {
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
                                    Text(s.name).font(.bodyReg)
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
