import Foundation
import Testing
@testable import WalletStore

/// The demo data the previews and the app render is copy, so it goes through
/// the catalog like any other string. A missing `defaultLocalization` or
/// `resources:` entry would fall back to the English key rather than fail.
@Suite("WalletStore — string catalog")
struct LocalizationTests {
    private func french(_ key: String.LocalizationValue) -> String {
        String(localized: LocalizedStringResource(key,
                                                  locale: Locale(identifier: "fr"),
                                                  bundle: .atURL(Bundle.module.bundleURL)))
    }

    @Test("a top-up method name resolves in French")
    func topUpMethodNameIsTranslated() {
        #expect(french("Bank transfer") == "Virement bancaire")
    }

    @Test("a notification body keeps its placeholder")
    func notificationBodyInterpolates() {
        #expect(french("%@ from MTN Mobile Money") == "%@ depuis MTN Mobile Money")
    }
}
