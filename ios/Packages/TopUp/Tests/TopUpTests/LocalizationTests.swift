import Foundation
import Testing
@testable import TopUp

/// A catalog is only wired up when `Bundle.module` resolves it: without
/// `defaultLocalization` or the `resources:` entry the lookup falls through to
/// the English key instead of failing, so the check has to be explicit.
@Suite("TopUp — string catalog")
struct LocalizationTests {
    /// The `locale:` on `String(localized:)` steers interpolations only — the
    /// localization itself is chosen through `LocalizedStringResource`.
    private func french(_ key: String.LocalizationValue) -> String {
        String(localized: LocalizedStringResource(key,
                                                  locale: Locale(identifier: "fr"),
                                                  bundle: .atURL(Bundle.module.bundleURL)))
    }

    @Test("the top-up flow reads in French")
    func topUpCopyIsTranslated() {
        #expect(french("Confirm the top-up") == "Confirmer le rechargement")
        #expect(french("1 to 2 business days") == "1 à 2 jours ouvrés")
    }
}
