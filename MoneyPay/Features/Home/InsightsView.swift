import SwiftUI
import Charts

struct MonthSpend: Identifiable {
    let id = UUID()
    var label: String
    var xaf: Int
    var isCurrent: Bool
}

struct InsightsView: View {
    @Environment(Store.self) private var store
    @State private var period = 0
    @State private var selectedMonth: String?
    @State private var showTable = false

    private let months: [MonthSpend] = [
        .init(label: "mars", xaf: 62_400, isCurrent: false),
        .init(label: "avr.", xaf: 88_100, isCurrent: false),
        .init(label: "mai", xaf: 74_900, isCurrent: false),
        .init(label: "juin", xaf: 121_300, isCurrent: false),
        .init(label: "juil.", xaf: 96_700, isCurrent: false),
        .init(label: "août", xaf: 108_500, isCurrent: true)
    ]

    private var current: Int { months.last?.xaf ?? 0 }
    private var previous: Int { months.dropLast().last?.xaf ?? 0 }
    private var delta: Double { previous == 0 ? 0 : Double(current - previous) / Double(previous) }

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 0) {
                    hero
                    trend
                    Rule()
                    ranking
                    Rule()
                    costs
                    Rule()
                    perCard
                }
                .padding(.bottom, 30)
            }
            .scrollIndicators(.hidden)
            .page()
            .safeAreaInset(edge: .top, spacing: 0) {
                VStack(alignment: .leading, spacing: 14) {
                    Text("Analyse").font(.title1).tight(-0.6).foregroundStyle(Brand.ink)
                    Segments(items: ["6 mois", "Cette année", "Tout"], selection: $period)
                }
                .gutter()
                .padding(.top, 4)
                .background(Brand.bg)
                .overlay(alignment: .bottom) { Rule() }
            }
        }
    }

    // MARK: Chiffre phare — pas un graphique

    private var hero: some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: "Dépensé en août")
            MoneyText.xaf(current, size: 36).padding(.top, 8)
            // L'état ne repose jamais sur la couleur seule : flèche + libellé.
            HStack(spacing: 5) {
                Image(systemName: delta >= 0 ? "arrow.up.right" : "arrow.down.right")
                    .font(.system(size: 11, weight: .bold))
                Text("\(delta >= 0 ? "+" : "")\(Int(delta * 100)) % par rapport à juillet").font(.sub)
            }
            .foregroundStyle(delta >= 0 ? Brand.pending : Brand.credit)
            .padding(.top, 8)
        }
        .gutter()
        .padding(.top, 20)
        .padding(.bottom, 26)
    }

    // MARK: Tendance — une série, une teinte, pas de légende

    private var trend: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                SectionHead(title: "Par mois", trailing: "FCFA")
                Spacer()
                Button {
                    Haptic.tap()
                    withAnimation(.easeOut(duration: 0.18)) { showTable.toggle() }
                } label: {
                    Text(showTable ? "Graphique" : "Tableau")
                        .font(.microMed).foregroundStyle(Brand.mark)
                }
            }
            .gutter()

            if showTable {
                // Vue tableau : les mêmes valeurs, lisibles sans la couleur.
                VStack(spacing: 0) {
                    ForEach(Array(months.reversed().enumerated()), id: \.element.id) { i, m in
                        HStack {
                            Text(m.label.capitalized).font(.body).foregroundStyle(Brand.inkMuted)
                            Spacer()
                            Text(Fmt.group(m.xaf))
                                .font(m.isCurrent ? .bodyMed : .body)
                                .foregroundStyle(Brand.ink).monospacedDigit()
                        }
                        .padding(.vertical, 12)
                        if i < months.count - 1 { Rule() }
                    }
                }
                .gutter()
            } else {
                VStack(alignment: .leading, spacing: 10) {
                    Text(selectedMonth.flatMap { s in months.first { $0.label == s } }
                            .map { "\($0.label.capitalized) · \(Fmt.xaf($0.xaf))" }
                         ?? "Touchez une barre pour le détail")
                        .font(.sub)
                        .foregroundStyle(selectedMonth == nil ? Brand.inkFaint : Brand.ink)
                        .monospacedDigit()
                        .gutter()

                    Chart(months) { m in
                        BarMark(x: .value("Mois", m.label),
                                y: .value("Dépense", m.xaf),
                                width: .fixed(20))
                            .foregroundStyle(Brand.ink.opacity(m.isCurrent ? 1 : 0.28))
                            .cornerRadius(4)
                            .annotation(position: .top, spacing: 5) {
                                // Étiquette directe sélective, jamais sur toutes les barres.
                                if m.isCurrent || m.label == selectedMonth {
                                    Text("\(m.xaf / 1000)k")
                                        .font(.system(size: 10, weight: .medium))
                                        .foregroundStyle(Brand.inkMuted)
                                        .monospacedDigit()
                                }
                            }
                            .accessibilityLabel(m.label)
                            .accessibilityValue(Fmt.xaf(m.xaf))
                    }
                    .chartXSelection(value: $selectedMonth)
                    .chartYAxis {
                        AxisMarks(position: .leading, values: .automatic(desiredCount: 3)) { v in
                            AxisGridLine().foregroundStyle(Viz.grid)
                            AxisValueLabel {
                                if let n = v.as(Int.self) {
                                    Text("\(n / 1000)k").font(.system(size: 10))
                                        .foregroundStyle(Brand.inkFaint)
                                }
                            }
                        }
                    }
                    .chartXAxis {
                        AxisMarks { _ in
                            AxisValueLabel().font(.system(size: 10)).foregroundStyle(Brand.inkFaint)
                        }
                    }
                    .frame(height: 158)
                    .gutter()
                }
            }
        }
        .padding(.top, 4)
        .padding(.bottom, 26)
    }

    // MARK: Catégories

    private var ranking: some View {
        let rows = store.spendByCategory()
        let maxV = rows.first?.xaf ?? 1
        return VStack(alignment: .leading, spacing: 14) {
            SectionHead(title: "Par catégorie", trailing: "FCFA").gutter().padding(.top, 22)
            VStack(spacing: 14) {
                ForEach(Array(rows.enumerated()), id: \.offset) { _, row in
                    VStack(spacing: 7) {
                        HStack(spacing: 9) {
                            // L'identité passe par l'icône teintée ; la barre ne
                            // code que la magnitude, en une seule teinte.
                            Image(systemName: row.category.symbol)
                                .font(.system(size: 10, weight: .medium))
                                .foregroundStyle(row.category.tint)
                                .frame(width: 20, height: 20)
                                .background(row.category.tint.opacity(0.14),
                                            in: .rect(cornerRadius: 6, style: .continuous))
                            Text(row.category.label).font(.sub).foregroundStyle(Brand.ink)
                            Spacer()
                            Text(Fmt.group(row.xaf)).font(.subMed)
                                .foregroundStyle(Brand.ink).monospacedDigit()
                        }
                        Meter(value: Double(row.xaf) / Double(maxV), tint: Brand.ink, height: 4)
                    }
                }
            }
            .gutter()
        }
        .padding(.bottom, 26)
    }

    // MARK: Coût réel

    private var costs: some View {
        VStack(alignment: .leading, spacing: 4) {
            SectionHead(title: "Coût réel du mois", trailing: "FCFA").gutter().padding(.top, 22)
            VStack(spacing: 0) {
                cost("Marge de change · 3 %", "3 254", "Prélevée sur chaque conversion FCFA → USD")
                Rule()
                cost("Frais de rechargement", "1 477", "1,5 % sur MTN MoMo et Orange Money")
                Rule()
                cost("Frais de refus", "220", "1 autorisation refusée ce mois")
                Rule()
                HStack {
                    Text("Total des frais").font(.bodyMed).foregroundStyle(Brand.ink)
                    Spacer()
                    Text("4 951 FCFA").font(.bodyMed).foregroundStyle(Brand.pending).monospacedDigit()
                }
                .padding(.vertical, Metric.rowVertical)
            }
            .gutter()
        }
        .padding(.bottom, 26)
    }

    private func cost(_ t: String, _ v: String, _ sub: String) -> some View {
        HStack(alignment: .top) {
            VStack(alignment: .leading, spacing: 3) {
                Text(t).font(.body).foregroundStyle(Brand.ink)
                Text(sub).font(.micro).foregroundStyle(Brand.inkMuted)
            }
            Spacer(minLength: 8)
            Text(v).font(.body).foregroundStyle(Brand.ink).monospacedDigit()
        }
        .padding(.vertical, Metric.rowVertical)
    }

    // MARK: Par carte

    private var perCard: some View {
        VStack(alignment: .leading, spacing: 4) {
            SectionHead(title: "Par carte").gutter().padding(.top, 22)
            VStack(spacing: 0) {
                ForEach(Array(store.cards.enumerated()), id: \.element.id) { i, card in
                    HStack(spacing: 12) {
                        RoundedRectangle(cornerRadius: 5, style: .continuous)
                            .fill(card.theme.fill).frame(width: 34, height: 22)
                            .overlay { RoundedRectangle(cornerRadius: 5).stroke(Brand.hairline, lineWidth: 1) }
                        VStack(alignment: .leading, spacing: 2) {
                            Text(card.label).font(.body).foregroundStyle(Brand.ink)
                            Text("•• \(card.last4)").font(.micro).foregroundStyle(Brand.inkMuted)
                        }
                        Spacer()
                        Text(Fmt.usd(card.spentUSDCents)).font(.subMed)
                            .foregroundStyle(Brand.ink).monospacedDigit()
                    }
                    .padding(.vertical, 12)
                    if i < store.cards.count - 1 { Rule(inset: 46) }
                }
            }
            .gutter()
        }
    }
}
