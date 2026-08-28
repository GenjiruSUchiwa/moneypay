import DesignSystem
import Gallery
import KYC
import Onboarding
import SwiftUI
import WalletStore

enum AppPhase { case splash, welcome, signup, kyc, main }

struct RootView: View {
    @Environment(Store.self) private var store
    @State private var phase: AppPhase = .splash

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
            case .splash:
                SplashView { phase = .welcome }
            case .welcome:
                WelcomeView(onStart: { phase = .signup }, onSignIn: { phase = .main })
            case .signup:
                SignUpFlow(onDone: { phase = .kyc })
            case .kyc:
                KYCFlow(onDone: { phase = .main }, onSkip: { phase = .main })
            case .main:
                MainTabView()
            }
            }
        }
        .animation(.easeInOut(duration: 0.35), value: phase)
    }
}
