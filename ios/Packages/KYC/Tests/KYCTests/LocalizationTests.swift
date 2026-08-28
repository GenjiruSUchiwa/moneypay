import Foundation
import Testing
@testable import KYC

/// A catalog is only wired up when `Bundle.module` resolves it: without
/// `defaultLocalization` or the `resources:` entry the lookup falls through to
/// the English key instead of failing, so the check has to be explicit.
@Suite("KYC — string catalog")
struct LocalizationTests {
    /// The `locale:` on `String(localized:)` steers interpolations only — the
    /// localization itself is chosen through `LocalizedStringResource`.
    private func french(_ key: String.LocalizationValue) -> String {
        String(localized: LocalizedStringResource(key,
                                                  locale: Locale(identifier: "fr"),
                                                  bundle: .atURL(Bundle.module.bundleURL)))
    }

    @Test("the KYC steps read in French")
    func kycCopyIsTranslated() {
        #expect(french("Start the verification") == "Commencer la vérification")
        #expect(french("Identity verified") == "Identité vérifiée")
    }
}
