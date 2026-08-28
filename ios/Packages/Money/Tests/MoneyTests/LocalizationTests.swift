import Foundation
import Testing
@testable import Money

/// The domain vocabulary the UI shows is a catalog lookup now, not a French
/// literal. If `defaultLocalization` or the `resources:` entry ever goes
/// missing, the lookup falls through to the English key instead of failing —
/// which is exactly what this asserts against.
@Suite("Money — string catalog")
struct LocalizationTests {
    /// The localization is chosen by `LocalizedStringResource`; the `locale:`
    /// on `String(localized:)` only steers interpolations.
    private func french(_ resource: LocalizedStringResource) -> String {
        var pinned = resource
        pinned.locale = Locale(identifier: "fr")
        return String(localized: pinned)
    }

    @Test("transaction states read in French")
    func statusLabelsAreTranslated() {
        #expect(french(TxStatus.approved.label) == "Réussi")
        #expect(french(TxStatus.declined.label) == "Refusé")
    }

    @Test("kinds and categories read in French")
    func kindAndCategoryLabelsAreTranslated() {
        #expect(french(TxKind.topUp.label) == "Rechargement")
        #expect(french(TxCategory.streaming.label) == "Divertissement")
    }
}
