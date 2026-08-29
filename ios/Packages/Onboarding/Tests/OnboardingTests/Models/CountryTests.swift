import Testing
@testable import Onboarding

@Suite("Country")
struct CountryTests {
    @Test("Grouping matches the prototype's fmtPhoneDigits")
    func groupingMatchesPrototype() {
        #expect(Country.cameroon.grouped("699123456") == "6 99 12 34 56")
    }

    @Test("A partial number groups as it is typed")
    func partialNumberGroups() {
        #expect(Country.cameroon.grouped("6991") == "6 99 1")
        #expect(Country.cameroon.grouped("") == "")
    }

    /// Gabon's mask is eight characters long, so the prototype's grouping leaves a lone
    /// trailing one: `X XX XX XX X`. The issue's table dropped it.
    @Test("The placeholder mask follows the country's digit count",
          arguments: [("CM", "6 XX XX XX XX"), ("CI", "X XX XX XX XXX"), ("GA", "X XX XX XX X")])
    func placeholderFollowsDigitCount(id: String, expected: String) throws {
        let country = try #require(Country.supported.first { $0.id == id })

        #expect(country.placeholder == expected)
    }

    @Test("The six supported countries carry the prototype's dial codes and lengths",
          arguments: [("CM", "+237", 9), ("CI", "+225", 10), ("SN", "+221", 9),
                      ("GA", "+241", 8), ("CD", "+243", 9), ("BJ", "+229", 8)])
    func supportedCountriesMatchPrototype(id: String, dialCode: String, digitCount: Int) throws {
        let country = try #require(Country.supported.first { $0.id == id })

        #expect(country.dialCode == dialCode)
        #expect(country.digitCount == digitCount)
        #expect(country.flag.regionCode == id)
        #expect(Country.supported.first == .cameroon)
    }

    @Test("A country name falls back to its region code and is never empty")
    func localizedNameIsNeverEmpty() {
        for country in Country.supported {
            #expect(!country.localizedName.isEmpty)
        }
    }
}
