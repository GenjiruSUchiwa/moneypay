import Foundation
import Testing
@testable import DesignSystem

@Suite("Fmt — locale-aware formatting")
struct FormattingTests {
    private let cameroon = Locale(identifier: "fr_CM")
    private let unitedStates = Locale(identifier: "en_US")

    private func normalized(_ text: String) -> String {
        text.replacingOccurrences(of: "\u{202F}", with: " ")
            .replacingOccurrences(of: "\u{00A0}", with: " ")
    }

    @Test("XAF carries no fraction digits")
    func xafHasNoFractionDigits() {
        let text = normalized(Fmt.xaf(428_500, locale: cameroon))
        #expect(text.hasPrefix("428 500"))
        #expect(!text.contains(","))
    }

    @Test("USD carries exactly two")
    func usdHasTwoFractionDigits() {
        #expect(normalized(Fmt.usd(1_099, locale: cameroon)).contains("10,99"))
        #expect(normalized(Fmt.usd(1_099, locale: unitedStates)).contains("10.99"))
    }

    @Test("grouping follows the caller's locale, not the host's")
    func groupingFollowsTheLocale() {
        #expect(normalized(Fmt.group(428_500, locale: cameroon)) == "428 500")
        #expect(Fmt.group(428_500, locale: unitedStates) == "428,500")
    }

    @Test("a Douala calendar decides what counts as today")
    func relativeDayUsesTheGivenCalendar() {
        var douala = Calendar(identifier: .gregorian)
        douala.timeZone = TimeZone(identifier: "Africa/Douala") ?? .gmt
        #expect(Fmt.relativeDay(.now, calendar: douala) == String(localized: "Today", bundle: .module))
    }
}
