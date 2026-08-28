import Cards
import DesignSystem
import Gallery
import Home
import Settings
import SwiftUI
import TopUp
import Transactions
import WalletStore

struct MainTabView: View {
    @Environment(Store.self) private var store
    @State private var tab = 0
    @State private var showTopUp = false
    @State private var showCreateCard = false

    var body: some View {
        TabView(selection: $tab) {
            Tab("Accueil", systemImage: "house.fill", value: 0) {
                HomeView(onTopUp: { showTopUp = true }, onNewCard: { showCreateCard = true })
            }
            Tab("Cartes", systemImage: "creditcard.fill", value: 1) {
                CardsListView(onNewCard: { showCreateCard = true })
            }
            Tab("Activité", systemImage: "list.bullet.rectangle.fill", value: 2) {
                TransactionsView()
            }
            Tab("Analyse", systemImage: "chart.pie.fill", value: 3) {
                InsightsView()
            }
            Tab("Profil", systemImage: "person.fill", value: 4) {
                SettingsView()
            }
            // The screen gallery is a review tool, not a product feature. It
            // lives here because the Gallery package references every screen,
            // so only the composition root is allowed to depend on it.
            Tab("Galerie", systemImage: "square.grid.2x2", value: 5) {
                GalleryView()
            }
        }
        .tint(Brand.ink)
        .sheet(isPresented: $showTopUp) { TopUpFlow() }
        .sheet(isPresented: $showCreateCard) { CreateCardFlow() }
    }
}
