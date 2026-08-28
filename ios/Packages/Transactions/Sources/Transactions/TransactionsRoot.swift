import DesignSystem
import Money
import SwiftUI
import WalletStore

// MARK: - List

public struct TransactionsView: View {
    public init() {
    }

    @Environment(Store.self) private var store
    @State private var filter = 0
    @State private var query = ""

    private let filters: [LocalizedStringKey] = ["All", "Cards", "Top-ups", "Declined"]

    private var filtered: [Money.Transaction] {
        var out = store.transactions
        switch filter {
        case 1: out = out.filter { $0.kind == .payment }
        case 2: out = out.filter { $0.kind == .topUp }
        case 3: out = out.filter { $0.status == .declined }
        default: break
        }
        if !query.isEmpty { out = out.filter { $0.merchant.localizedCaseInsensitiveContains(query) } }
        return out
    }

    public var body: some View {
        NavigationStack {
            ScrollView {
                LazyVStack(alignment: .leading, spacing: 0) {
                    ForEach(store.grouped(filtered), id: \.day) { group in
                        HStack {
                            Eyebrow(text: Text(verbatim: Fmt.relativeDay(group.day)))
                            Spacer()
                            Text(verbatim: dayTotal(group.items)).font(.micro).foregroundStyle(Brand.inkFaint)
                                .monospacedDigit()
                        }
                        .gutter()
                        .padding(.top, 22)
                        .padding(.bottom, 2)

                        ForEach(group.items) { tx in
                            NavigationLink { TransactionDetailView(tx: tx) } label: {
                                TransactionRow(tx: tx)
                            }
                            .buttonStyle(.plain)
                            .gutter()
                        }
                    }

                    if filtered.isEmpty {
                        EmptyNote(title: Text("No result", bundle: .module),
                                  message: Text("Try another filter or another merchant name.",
                                                bundle: .module))
                            .gutter()
                    }
                }
                .padding(.bottom, 26)
            }
            .scrollIndicators(.hidden)
            .page()
            .safeAreaInset(edge: .top, spacing: 0) {
                VStack(alignment: .leading, spacing: 14) {
                    HStack {
                        Text("Activity", bundle: .module).font(.heading1).tight(-0.6).foregroundStyle(Brand.ink)
                        Spacer()
                        Menu {
                            exportButton("Export as PDF", "arrow.down.doc")
                            exportButton("Export as CSV", "tablecells")
                        } label: {
                            Image(systemName: "square.and.arrow.up")
                                .font(.system(size: 16)).foregroundStyle(Brand.ink)
                                .frame(width: 34, height: 34)
                        }
                    }
                    Segments(items: filters.map { Text($0, bundle: .module) }, selection: $filter)
                }
                .gutter()
                .padding(.top, 4)
                .background(Brand.bg)
                .overlay(alignment: .bottom) { Rule() }
            }
            .searchable(text: $query, prompt: Text("Search a merchant", bundle: .module))
        }
    }

    private func exportButton(_ title: LocalizedStringKey, _ symbol: String) -> some View {
        Button(
            action: {},
            label: {
                Label(
                    title: { Text(title, bundle: .module) },
                    icon: { Image(systemName: symbol) }
                )
            }
        )
    }

    private func dayTotal(_ items: [Money.Transaction]) -> String {
        let net = items.filter { $0.status != .declined }.reduce(0) { $0 + $1.amountXAF }
        let sign = net > 0 ? "+" : (net < 0 ? "−" : "")
        return sign + Fmt.xaf(net)
    }
}

#Preview("Transactions — fr") {
    TransactionsView()
        .environment(Store())
        .environment(\.locale, Locale(identifier: "fr"))
}
