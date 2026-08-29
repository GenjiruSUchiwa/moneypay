import DesignSystem
import Gallery
import KYC
import Onboarding
import SwiftUI
import WalletStore

enum AppPhase: Equatable { case onboarding, kyc, main }

struct RootView: View {
    let configuration: AppConfiguration
    @Environment(Store.self) private var store
    @State private var phase: AppPhase = .onboarding

    /// `-screen <key>` opens one catalog view directly. Used for review
    /// screenshots; no effect in normal use.
    private var directScreen: CatalogEntry? {
        let args = ProcessInfo.processInfo.arguments
        guard let i = args.firstIndex(of: "-screen"), i + 1 < args.count else { return nil }
        return ScreenCatalog.entry(args[i + 1])
    }

    var body: some View {
        ZStack {
            Brand.bg.ignoresSafeArea()

            if let directScreen {
                directScreen.make(store)
            } else {
                switch phase {
                case .onboarding:
                    OnboardingRoot(
                        dependencies: .init(accounts: configuration.accounts),
                        onFinish: { outcome in
                            switch outcome {
                            case .signedUp(let user):
                                store.user = user
                                phase = .kyc
                            case .signedIn:
                                phase = .main
                            }
                        }
                    )
                case .kyc:
                    KYCFlow(onDone: { phase = .main }, onSkip: { phase = .main })
                case .main:
                    MainTabView()
                }
            }
        }
        .animation(Motion.screen, value: phase)
    }
}
