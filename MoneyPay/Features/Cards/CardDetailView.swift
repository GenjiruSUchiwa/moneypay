import SwiftUI

struct CardDetailView: View {
    var card: VirtualCard
    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    @State private var revealed = false
    @State private var toastMsg: Toast?
    @State private var confirmFreeze = false
    @State private var confirmDelete = false
    @State private var showControls = false

    private var live: VirtualCard { store.cards.first { $0.id == card.id } ?? card }

    var body: some View {
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
                    Row(icon: "trash", title: "Supprimer cette carte", destructive: true)
                }
                .buttonStyle(.plain)
                .gutter()
            }
            .padding(.bottom, 34)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle(live.label)
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Menu {
                    Button("Renommer la carte", systemImage: "pencil") {}
                    Button("Ajouter à Apple Wallet", systemImage: "wallet.pass") {}
                    Button("Voir le relevé", systemImage: "doc.text") {}
                    Divider()
                    Button("Supprimer la carte", systemImage: "trash", role: .destructive) {
                        confirmDelete = true
                    }
                } label: {
                    Image(systemName: "ellipsis").font(.system(size: 15, weight: .semibold))
                }
            }
        }
        .toast($toastMsg)
        .sheet(isPresented: $showControls) { CardControlsView(card: live) }
        .confirmationDialog(live.isFrozen ? "Dégeler cette carte ?" : "Geler cette carte ?",
                            isPresented: $confirmFreeze, titleVisibility: .visible) {
            Button(live.isFrozen ? "Dégeler" : "Geler") {
                let wasFrozen = live.isFrozen
                store.toggleFreeze(live)
                toastMsg = Toast(text: wasFrozen ? "Carte dégelée" : "Carte gelée",
                                 icon: wasFrozen ? "checkmark" : "snowflake")
            }
            Button("Annuler", role: .cancel) {}
        } message: {
            Text("Tant que la carte est gelée, chaque autorisation est refusée. Les abonnements rattachés échoueront.")
        }
        .confirmationDialog("Supprimer définitivement ?", isPresented: $confirmDelete,
                            titleVisibility: .visible) {
            Button("Supprimer la carte", role: .destructive) { store.deleteCard(live); dismiss() }
            Button("Annuler", role: .cancel) {}
        } message: {
            Text("Les abonnements rattachés cesseront d'être prélevés.")
        }
    }

    private var actions: some View {
        HStack(spacing: 4) {
            QuickAction(icon: revealed ? "eye.slash" : "eye",
                        label: revealed ? "Masquer" : "Détails") {
                withAnimation(.spring(response: 0.32, dampingFraction: 0.86)) { revealed.toggle() }
            }
            QuickAction(icon: live.isFrozen ? "sun.max" : "snowflake",
                        label: live.isFrozen ? "Dégeler" : "Geler") { confirmFreeze = true }
            QuickAction(icon: "slider.horizontal.3", label: "Contrôles") { showControls = true }
            QuickAction(icon: "wallet.pass", label: "Wallet") {
                toastMsg = Toast(text: "Ajout à Apple Wallet simulé", icon: "wallet.pass")
            }
        }
        .gutter()
        .padding(.top, 22)
    }

    // MARK: Secrets

    private var secrets: some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: "Détails de la carte").gutter().padding(.top, 22).padding(.bottom, 4)
            VStack(spacing: 0) {
                copyRow("Titulaire", store.user.fullName.uppercased(), mono: false)
                Rule()
                copyRow("Numéro", live.pan.chunked())
                Rule()
                copyRow("Expiration", live.expiry)
                Rule()
                copyRow("CVV", live.cvv)
            }
            .gutter()
            Text("MoneyPay ne vous demandera jamais ces informations.")
                .font(.micro).foregroundStyle(Brand.inkFaint)
                .gutter().padding(.top, 10).padding(.bottom, 20)
        }
    }

    private func copyRow(_ label: String, _ value: String, mono: Bool = true) -> some View {
        Button {
            UIPasteboard.general.string = value
            Haptic.success(); toastMsg = Toast(text: "\(label) copié", icon: "doc.on.doc")
        } label: {
            HStack {
                Text(label).font(.body).foregroundStyle(Brand.inkMuted)
                Spacer()
                Text(value).font(mono ? .dataMono : .body).foregroundStyle(Brand.ink)
                Image(systemName: "doc.on.doc").font(.system(size: 12))
                    .foregroundStyle(Brand.inkFaint)
            }
            .padding(.vertical, Metric.rowVertical)
            .contentShape(.rect)
        }
        .buttonStyle(.plain)
    }

    // MARK: Plafond

    private var limit: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack {
                Eyebrow(text: "Plafond mensuel")
                Spacer()
                Button { showControls = true } label: {
                    Text("Modifier").font(.microMed).foregroundStyle(Brand.mark)
                }
            }
            .gutter().padding(.top, 22)

            HStack(alignment: .firstTextBaseline, spacing: 7) {
                MoneyText.usd(live.spentUSDCents, size: 26)
                Text(live.monthlyLimitUSDCents.map { "sur \(Fmt.usd($0))" } ?? "sans plafond")
                    .font(.sub).foregroundStyle(Brand.inkMuted)
            }
            .gutter().padding(.top, 10)

            if let l = live.monthlyLimitUSDCents {
                Meter(value: live.usage, tint: live.usage > 0.85 ? Brand.debit : Brand.ink)
                    .gutter().padding(.top, 12)
                HStack {
                    Text("Reste \(Fmt.usd(max(0, l - live.spentUSDCents)))")
                        .font(.micro).foregroundStyle(Brand.inkMuted)
                    Spacer()
                    Text("Réinitialisé le 1er").font(.micro).foregroundStyle(Brand.inkFaint)
                }
                .gutter().padding(.top, 7)
            }

            if live.declineCount > 0 {
                HStack(spacing: 8) {
                    Image(systemName: "exclamationmark.triangle.fill")
                        .font(.system(size: 12)).foregroundStyle(Brand.pending)
                    Text("\(live.declineCount) refus ce mois · la carte se bloque à 3")
                        .font(.sub).foregroundStyle(Brand.inkMuted)
                }
                .gutter().padding(.top, 14)
            }
        }
        .padding(.bottom, 20)
    }

    // MARK: Contrôles

    private var controls: some View {
        VStack(spacing: 0) {
            Row(icon: "globe", title: "Paiements en ligne") {
                RowValue(text: live.onlineAllowed ? "Autorisés" : "Bloqués",
                         tint: live.onlineAllowed ? Brand.credit : Brand.debit)
            }
            Rule(inset: 51)
            Row(icon: "arrow.triangle.2.circlepath", title: "Abonnements récurrents") {
                RowValue(text: live.subscriptionsAllowed ? "Autorisés" : "Bloqués",
                         tint: live.subscriptionsAllowed ? Brand.credit : Brand.debit)
            }
            Rule(inset: 51)
            Button { showControls = true } label: {
                Row(icon: "slider.horizontal.3", title: "Tous les contrôles", chevron: true)
            }
            .buttonStyle(.plain)
        }
        .gutter()
    }

    // MARK: Transactions

    private var transactions: some View {
        let txs = store.transactions(for: live.id)
        return VStack(alignment: .leading, spacing: 2) {
            SectionHead(title: "Transactions", trailing: txs.isEmpty ? nil : "\(txs.count)",
                        tappable: !txs.isEmpty)
                .gutter().padding(.top, 22)
            if txs.isEmpty {
                EmptyNote(title: "Aucune transaction",
                          message: "Les paiements effectués avec cette carte apparaîtront ici.")
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
