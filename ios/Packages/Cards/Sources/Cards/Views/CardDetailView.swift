import DesignSystem
import Money
import SwiftUI
import Transactions
import WalletStore

public struct CardDetailView: View {
    public init(card: VirtualCard) {
        self.card = card
    }

    public var card: VirtualCard
    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    @State private var revealed = false
    @State private var toastMsg: Toast?
    @State private var confirmFreeze = false
    @State private var confirmDelete = false
    @State private var showControls = false

    private var live: VirtualCard { store.cards.first { $0.id == card.id } ?? card }

    public var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                VirtualCardView(card: live, revealed: revealed)
                    .frame(maxWidth: 300)
                    .frame(maxWidth: .infinity, alignment: .center)
                    .gutter()
                    .padding(.top, 4)

                actions
                Rule().padding(.top, 24)
                if revealed { secrets; Rule() }
                limit
                Rule()
                controls
                Rule()
                transactions
                Rule()
                Button { confirmDelete = true } label: {
                    Row(icon: "trash", title: Text("Delete this card", bundle: .module), destructive: true)
                }
                .buttonStyle(.plain)
                .gutter()
            }
            .padding(.bottom, 34)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle(Text(verbatim: live.label))
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Menu {
                    menuButton("Rename the card", "pencil") {}
                    menuButton("Add to Apple Wallet", "wallet.pass") {}
                    menuButton("View the statement", "doc.text") {}
                    Divider()
                    Button(role: .destructive) { confirmDelete = true } label: {
                        Label {
                            Text("Delete the card", bundle: .module)
                        } icon: {
                            Image(systemName: "trash")
                        }
                    }
                } label: {
                    Image(systemName: "ellipsis").font(.system(size: 15, weight: .semibold))
                }
            }
        }
        .toast($toastMsg)
        .sheet(isPresented: $showControls) { CardControlsView(card: live) }
        .confirmationDialog(Text(live.isFrozen ? "Unfreeze this card?" : "Freeze this card?",
                                 bundle: .module),
                            isPresented: $confirmFreeze, titleVisibility: .visible) {
            Button {
                let wasFrozen = live.isFrozen
                store.toggleFreeze(live)
                toastMsg = Toast(text: Text(wasFrozen ? "Card unfrozen" : "Card frozen",
                                            bundle: .module),
                                 icon: wasFrozen ? "checkmark" : "snowflake")
            } label: {
                Text(live.isFrozen ? "Unfreeze" : "Freeze", bundle: .module)
            }
            Button(role: .cancel) {} label: { Text("Cancel", bundle: .module) }
        } message: {
            Text("While the card is frozen every authorization is declined. Attached subscriptions will fail.",
                 bundle: .module)
        }
        .confirmationDialog(Text("Delete permanently?", bundle: .module), isPresented: $confirmDelete,
                            titleVisibility: .visible) {
            Button(role: .destructive) { store.deleteCard(live); dismiss() } label: {
                Text("Delete the card", bundle: .module)
            }
            Button(role: .cancel) {} label: { Text("Cancel", bundle: .module) }
        } message: {
            Text("Attached subscriptions will stop being charged.", bundle: .module)
        }
    }

    private var actions: some View {
        HStack(spacing: 4) {
            QuickAction(icon: revealed ? "eye.slash" : "eye",
                        label: Text(revealed ? "Hide" : "Details", bundle: .module)) {
                withAnimation(.spring(response: 0.32, dampingFraction: 0.86)) { revealed.toggle() }
            }
            QuickAction(icon: live.isFrozen ? "sun.max" : "snowflake",
                        label: Text(live.isFrozen ? "Unfreeze" : "Freeze",
                                    bundle: .module)) { confirmFreeze = true }
            QuickAction(icon: "slider.horizontal.3",
                        label: Text("Controls", bundle: .module)) { showControls = true }
            QuickAction(icon: "wallet.pass", label: Text("Wallet", bundle: .module)) {
                toastMsg = Toast(text: Text("Apple Wallet hand-off simulated", bundle: .module),
                                 icon: "wallet.pass")
            }
        }
        .gutter()
        .padding(.top, 22)
    }

    // MARK: Card secrets

    private var secrets: some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: Text("Card details", bundle: .module)).gutter().padding(.top, 22).padding(.bottom, 4)
            VStack(spacing: 0) {
                copyRow("Cardholder", store.user.fullName.uppercased(), mono: false)
                Rule()
                copyRow("Number", live.pan.chunked())
                Rule()
                copyRow("Expiry", live.expiry)
                Rule()
                copyRow("CVV", live.cvv)
            }
            .gutter()
            Text("MoneyPay will never ask you for these details.", bundle: .module)
                .font(.micro).foregroundStyle(Brand.inkFaint)
                .gutter().padding(.top, 10).padding(.bottom, 20)
        }
    }

    private func menuButton(_ title: LocalizedStringKey, _ symbol: String,
                            action: @escaping () -> Void) -> some View {
        Button(action: action) {
            Label {
                Text(title, bundle: .module)
            } icon: {
                Image(systemName: symbol)
            }
        }
    }

    private func copyRow(_ label: String.LocalizationValue, _ value: String,
                         mono: Bool = true) -> some View {
        let title = String(localized: label, bundle: .module)
        return Button {
            UIPasteboard.general.string = value
            Haptic.success()
            toastMsg = Toast(text: Text("\(title) copied", bundle: .module), icon: "doc.on.doc")
        } label: {
            HStack {
                Text(verbatim: title).font(.bodyReg).foregroundStyle(Brand.inkMuted)
                Spacer()
                Text(verbatim: value).font(mono ? .dataMono : .bodyReg).foregroundStyle(Brand.ink)
                Image(systemName: "doc.on.doc").font(.system(size: 12))
                    .foregroundStyle(Brand.inkFaint)
            }
            .padding(.vertical, Metric.rowVertical)
            .contentShape(.rect)
        }
        .buttonStyle(.plain)
    }

    // MARK: Spend limit

    private var limit: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack {
                Eyebrow(text: Text("Monthly cap", bundle: .module))
                Spacer()
                Button { showControls = true } label: {
                    Text("Edit", bundle: .module).font(.microMed).foregroundStyle(Brand.mark)
                }
            }
            .gutter().padding(.top, 22)

            HStack(alignment: .firstTextBaseline, spacing: 7) {
                MoneyText.usd(live.spentUSDCents, size: 26)
                Group {
                    if let cap = live.monthlyLimitUSDCents {
                        Text("of \(Fmt.usd(cap))", bundle: .module)
                    } else {
                        Text("without a cap", bundle: .module)
                    }
                }
                .font(.sub).foregroundStyle(Brand.inkMuted)
            }
            .gutter().padding(.top, 10)

            if let l = live.monthlyLimitUSDCents {
                ProgressView(value: live.usage)
                    .tint(live.usage > 0.85 ? Brand.debit : Brand.ink)
                    .accessibilityLabel(Text("Card spending", bundle: .module))
                    .gutter().padding(.top, 12)
                HStack {
                    Text("\(Fmt.usd(max(0, l - live.spentUSDCents))) left", bundle: .module)
                        .font(.micro).foregroundStyle(Brand.inkMuted)
                    Spacer()
                    Text("Resets on the 1st", bundle: .module).font(.micro).foregroundStyle(Brand.inkFaint)
                }
                .gutter().padding(.top, 7)
            }

            if live.declineCount > 0 {
                HStack(spacing: 8) {
                    Image(systemName: "exclamationmark.triangle.fill")
                        .font(.system(size: 12)).foregroundStyle(Brand.pending)
                    Text("\(live.declineCount) declines this month · the card blocks at 3", bundle: .module)
                        .font(.sub).foregroundStyle(Brand.inkMuted)
                }
                .gutter().padding(.top, 14)
            }
        }
        .padding(.bottom, 20)
    }

    // MARK: Controls

    private var controls: some View {
        VStack(spacing: 0) {
            Row(icon: "globe", title: Text("Online payments", bundle: .module)) {
                RowValue(text: Text(live.onlineAllowed ? "Allowed" : "Blocked", bundle: .module),
                         tint: live.onlineAllowed ? Brand.credit : Brand.debit)
            }
            Rule(inset: 51)
            Row(icon: "arrow.triangle.2.circlepath",
                title: Text("Recurring subscriptions", bundle: .module)) {
                RowValue(text: Text(live.subscriptionsAllowed ? "Allowed" : "Blocked", bundle: .module),
                         tint: live.subscriptionsAllowed ? Brand.credit : Brand.debit)
            }
            Rule(inset: 51)
            Button { showControls = true } label: {
                Row(icon: "slider.horizontal.3", title: Text("All controls", bundle: .module),
                    chevron: true)
            }
            .buttonStyle(.plain)
        }
        .gutter()
    }

    // MARK: Transactions

    private var transactions: some View {
        let txs = store.transactions(for: live.id)
        return VStack(alignment: .leading, spacing: 2) {
            SectionHead(title: Text("Transactions", bundle: .module),
                        trailing: txs.isEmpty ? nil : txs.count.formatted(),
                        tappable: !txs.isEmpty)
                .gutter().padding(.top, 22)
            if txs.isEmpty {
                EmptyNote(title: Text("No transaction", bundle: .module),
                          message: Text("Payments made with this card will show up here.",
                                        bundle: .module))
                    .gutter()
            } else {
                ForEach(txs.prefix(6)) { tx in
                    NavigationLink { TransactionDetailView(tx: tx) } label: { TransactionRow(tx: tx) }
                        .buttonStyle(.plain)
                        .gutter()
                }
            }
        }
        .padding(.bottom, 8)
    }
}
