import Foundation
import Testing
@testable import Cards

/// A catalog is only wired up when `Bundle.module` resolves it: without
/// `defaultLocalization` or the `resources:` entry the lookup falls through to
/// the English key instead of failing, so the check has to be explicit.
@Suite("Cards — string catalog")
struct LocalizationTests {
    /// The `locale:` on `String(localized:)` steers interpolations only — the
    /// localization itself is chosen through `LocalizedStringResource`.
    private func french(_ key: String.LocalizationValue) -> String {
        String(localized: LocalizedStringResource(key,
                                                  locale: Locale(identifier: "fr"),
                                                  bundle: .atURL(Bundle.module.bundleURL)))
    }

    @Test("the card screens read in French")
    func cardCopyIsTranslated() {
        #expect(french("Create the card") == "Créer la carte")
        #expect(french("Allowed countries") == "Pays autorisés")
    }
}
