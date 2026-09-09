import Cards
import Convert
import DesignSystem
import Money
import Settings
import SwiftUI
import Transactions
import WalletStore

public struct HomeView: View {
    public init(onTopUp: @escaping () -> Void, onNewCard: @escaping () -> Void) {
        self.onTopUp = onTopUp
        self.onNewCard = onNewCard
    }

    public var onTopUp: () -> Void
    public var onNewCard: () -> Void

    @Environment(Store.self) private var store
    @State private var showNotifications = false
    @State private var showConvert = false
    @State private var showSend = false
    @State private var selectedCard: VirtualCard?

    public var body: some View {
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 0) {
                    balance
                    actions
                    Rule().padding(.top, Metric.section)
                    monthRow
                    Rule()
                    cardsSection
                    Rule()
                    activitySection
                }
                .padding(.bottom, 30)
            }
            .scrollIndicators(.hidden)
            .page()
            .safeAreaInset(edge: .top, spacing: 0) { topBar }
            .navigationDestination(item: $selectedCard) { CardDetailView(card: $0) }
            .sheet(isPresented: $showNotifications) { NotificationsView() }
            .sheet(isPresented: $showConvert) { ConvertView() }
            .sheet(isPresented: $showSend) { SendMoneyView() }
        }
    }

    private var topBar: some View {
        HStack(spacing: 10) {
            NavigationLink { SettingsView() } label: {
                HStack(spacing: 8) {
                    Text(verbatim: store.user.initials)
                        .font(.system(size: 12, weight: .semibold))
                        .foregroundStyle(Brand.onAction)
                        .frame(width: 26, height: 26)
                        .background(Brand.action, in: .rect(cornerRadius: 7, style: .continuous))
                    Text(verbatim: store.user.fullName).font(.subMed).foregroundStyle(Brand.ink)
                    Image(systemName: "chevron.down")
                        .font(.system(size: 9, weight: .bold))
                        .foregroundStyle(Brand.inkFaint)
                }
            }
            .buttonStyle(.plain)
            Spacer()
            Button { showNotifications = true } label: {
                ZStack(alignment: .topTrailing) {
                    Image(systemName: "bell")
                        .font(.system(size: 17, weight: .regular))
                        .foregroundStyle(Brand.ink)
                        .frame(width: 34, height: 34)
                    if store.unreadCount > 0 {
                        Circle().fill(Brand.debit).frame(width: 7, height: 7).offset(x: -6, y: 6)
                    }
                }
            }
        }
        .gutter()
        .frame(height: 46)
        .background(Brand.bg)
    }

    private var balance: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack(spacing: 7) {
                Eyebrow(text: Text("Available balance", bundle: .module))
                Button {
                    withAnimation(.easeOut(duration: 0.2)) { store.hiddenBalance.toggle() }
                } label: {
                    Image(systemName: store.hiddenBalance ? "eye.slash" : "eye")
                        .font(.system(size: 11))
                        .foregroundStyle(Brand.inkFaint)
                }
            }

            Group {
                if store.hiddenBalance {
                    Text(verbatim: "••• •••").font(.system(size: 40, weight: .semibold))
                        .foregroundStyle(Brand.ink)
                } else {
                    MoneyText.xaf(store.balanceXAF, size: 40)
                }
            }
            .padding(.top, 8)

            HStack(spacing: 5) {
                Image(systemName: "arrow.left.arrow.right").font(.system(size: 10, weight: .semibold))
                Text("≈ \(Fmt.usd(store.usdEquivalentCents)) to spend on a card", bundle: .module)
                    .font(.sub)
            }
            .foregroundStyle(Brand.inkMuted)
            .padding(.top, 7)
            .opacity(store.hiddenBalance ? 0 : 1)
        }
        .gutter()
        .padding(.top, 14)
    }

    private var actions: some View {
        HStack(spacing: 4) {
            QuickAction(icon: "plus", label: Text("Top up", bundle: .module), action: onTopUp)
            QuickAction(icon: "arrow.left.arrow.right",
                        label: Text("Convert", bundle: .module)) { showConvert = true }
            QuickAction(icon: "creditcard", label: Text("New card", bundle: .module),
                        action: onNewCard)
            QuickAction(icon: "arrow.up.right",
                        label: Text("Send", bundle: .module)) { showSend = true }
        }
        .gutter()
        .padding(.top, 24)
    }

    private var monthRow: some View {
        HStack(alignment: .top, spacing: 0) {
            monthCell("In", store.monthCreditXAF, "arrow.down.left", Brand.credit)
            Rectangle().fill(Brand.hairline).frame(width: 1, height: 34)
            monthCell("Out", store.monthSpendXAF, "arrow.up.right", Brand.debit)
                .padding(.leading, 18)
        }
        .gutter()
        .padding(.vertical, 18)
    }

    private func monthCell(_ label: String.LocalizationValue, _ amount: Int,
                           _ symbol: String, _ tint: Color) -> some View {
        VStack(alignment: .leading, spacing: 5) {
            Text("\(String(localized: label, bundle: .module)) · \(Date.now.formatted(.dateTime.month(.wide)))",
                 bundle: .module)
                .font(.micro).foregroundStyle(Brand.inkMuted)
            HStack(spacing: 5) {
                Image(systemName: symbol).font(.system(size: 11, weight: .bold)).foregroundStyle(tint)
                MoneyText.xaf(amount, size: 17, weight: .medium, unit: false)
            }
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }

    private var cardsSection: some View {
        VStack(alignment: .leading, spacing: 14) {
            SectionHead(title: Text("Cards", bundle: .module),
                        trailing: store.cards.count.formatted(), tappable: true)
                .gutter()
                .padding(.top, 22)

            if store.cards.isEmpty {
                EmptyNote(title: Text("No card yet", bundle: .module),
                          message: Text("Create a card for each use: subscriptions, shopping, ads.",
                                        bundle: .module),
                          actionTitle: Text("Create a card", bundle: .module), action: onNewCard)
                    .gutter()
            } else {
                ScrollView(.horizontal) {
                    HStack(spacing: 11) {
                        ForEach(store.cards) { card in
                            Button { selectedCard = card } label: {
                                VirtualCardView(card: card, compact: true).frame(width: 196)
                            }
                            .buttonStyle(Press())
                        }
                        Button(action: onNewCard) {
                            VStack(spacing: 8) {
                                Image(systemName: "plus").font(.system(size: 16, weight: .medium))
                                Text("New", bundle: .module).font(.microMed)
                            }
                            .foregroundStyle(Brand.inkMuted)
                            .frame(width: 108, height: 124)
                            .background {
                                RoundedRectangle(cornerRadius: 12, style: .continuous)
                                    .strokeBorder(Brand.rule, style: .init(lineWidth: 1, dash: [5, 4]))
                            }
                        }
                        .buttonStyle(Press())
                    }
                    .gutter()
                }
                .scrollIndicators(.hidden)
            }
        }
        .padding(.bottom, 24)
    }

    private var activitySection: some View {
        VStack(alignment: .leading, spacing: 6) {
            SectionHead(title: Text("Activity", bundle: .module), tappable: true).gutter().padding(.top, 22)
            VStack(spacing: 0) {
                ForEach(store.transactions.prefix(5)) { tx in
                    NavigationLink { TransactionDetailView(tx: tx) } label: {
                        TransactionRow(tx: tx)
                    }
                    .buttonStyle(.plain)
                }
            }
            .gutter()
        }
    }
}

#Preview("Home — fr") {
    HomeView(onTopUp: {}, onNewCard: {})
        .environment(Store())
        .environment(\.locale, Locale(identifier: "fr"))
}

#Preview("Home — en") {
    HomeView(onTopUp: {}, onNewCard: {})
        .environment(Store())
        .environment(\.locale, Locale(identifier: "en"))
}
