import Cards
import Convert
import DesignSystem
import Home
import KYC
import Money
import Onboarding
import Settings
import SwiftUI
import TopUp
import Transactions
import WalletStore

/// The single registry of every view in the mockup.
/// It serves two uses: the in-app gallery, and launching straight into one
/// screen (`-screen <key>`) to capture it without replaying the flows.
public struct CatalogEntry: Identifiable {
    public init(
        key: String,
        title: String,
        section: String,
        icon: String,
        tint: Color,
        make: @escaping (Store) -> AnyView
    ) {
        self.key = key
        self.title = title
        self.section = section
        self.icon = icon
        self.tint = tint
        self.make = make
    }

    public let key: String
    public let title: String
    public let section: String
    public let icon: String
    public let tint: Color
    public let make: (Store) -> AnyView
    public var id: String { key }
}

@MainActor
public enum ScreenCatalog {
    public static func entry(_ key: String) -> CatalogEntry? { all.first { $0.key == key } }

    public static var sections: [String] {
        var seen: [String] = []
        for e in all where !seen.contains(e.section) { seen.append(e.section) }
        return seen
    }

    public static let all: [CatalogEntry] = [
        // Onboarding
        e(.init("splash", "Splash", "Accueil et onboarding", "sparkles", Viz.categorical[6])) { _ in
            SplashView(onFinish: {})
        },
        e(.init("welcome", "Carrousel d'accueil", "Accueil et onboarding", "rectangle.stack", Viz.categorical[0])) { _ in
            WelcomeView(onStart: {}, onSignIn: {})
        },
        e(.init("phone", "Numéro de téléphone", "Accueil et onboarding", "phone", Viz.categorical[2])) { _ in
            wrap { PhoneStep(next: {}) }
        },
        e(.init("otp", "Code de vérification", "Accueil et onboarding", "123.rectangle", Viz.categorical[3])) { _ in
            wrap { OTPStep(next: {}) }
        },
        e(.init("passcode", "Création du code secret", "Accueil et onboarding", "lock", Brand.ink)) { _ in
            wrap { PasscodeStep(next: {}) }
        },
        e(.init("faceid", "Face ID", "Accueil et onboarding", "faceid", Viz.categorical[6])) { _ in
            wrap { BiometricStep(next: {}) }
        },
        e(.init("profilestep", "Informations du profil", "Accueil et onboarding", "person.text.rectangle", Viz.categorical[1])) { _ in
            wrap { ProfileStep(next: {}) }
        },

        // KYC
        e(.init("kyc-intro", "Introduction KYC", "Vérification d'identité", "checkmark.shield", Brand.ink)) { _ in
            KYCIntroView(onStart: {}, onLater: {}).padding(.top, 16).page()
        },
        e(.init("kyc-doc", "Choix du document", "Vérification d'identité", "doc.text.magnifyingglass", Viz.categorical[0])) { _ in
            KYCDocumentPickerView(onPick: { _ in }, onBack: {}).padding(.top, 16).page()
        },
        e(.init("kyc-capture", "Capture de la pièce", "Vérification d'identité", "camera.viewfinder", Viz.categorical[3])) { _ in
            KYCCaptureView(mode: .document, onNext: {}, onBack: {}).padding(.top, 16).page()
        },
        e(.init("kyc-selfie", "Selfie de vivacité", "Vérification d'identité",
                "person.crop.circle.badge.checkmark", Viz.categorical[2])) { _ in
            KYCCaptureView(mode: .selfie, onNext: {}, onBack: {}).padding(.top, 16).page()
        },
        e(.init("kyc-review", "Vérification en cours", "Vérification d'identité", "hourglass", Viz.categorical[6])) { _ in
            KYCReviewView(onDone: {}).page()
        },

        // Main app
        e(.init("home", "Accueil / wallet", "Application", "house", Brand.ink)) { _ in
            HomeView(onTopUp: {}, onNewCard: {})
        },
        e(.init("cards", "Liste des cartes", "Application", "creditcard", Viz.categorical[0])) { _ in
            CardsListView(onNewCard: {})
        },
        e(.init("card-detail", "Détail d'une carte", "Application", "creditcard.and.123", Viz.categorical[6])) { s in
            NavigationStack { CardDetailView(card: s.cards[0]) }
        },
        e(.init("card-frozen", "Carte gelée", "Application", "snowflake", Viz.categorical[0])) { s in
            NavigationStack { CardDetailView(card: s.cards[2]) }
        },
        e(.init("card-controls", "Contrôles de carte", "Application", "slider.horizontal.3", Viz.categorical[2])) { s in
            CardControlsView(card: s.cards[0])
        },
        e(.init("activity", "Activité", "Application", "list.bullet.rectangle", Viz.categorical[3])) { _ in
            TransactionsView()
        },
        e(.init("tx-detail", "Détail de transaction", "Application", "doc.text.magnifyingglass", Viz.categorical[1])) { s in
            NavigationStack { TransactionDetailView(tx: s.transactions[0]) }
        },
        e(.init("tx-declined", "Transaction refusée", "Application", "xmark.octagon", Brand.debit)) { s in
            NavigationStack {
                TransactionDetailView(tx: s.transactions.first { $0.status == .declined } ?? s.transactions[0])
            }
        },
        e(.init("insights", "Analyse", "Application", "chart.bar", Viz.categorical[5])) { _ in InsightsView() },
        e(.init("authorization", "Autorisation en direct", "Application", "bolt.badge.clock", Brand.pending)) { s in
            AuthorizationSheet(merchant: "Netflix", category: .streaming,
                               amountUSDCents: 1_099, card: s.cards[0])
        },
        e(.init("subscriptions", "Abonnements récurrents", "Application", "arrow.triangle.2.circlepath", Viz.categorical[2])) { _ in
            NavigationStack { SubscriptionsView() }
        },
        e(.init("lock", "Déverrouillage", "Accueil et onboarding", "lock.iphone", Brand.ink)) { _ in
            LockScreenView()
        },
        e(.init("notifications", "Notifications", "Application", "bell", Viz.categorical[1])) { _ in
            NotificationsView()
        },

        // Flows
        e(.init("create-card", "Créer une carte", "Parcours monétaires", "plus.rectangle.on.rectangle", Brand.ink)) { _ in
            CreateCardFlow()
        },
        e(.init("card-created", "Carte créée", "Parcours monétaires", "checkmark.seal", Viz.categorical[2])) { s in
            NavigationStack { CardCreatedView(card: s.cards[1], onDone: {}) }
        },
        e(.init("topup", "Recharger", "Parcours monétaires", "arrow.down.circle", Viz.categorical[0])) { _ in
            TopUpFlow()
        },
        e(.init("convert", "Convertir FCFA → USD", "Parcours monétaires", "arrow.left.arrow.right", Viz.categorical[6])) { _ in
            ConvertView()
        },
        e(.init("send", "Envoyer de l'argent", "Parcours monétaires", "paperplane", Viz.categorical[3])) { _ in
            SendMoneyView()
        },

        // Settings
        e(.init("settings", "Profil", "Profil et réglages", "person.crop.circle", Brand.ink)) { _ in SettingsView() },
        e(.init("profile", "Informations personnelles", "Profil et réglages", "person.text.rectangle", Viz.categorical[0])) { _ in
            NavigationStack { ProfileView() }
        },
        e(.init("limits", "Plafonds et limites", "Profil et réglages", "gauge.with.dots.needle.50percent", Viz.categorical[2])) { _ in
            NavigationStack { LimitsView() }
        },
        e(.init("security", "Sécurité", "Profil et réglages", "lock.shield", Viz.categorical[6])) { _ in
            NavigationStack { SecurityView() }
        },
        e(.init("devices", "Appareils connectés", "Profil et réglages", "iphone", Viz.categorical[1])) { _ in
            NavigationStack { DevicesView() }
        },
        e(.init("documents", "Documents et relevés", "Profil et réglages", "doc.on.doc", Viz.categorical[3])) { _ in
            NavigationStack { DocumentsView() }
        },
        e(.init("referral", "Parrainage", "Profil et réglages", "gift", Brand.ink)) { _ in
            NavigationStack { ReferralView() }
        },
        e(.init("help", "Centre d'aide", "Profil et réglages", "questionmark.circle", Viz.categorical[5])) { _ in
            NavigationStack { HelpView() }
        },

        // Showcases
        e(.init("empty", "États vides", "États et composants", "tray", Brand.inkMuted)) { _ in
            NavigationStack { EmptyStatesShowcase() }
        },
        e(.init("themes", "Habillages de carte", "États et composants", "paintpalette", Viz.categorical[1])) { _ in
            NavigationStack { CardThemeShowcase() }
        },
        e(.init("components", "Composants", "États et composants", "square.on.circle", Viz.categorical[0])) { _ in
            NavigationStack { ComponentsShowcase() }
        },
        e(.init("gallery", "Galerie", "États et composants", "square.grid.2x2", Brand.ink)) { _ in
            NavigationStack { GalleryView() }
        }
    ]

    /// Identity and presentation of one entry, grouped so the builder below
    /// takes a value plus its view instead of six positional parameters.
    private struct EntryInfo {
        let key: String
        let title: String
        let section: String
        let icon: String
        let tint: Color

        init(_ key: String, _ title: String, _ section: String,
             _ icon: String, _ tint: Color = Brand.ink) {
            self.key = key
            self.title = title
            self.section = section
            self.icon = icon
            self.tint = tint
        }
    }

    private static func e<V: View>(_ info: EntryInfo,
                                   @ViewBuilder _ make: @escaping (Store) -> V) -> CatalogEntry {
        CatalogEntry(key: info.key, title: info.title, section: info.section,
                     icon: info.icon, tint: info.tint) {
            AnyView(make($0))
        }
    }

    private static func wrap<C: View>(@ViewBuilder _ c: () -> C) -> some View {
        c().padding(.top, 16).page()
    }
}
