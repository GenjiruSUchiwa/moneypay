import Foundation
import Testing
@testable import Convert

/// A catalog is only wired up when `Bundle.module` resolves it: without
/// `defaultLocalization` or the `resources:` entry the lookup falls through to
/// the English key instead of failing, so the check has to be explicit.
@Suite("Convert — string catalog")
struct LocalizationTests {
    /// The `locale:` on `String(localized:)` steers interpolations only — the
    /// localization itself is chosen through `LocalizedStringResource`.
    private func french(_ key: String.LocalizationValue) -> String {
        String(localized: LocalizedStringResource(key,
                                                  locale: Locale(identifier: "fr"),
                                                  bundle: .atURL(Bundle.module.bundleURL)))
    }

    @Test("the convert and send screens read in French")
    func convertCopyIsTranslated() {
        #expect(french("Interbank rate") == "Taux interbancaire")
        #expect(french("Money sent") == "Argent envoyé")
    }
}
