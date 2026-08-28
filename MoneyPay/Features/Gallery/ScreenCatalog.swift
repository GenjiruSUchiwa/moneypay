import SwiftUI

/// Registre unique de toutes les vues de la maquette.
/// Sert deux usages : la galerie dans l'app, et le lancement direct sur un écran
/// (`-screen <clé>`) pour capturer chaque vue sans rejouer les parcours.
struct CatalogEntry: Identifiable {
    let key: String
    let title: String
    let section: String
    let icon: String
    let tint: Color
    let make: (Store) -> AnyView
    var id: String { key }
}

@MainActor
enum ScreenCatalog {
    static func entry(_ key: String) -> CatalogEntry? { all.first { $0.key == key } }

    static var sections: [String] {
        var seen: [String] = []
        for e in all where !seen.contains(e.section) { seen.append(e.section) }
        return seen
    }

    static let all: [CatalogEntry] = [
        // Onboarding
        e("splash", "Splash", "Accueil et onboarding", "sparkles", Viz.categorical[6]) { _ in
            SplashView(onFinish: {})
        },
        e("welcome", "Carrousel d'accueil", "Accueil et onboarding", "rectangle.stack", Viz.categorical[0]) { _ in
            WelcomeView(onStart: {}, onSignIn: {})
        },
        e("phone", "Numéro de téléphone", "Accueil et onboarding", "phone", Viz.categorical[2]) { _ in
            wrap { PhoneStep(next: {}) }
        },
        e("otp", "Code de vérification", "Accueil et onboarding", "123.rectangle", Viz.categorical[3]) { _ in
            wrap { OTPStep(next: {}) }
        },
        e("passcode", "Création du code secret", "Accueil et onboarding", "lock", Brand.ink) { _ in
            wrap { PasscodeStep(next: {}) }
        },
        e("faceid", "Face ID", "Accueil et onboarding", "faceid", Viz.categorical[6]) { _ in
            wrap { BiometricStep(next: {}) }
        },
        e("profilestep", "Informations du profil", "Accueil et onboarding", "person.text.rectangle", Viz.categorical[1]) { _ in
            wrap { ProfileStep(next: {}) }
        },

        // KYC
        e("kyc-intro", "Introduction KYC", "Vérification d'identité", "checkmark.shield", Brand.ink) { _ in
            KYCIntroView(onStart: {}, onLater: {}).padding(.top, 16).page()
        },
        e("kyc-doc", "Choix du document", "Vérification d'identité", "doc.text.magnifyingglass", Viz.categorical[0]) { _ in
            KYCDocumentPickerView(onPick: { _ in }, onBack: {}).padding(.top, 16).page()
        },
        e("kyc-capture", "Capture de la pièce", "Vérification d'identité", "camera.viewfinder", Viz.categorical[3]) { _ in
            KYCCaptureView(mode: .document, onNext: {}, onBack: {}).padding(.top, 16).page()
        },
        e("kyc-selfie", "Selfie de vivacité", "Vérification d'identité", "person.crop.circle.badge.checkmark", Viz.categorical[2]) { _ in
            KYCCaptureView(mode: .selfie, onNext: {}, onBack: {}).padding(.top, 16).page()
        },
        e("kyc-review", "Vérification en cours", "Vérification d'identité", "hourglass", Viz.categorical[6]) { _ in
            KYCReviewView(onDone: {}).page()
        },

        // Application
        e("home", "Accueil / wallet", "Application", "house", Brand.ink) { _ in
            HomeView(onTopUp: {}, onNewCard: {})
        },
        e("cards", "Liste des cartes", "Application", "creditcard", Viz.categorical[0]) { _ in
            CardsListView(onNewCard: {})
        },
        e("card-detail", "Détail d'une carte", "Application", "creditcard.and.123", Viz.categorical[6]) { s in
            NavigationStack { CardDetailView(card: s.cards[0]) }
        },
        e("card-frozen", "Carte gelée", "Application", "snowflake", Viz.categorical[0]) { s in
            NavigationStack { CardDetailView(card: s.cards[2]) }
        },
        e("card-controls", "Contrôles de carte", "Application", "slider.horizontal.3", Viz.categorical[2]) { s in
            CardControlsView(card: s.cards[0])
        },
        e("activity", "Activité", "Application", "list.bullet.rectangle", Viz.categorical[3]) { _ in
            TransactionsView()
        },
        e("tx-detail", "Détail de transaction", "Application", "doc.text.magnifyingglass", Viz.categorical[1]) { s in
            NavigationStack { TransactionDetailView(tx: s.transactions[0]) }
        },
        e("tx-declined", "Transaction refusée", "Application", "xmark.octagon", Brand.debit) { s in
            NavigationStack {
                TransactionDetailView(tx: s.transactions.first { $0.status == .declined } ?? s.transactions[0])
            }
        },
        e("insights", "Analyse", "Application", "chart.bar", Viz.categorical[5]) { _ in InsightsView() },
        e("authorization", "Autorisation en direct", "Application", "bolt.badge.clock", Brand.pending) { s in
            AuthorizationSheet(merchant: "Netflix", category: .streaming,
                               amountUSDCents: 1_099, card: s.cards[0])
        },
        e("subscriptions", "Abonnements récurrents", "Application", "arrow.triangle.2.circlepath", Viz.categorical[2]) { _ in
            NavigationStack { SubscriptionsView() }
        },
        e("lock", "Déverrouillage", "Accueil et onboarding", "lock.iphone", Brand.ink) { _ in
            LockScreenView()
        },
        e("notifications", "Notifications", "Application", "bell", Viz.categorical[1]) { _ in
            NotificationsView()
        },

        // Parcours
        e("create-card", "Créer une carte", "Parcours monétaires", "plus.rectangle.on.rectangle", Brand.ink) { _ in
            CreateCardFlow()
        },
        e("card-created", "Carte créée", "Parcours monétaires", "checkmark.seal", Viz.categorical[2]) { s in
            NavigationStack { CardCreatedView(card: s.cards[1], onDone: {}) }
        },
        e("topup", "Recharger", "Parcours monétaires", "arrow.down.circle", Viz.categorical[0]) { _ in
            TopUpFlow()
        },
        e("convert", "Convertir FCFA → USD", "Parcours monétaires", "arrow.left.arrow.right", Viz.categorical[6]) { _ in
            ConvertView()
        },
        e("send", "Envoyer de l'argent", "Parcours monétaires", "paperplane", Viz.categorical[3]) { _ in
            SendMoneyView()
        },

        // Réglages
        e("settings", "Profil", "Profil et réglages", "person.crop.circle", Brand.ink) { _ in SettingsView() },
        e("profile", "Informations personnelles", "Profil et réglages", "person.text.rectangle", Viz.categorical[0]) { _ in
            NavigationStack { ProfileView() }
        },
        e("limits", "Plafonds et limites", "Profil et réglages", "gauge.with.dots.needle.50percent", Viz.categorical[2]) { _ in
            NavigationStack { LimitsView() }
        },
        e("security", "Sécurité", "Profil et réglages", "lock.shield", Viz.categorical[6]) { _ in
            NavigationStack { SecurityView() }
        },
        e("devices", "Appareils connectés", "Profil et réglages", "iphone", Viz.categorical[1]) { _ in
            NavigationStack { DevicesView() }
        },
        e("documents", "Documents et relevés", "Profil et réglages", "doc.on.doc", Viz.categorical[3]) { _ in
            NavigationStack { DocumentsView() }
        },
        e("referral", "Parrainage", "Profil et réglages", "gift", Brand.ink) { _ in
            NavigationStack { ReferralView() }
        },
        e("help", "Centre d'aide", "Profil et réglages", "questionmark.circle", Viz.categorical[5]) { _ in
            NavigationStack { HelpView() }
        },

        // Vitrines
        e("empty", "États vides", "États et composants", "tray", Brand.inkMuted) { _ in
            NavigationStack { EmptyStatesShowcase() }
        },
        e("themes", "Habillages de carte", "États et composants", "paintpalette", Viz.categorical[1]) { _ in
            NavigationStack { CardThemeShowcase() }
        },
        e("components", "Composants", "États et composants", "square.on.circle", Viz.categorical[0]) { _ in
            NavigationStack { ComponentsShowcase() }
        },
        e("gallery", "Galerie", "États et composants", "square.grid.2x2", Brand.ink) { _ in
            NavigationStack { GalleryView() }
        }
    ]

    private static func e<V: View>(_ key: String, _ title: String, _ section: String,
                                   _ icon: String, _ tint: Color,
                                   @ViewBuilder _ make: @escaping (Store) -> V) -> CatalogEntry {
        CatalogEntry(key: key, title: title, section: section, icon: icon, tint: tint) {
            AnyView(make($0))
        }
    }

    private static func wrap<C: View>(@ViewBuilder _ c: () -> C) -> some View {
        c().padding(.top, 16).page()
    }
}
