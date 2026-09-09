import Charts
import DesignSystem
import SwiftUI
import WalletStore

public struct InsightsView: View {
    public init() {
    }

    @Environment(Store.self) private var store
    @State private var period = 0
    @State private var selectedMonth: String?
    @State private var showTable = false

    private let months = MonthSpend.demoSeries([62_400, 88_100, 74_900, 121_300, 96_700, 108_500])

    private var current: Int { months.last?.xaf ?? 0 }
    private var previous: Int { months.dropLast().last?.xaf ?? 0 }
    private var currentLabel: String { months.last?.label ?? "" }
    private var previousLabel: String { months.dropLast().last?.label ?? "" }
    private var delta: Double { previous == 0 ? 0 : Double(current - previous) / Double(previous) }

    public var body: some View {
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
                    Text("Analyse", bundle: .module).font(.heading1).tight(-0.6).foregroundStyle(Brand.ink)
                    Segments(items: [Text("6 months", bundle: .module),
                                     Text("This year", bundle: .module),
                                     Text("All", bundle: .module)],
                             selection: $period)
                }
                .gutter()
                .padding(.top, 4)
                .background(Brand.bg)
                .overlay(alignment: .bottom) { Rule() }
            }
        }
    }

    private var hero: some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: Text("Spent in \(currentLabel)", bundle: .module))
            MoneyText.xaf(current, size: 36).padding(.top, 8)
            HStack(spacing: 5) {
                Image(systemName: delta >= 0 ? "arrow.up.right" : "arrow.down.right")
                    .font(.system(size: 11, weight: .bold))
                Text("\(delta, format: .percent.precision(.fractionLength(0)).sign(strategy: .always())) versus \(previousLabel)",
                     bundle: .module)
                    .font(.sub)
            }
            .foregroundStyle(delta >= 0 ? Brand.pending : Brand.credit)
            .padding(.top, 8)
        }
        .gutter()
        .padding(.top, 20)
        .padding(.bottom, 26)
    }

    private var trend: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                SectionHead(title: Text("By month", bundle: .module), trailing: "FCFA")
                Spacer()
                Button {
                    withAnimation(.easeOut(duration: 0.18)) { showTable.toggle() }
                } label: {
                    Text(showTable ? "Chart" : "Table", bundle: .module)
                        .font(.microMed).foregroundStyle(Brand.mark)
                }
            }
            .gutter()

            if showTable {
                VStack(spacing: 0) {
                    ForEach(Array(months.reversed().enumerated()), id: \.element.id) { i, m in
                        HStack {
                            Text(verbatim: m.label.capitalized).font(.bodyReg)
                                .foregroundStyle(Brand.inkMuted)
                            Spacer()
                            Text(verbatim: Fmt.group(m.xaf))
                                .font(m.isCurrent ? .bodyMed : .bodyReg)
                                .foregroundStyle(Brand.ink).monospacedDigit()
                        }
                        .padding(.vertical, 12)
                        if i < months.count - 1 { Rule() }
                    }
                }
                .gutter()
            } else {
                VStack(alignment: .leading, spacing: 10) {
                    Group {
                        if let picked = selectedMonth.flatMap({ s in months.first { $0.label == s } }) {
                            Text(verbatim: "\(picked.label.capitalized) · \(Fmt.xaf(picked.xaf))")
                        } else {
                            Text("Tap a bar for the detail", bundle: .module)
                        }
                    }
                        .font(.sub)
                        .foregroundStyle(selectedMonth == nil ? Brand.inkFaint : Brand.ink)
                        .monospacedDigit()
                        .gutter()

                    Chart(months) { m in
                        BarMark(x: .value(String(localized: "Month", bundle: .module), m.label),
                                y: .value(String(localized: "Spend", bundle: .module), m.xaf),
                                width: .fixed(20))
                            .foregroundStyle(Brand.ink.opacity(m.isCurrent ? 1 : 0.28))
                            .cornerRadius(4)
                            .annotation(position: .top, spacing: 5) {
                                if m.isCurrent || m.label == selectedMonth {
                                    Text(m.xaf, format: .number.notation(.compactName))
                                        .font(.system(size: 10, weight: .medium))
                                        .foregroundStyle(Brand.inkMuted)
                                        .monospacedDigit()
                                }
                            }
                            .accessibilityLabel(Text(verbatim: m.label))
                            .accessibilityValue(Text(verbatim: Fmt.xaf(m.xaf)))
                    }
                    .chartXSelection(value: $selectedMonth)
                    .chartYAxis {
                        AxisMarks(position: .leading, values: .automatic(desiredCount: 3)) { v in
                            AxisGridLine().foregroundStyle(Viz.grid)
                            AxisValueLabel {
                                if let n = v.as(Int.self) {
                                    Text(n, format: .number.notation(.compactName))
                                        .font(.system(size: 10))
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

    private var ranking: some View {
        let rows = store.spendByCategory()
        let maxV = rows.first?.xaf ?? 1
        return VStack(alignment: .leading, spacing: 14) {
            SectionHead(title: Text("By category", bundle: .module), trailing: "FCFA").gutter().padding(.top, 22)
            VStack(spacing: 14) {
                ForEach(Array(rows.enumerated()), id: \.offset) { _, row in
                    VStack(spacing: 7) {
                        HStack(spacing: 9) {
                            Image(systemName: row.category.symbol)
                                .font(.system(size: 10, weight: .medium))
                                .foregroundStyle(row.category.tint)
                                .frame(width: 20, height: 20)
                                .background(row.category.tint.opacity(0.14),
                                            in: .rect(cornerRadius: 6, style: .continuous))
                            Text(row.category.label).font(.sub).foregroundStyle(Brand.ink)
                            Spacer()
                            Text(verbatim: Fmt.group(row.xaf)).font(.subMed)
                                .foregroundStyle(Brand.ink).monospacedDigit()
                        }
                        ProgressView(value: Double(row.xaf) / Double(maxV))
                            .tint(Brand.ink)
                            .accessibilityLabel(Text("Share of spending", bundle: .module))
                    }
                }
            }
            .gutter()
        }
        .padding(.bottom, 26)
    }

    private var costs: some View {
        VStack(alignment: .leading, spacing: 4) {
            SectionHead(title: Text("True cost of the month", bundle: .module), trailing: "FCFA").gutter().padding(.top, 22)
            VStack(spacing: 0) {
                cost(Text("FX margin · \(0.03, format: .percent)", bundle: .module),
                     3_254,
                     Text("Taken on every FCFA → USD conversion", bundle: .module))
                Rule()
                cost(Text("Top-up fees", bundle: .module),
                     1_477,
                     Text("\(0.015, format: .percent.precision(.fractionLength(1))) on MTN MoMo and Orange Money",
                          bundle: .module))
                Rule()
                cost(Text("Decline fees", bundle: .module),
                     220,
                     Text("\(1) authorization declined this month", bundle: .module))
                Rule()
                HStack {
                    Text("Total fees", bundle: .module).font(.bodyMed).foregroundStyle(Brand.ink)
                    Spacer()
                    Text(verbatim: Fmt.xaf(4_951)).font(.bodyMed)
                        .foregroundStyle(Brand.pending).monospacedDigit()
                }
                .padding(.vertical, Metric.rowVertical)
            }
            .gutter()
        }
        .padding(.bottom, 26)
    }

    private func cost(_ title: Text, _ amountXAF: Int, _ detail: Text) -> some View {
        HStack(alignment: .top) {
            VStack(alignment: .leading, spacing: 3) {
                title.font(.bodyReg).foregroundStyle(Brand.ink)
                detail.font(.micro).foregroundStyle(Brand.inkMuted)
            }
            Spacer(minLength: 8)
            Text(verbatim: Fmt.xaf(amountXAF, symbol: false))
                .font(.bodyReg).foregroundStyle(Brand.ink).monospacedDigit()
        }
        .padding(.vertical, Metric.rowVertical)
    }

    private var perCard: some View {
        VStack(alignment: .leading, spacing: 4) {
            SectionHead(title: Text("By card", bundle: .module)).gutter().padding(.top, 22)
            VStack(spacing: 0) {
                ForEach(Array(store.cards.enumerated()), id: \.element.id) { i, card in
                    HStack(spacing: 12) {
                        RoundedRectangle(cornerRadius: 5, style: .continuous)
                            .fill(card.theme.fill).frame(width: 34, height: 22)
                            .overlay { RoundedRectangle(cornerRadius: 5).stroke(Brand.hairline, lineWidth: 1) }
                        VStack(alignment: .leading, spacing: 2) {
                            Text(verbatim: card.label).font(.bodyReg).foregroundStyle(Brand.ink)
                            Text(verbatim: "•• \(card.last4)").font(.micro)
                                .foregroundStyle(Brand.inkMuted)
                        }
                        Spacer()
                        Text(verbatim: Fmt.usd(card.spentUSDCents)).font(.subMed)
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

#Preview("Insights — fr") {
    InsightsView()
        .environment(Store())
        .environment(\.locale, Locale(identifier: "fr"))
}
