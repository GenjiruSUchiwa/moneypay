import SwiftUI

struct HomeView: View {
    var onTopUp: () -> Void
    var onNewCard: () -> Void

    @Environment(Store.self) private var store
    @State private var showNotifications = false
    @State private var showConvert = false
    @State private var showSend = false
    @State private var selectedCard: VirtualCard?

    var body: some View {
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

    // MARK: En-tête

    private var topBar: some View {
        HStack(spacing: 10) {
            NavigationLink { SettingsView() } label: {
                HStack(spacing: 8) {
                    Text(store.user.initials)
                        .font(.system(size: 12, weight: .semibold))
                        .foregroundStyle(Brand.onInk)
                        .frame(width: 26, height: 26)
                        .background(Brand.inkFill, in: .rect(cornerRadius: 7, style: .continuous))
                    Text(store.user.fullName).font(.subMed).foregroundStyle(Brand.ink)
                    Image(systemName: "chevron.down")
                        .font(.system(size: 9, weight: .bold))
                        .foregroundStyle(Brand.inkFaint)
                }
            }
            .buttonStyle(.plain)
            Spacer()
            Button { Haptic.tap(); showNotifications = true } label: {
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

    // MARK: Solde

    private var balance: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack(spacing: 7) {
                Eyebrow(text: "Solde disponible")
                Button {
                    Haptic.tap()
                    withAnimation(.easeOut(duration: 0.2)) { store.hiddenBalance.toggle() }
                } label: {
                    Image(systemName: store.hiddenBalance ? "eye.slash" : "eye")
                        .font(.system(size: 11))
                        .foregroundStyle(Brand.inkFaint)
                }
            }

            Group {
                if store.hiddenBalance {
                    Text("••• •••").font(.system(size: 40, weight: .semibold))
                        .foregroundStyle(Brand.ink)
                } else {
                    MoneyText.xaf(store.balanceXAF, size: 40)
                }
            }
            .padding(.top, 8)

            HStack(spacing: 5) {
                Image(systemName: "arrow.left.arrow.right").font(.system(size: 10, weight: .semibold))
                Text("≈ \(Fmt.usd(store.usdEquivalentCents)) dépensables en carte")
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
            QuickAction(icon: "plus", label: "Recharger", emphasis: true, action: onTopUp)
            QuickAction(icon: "arrow.left.arrow.right", label: "Convertir") { showConvert = true }
            QuickAction(icon: "creditcard", label: "Nouvelle carte", action: onNewCard)
            QuickAction(icon: "arrow.up.right", label: "Envoyer") { showSend = true }
        }
        .gutter()
        .padding(.top, 24)
    }

    // MARK: Mois

    private var monthRow: some View {
        HStack(alignment: .top, spacing: 0) {
            monthCell("Entrées", store.monthCreditXAF, "arrow.down.left", Brand.credit)
            Rectangle().fill(Brand.hairline).frame(width: 1, height: 34)
            monthCell("Sorties", store.monthSpendXAF, "arrow.up.right", Brand.debit)
                .padding(.leading, 18)
        }
        .gutter()
        .padding(.vertical, 18)
    }

    private func monthCell(_ label: String, _ amount: Int, _ symbol: String, _ tint: Color) -> some View {
        VStack(alignment: .leading, spacing: 5) {
            Text("\(label) · août").font(.micro).foregroundStyle(Brand.inkMuted)
            HStack(spacing: 5) {
                Image(systemName: symbol).font(.system(size: 11, weight: .bold)).foregroundStyle(tint)
                MoneyText.xaf(amount, size: 17, weight: .medium, unit: false)
            }
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }

    // MARK: Cartes

    private var cardsSection: some View {
        VStack(alignment: .leading, spacing: 14) {
            SectionHead(title: "Cartes", trailing: "\(store.cards.count)", tappable: true)
                .gutter()
                .padding(.top, 22)

            if store.cards.isEmpty {
                EmptyNote(title: "Aucune carte",
                          message: "Créez une carte dédiée à chaque usage : abonnements, achats, publicité.",
                          actionTitle: "Créer une carte", action: onNewCard)
                    .gutter()
            } else {
                ScrollView(.horizontal) {
                    HStack(spacing: 11) {
                        ForEach(store.cards) { card in
                            Button { Haptic.tap(); selectedCard = card } label: {
                                VirtualCardView(card: card, compact: true).frame(width: 196)
                            }
                            .buttonStyle(Press())
                        }
                        Button { Haptic.tap(); onNewCard() } label: {
                            VStack(spacing: 8) {
                                Image(systemName: "plus").font(.system(size: 16, weight: .medium))
                                Text("Nouvelle").font(.microMed)
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

    // MARK: Activité

    private var activitySection: some View {
        VStack(alignment: .leading, spacing: 6) {
            SectionHead(title: "Activité", tappable: true).gutter().padding(.top, 22)
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
