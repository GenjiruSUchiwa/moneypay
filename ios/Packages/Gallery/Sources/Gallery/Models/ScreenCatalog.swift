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

    public static let all: [CatalogEntry] = authenticationPreviews + [
        e(.init("splash", "Splash", "Onboarding", "sparkles", Viz.categorical[6])) { _ in
            OnboardingRoot(dependencies: .preview, stage: .splash, onFinish: { _ in })
        },
        e(.init("welcome", "Welcome carousel", "Onboarding", "rectangle.stack", Viz.categorical[0])) { _ in
            OnboardingRoot(dependencies: .preview, stage: .welcome, onFinish: { _ in })
        },
        e(.init("phone", "Phone number", "Onboarding", "phone", Viz.categorical[2])) { _ in
            OnboardingRoot(dependencies: .preview, stage: .signUp(.phone), onFinish: { _ in })
        },
        e(.init("otp", "Verification code", "Onboarding", "123.rectangle", Viz.categorical[3])) { _ in
            OnboardingRoot(dependencies: .preview, stage: .signUp(.code), onFinish: { _ in })
        },
        e(.init("passcode", "Passcode creation", "Onboarding", "lock", Brand.ink)) { _ in
            OnboardingRoot(dependencies: .preview, stage: .signUp(.passcode), onFinish: { _ in })
        },
        e(.init("faceid", "Face ID", "Onboarding", "faceid", Viz.categorical[6])) { _ in
            OnboardingRoot(dependencies: .preview, stage: .signUp(.biometrics), onFinish: { _ in })
        },
        e(.init("profilestep", "Profile details", "Onboarding", "person.text.rectangle", Viz.categorical[1])) { _ in
            OnboardingRoot(dependencies: .preview, stage: .signUp(.profile), onFinish: { _ in })
        },

        e(.init("kyc-intro", "KYC introduction", "Identity verification", "checkmark.shield", Brand.ink)) { _ in
            NavigationStack { KYCIntroView(onStart: {}, onLater: {}).padding(.top, 16).page() }
        },
        e(.init("kyc-doc", "Document choice", "Identity verification", "doc.text.magnifyingglass", Viz.categorical[0])) { _ in
            NavigationStack { KYCDocumentPickerView(onPick: { _ in }, onBack: {}).page() }
        },
        e(.init("kyc-capture", "Document capture", "Identity verification", "camera.viewfinder", Viz.categorical[3])) { _ in
            NavigationStack { KYCCaptureView(mode: .document, onNext: {}, onBack: {}).page() }
        },
        e(.init("kyc-selfie", "Liveness selfie", "Identity verification",
                "person.crop.circle.badge.checkmark", Viz.categorical[2])) { _ in
            NavigationStack { KYCCaptureView(mode: .selfie, onNext: {}, onBack: {}).page() }
        },
        e(.init("kyc-review", "Verification in progress", "Identity verification", "hourglass", Viz.categorical[6])) { _ in
            NavigationStack { KYCReviewView(onDone: {}).page() }
        },

        e(.init("home", "Home / wallet", "App", "house", Brand.ink)) { _ in
            HomeView(onTopUp: {}, onNewCard: {})
        },
        e(.init("cards", "Card list", "App", "creditcard", Viz.categorical[0])) { _ in
            CardsListView(onNewCard: {})
        },
        e(.init("card-detail", "Card detail", "App", "creditcard.and.123", Viz.categorical[6])) { s in
            NavigationStack { CardDetailView(card: s.cards[0]) }
        },
        e(.init("card-frozen", "Frozen card", "App", "snowflake", Viz.categorical[0])) { s in
            NavigationStack { CardDetailView(card: s.cards[2]) }
        },
        e(.init("card-controls", "Card controls", "App", "slider.horizontal.3", Viz.categorical[2])) { s in
            CardControlsView(card: s.cards[0])
        },
        e(.init("activity", "Activity", "App", "list.bullet.rectangle", Viz.categorical[3])) { _ in
            TransactionsView()
        },
        e(.init("tx-detail", "Transaction detail", "App", "doc.text.magnifyingglass", Viz.categorical[1])) { s in
            NavigationStack { TransactionDetailView(tx: s.transactions[0]) }
        },
        e(.init("tx-declined", "Declined transaction", "App", "xmark.octagon", Brand.debit)) { s in
            NavigationStack {
                TransactionDetailView(tx: s.transactions.first { $0.status == .declined } ?? s.transactions[0])
            }
        },
        e(.init("insights", "Analyse", "App", "chart.bar", Viz.categorical[5])) { _ in InsightsView() },
        e(.init("authorization", "Live authorization", "App", "bolt.badge.clock", Brand.pending)) { s in
            AuthorizationSheet(merchant: "Netflix", category: .streaming,
                               amountUSDCents: 1_099, card: s.cards[0])
        },
        e(.init("subscriptions", "Recurring subscriptions", "App", "arrow.triangle.2.circlepath", Viz.categorical[2])) { _ in
            NavigationStack { SubscriptionsView() }
        },
        e(.init("lock", "Unlock", "Onboarding", "lock.iphone", Brand.ink)) { _ in
            LockScreenView()
        },
        e(.init("notifications", "Notifications", "App", "bell", Viz.categorical[1])) { _ in
            NotificationsView()
        },

        e(.init("create-card", "Create a card", "Money flows", "plus.rectangle.on.rectangle", Brand.ink)) { _ in
            CreateCardFlow()
        },
        e(.init("card-created", "Card created", "Money flows", "checkmark.seal", Viz.categorical[2])) { s in
            NavigationStack { CardCreatedView(card: s.cards[1], onDone: {}) }
        },
        e(.init("topup", "Top up", "Money flows", "arrow.down.circle", Viz.categorical[0])) { _ in
            TopUpFlow()
        },
        e(.init("convert", "Convert FCFA to USD", "Money flows", "arrow.left.arrow.right", Viz.categorical[6])) { _ in
            ConvertView()
        },
        e(.init("send", "Send money", "Money flows", "paperplane", Viz.categorical[3])) { _ in
            SendMoneyView()
        },

        e(.init("settings", "Profile", "Profile and settings", "person.crop.circle", Brand.ink)) { _ in SettingsView() },
        e(.init("profile", "Personal details", "Profile and settings", "person.text.rectangle", Viz.categorical[0])) { _ in
            NavigationStack { ProfileView() }
        },
        e(.init("limits", "Caps and limits", "Profile and settings", "gauge.with.dots.needle.50percent", Viz.categorical[2])) { _ in
            NavigationStack { LimitsView() }
        },
        e(.init("security", "Security", "Profile and settings", "lock.shield", Viz.categorical[6])) { _ in
            NavigationStack { SecurityView() }
        },
        e(.init("devices", "Connected devices", "Profile and settings", "iphone", Viz.categorical[1])) { _ in
            NavigationStack { DevicesView() }
        },
        e(.init("documents", "Documents and statements", "Profile and settings", "doc.on.doc", Viz.categorical[3])) { _ in
            NavigationStack { DocumentsView() }
        },
        e(.init("referral", "Referrals", "Profile and settings", "gift", Brand.ink)) { _ in
            NavigationStack { ReferralView() }
        },
        e(.init("help", "Help centre", "Profile and settings", "questionmark.circle", Viz.categorical[5])) { _ in
            NavigationStack { HelpView() }
        },

        e(.init("empty", "Empty states", "States and components", "tray", Brand.inkMuted)) { _ in
            NavigationStack { EmptyStatesShowcase() }
        },
        e(.init("themes", "Card finishes", "States and components", "paintpalette", Viz.categorical[1])) { _ in
            NavigationStack { CardThemeShowcase() }
        },
        e(.init("components", "Components", "States and components", "square.on.circle", Viz.categorical[0])) { _ in
            NavigationStack { ComponentsShowcase() }
        },
        e(.init("gallery", "Gallery", "States and components", "square.grid.2x2", Brand.ink)) { _ in
            NavigationStack { GalleryView() }
        }
    ]

    private static let authenticationPreviews: [CatalogEntry] = OnboardingRoot.PreviewScenario.allCases.map { scenario in
        let title = scenario.rawValue.replacingOccurrences(of: "-", with: " ").capitalized
        return e(.init(scenario.rawValue, title, "Authentication states", "person.badge.key")) { _ in
            OnboardingRoot(dependencies: .preview, stage: .preview(scenario), onFinish: { _ in })
        }
    }

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
}
