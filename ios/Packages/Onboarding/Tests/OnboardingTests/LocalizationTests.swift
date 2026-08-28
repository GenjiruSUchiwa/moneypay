import Foundation
import Testing
@testable import Onboarding

/// A catalog is only wired up when `Bundle.module` resolves it: without
/// `defaultLocalization` or the `resources:` entry the lookup falls through to
/// the English key instead of failing, so the check has to be explicit.
@Suite("Onboarding — string catalog")
struct LocalizationTests {
    /// The `locale:` on `String(localized:)` steers interpolations only — the
    /// localization itself is chosen through `LocalizedStringResource`.
    private func french(_ key: String.LocalizationValue) -> String {
        String(localized: LocalizedStringResource(key,
                                                  locale: Locale(identifier: "fr"),
                                                  bundle: .atURL(Bundle.module.bundleURL)))
    }

    @Test("the sign-up copy reads in French")
    func onboardingCopyIsTranslated() {
        #expect(french("Create my account") == "Créer mon compte")
        #expect(french("Turn on Face ID") == "Activer Face ID")
    }
}
