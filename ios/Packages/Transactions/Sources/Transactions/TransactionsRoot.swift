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

    private let filters = ["Tout", "Cartes", "Recharges", "Refusés"]

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
                            Eyebrow(text: Fmt.relativeDay(group.day))
                            Spacer()
                            Text(dayTotal(group.items)).font(.micro).foregroundStyle(Brand.inkFaint)
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
                        EmptyNote(title: "Aucun résultat",
                                  message: "Essayez un autre filtre ou un autre nom de marchand.")
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
                        Text("Activité").font(.heading1).tight(-0.6).foregroundStyle(Brand.ink)
                        Spacer()
                        Menu {
                            Button("Exporter en PDF", systemImage: "arrow.down.doc") {}
                            Button("Exporter en CSV", systemImage: "tablecells") {}
                        } label: {
                            Image(systemName: "square.and.arrow.up")
                                .font(.system(size: 16)).foregroundStyle(Brand.ink)
                                .frame(width: 34, height: 34)
                        }
                    }
                    Segments(items: filters, selection: $filter)
                }
                .gutter()
                .padding(.top, 4)
                .background(Brand.bg)
                .overlay(alignment: .bottom) { Rule() }
            }
            .searchable(text: $query, prompt: "Rechercher un marchand")
        }
    }

    private func dayTotal(_ items: [Money.Transaction]) -> String {
        let net = items.filter { $0.status != .declined }.reduce(0) { $0 + $1.amountXAF }
        return (net > 0 ? "+" : net < 0 ? "−" : "") + Fmt.group(net) + " FCFA"
    }
}
