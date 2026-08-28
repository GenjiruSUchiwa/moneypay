import Foundation
import Testing
@testable import Settings

/// A catalog is only wired up when `Bundle.module` resolves it: without
/// `defaultLocalization` or the `resources:` entry the lookup falls through to
/// the English key instead of failing, so the check has to be explicit.
@Suite("Settings — string catalog")
struct LocalizationTests {
    /// The `locale:` on `String(localized:)` steers interpolations only — the
    /// localization itself is chosen through `LocalizedStringResource`.
    private func french(_ key: String.LocalizationValue) -> String {
        String(localized: LocalizedStringResource(key,
                                                  locale: Locale(identifier: "fr"),
                                                  bundle: .atURL(Bundle.module.bundleURL)))
    }

    @Test("the settings screens read in French")
    func settingsCopyIsTranslated() {
        #expect(french("Your current caps") == "Vos plafonds actuels")
        #expect(french("Advisers online") == "Conseillers en ligne")
    }
}
