import SwiftUI

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
        }
        .tint(Brand.ink)
        .sheet(isPresented: $showTopUp) { TopUpFlow() }
        .sheet(isPresented: $showCreateCard) { CreateCardFlow() }
    }
}
