import Testing
@testable import DesignSystem

@Suite("FlagMark")
struct FlagMarkTests {
    @Test("Lists every supported country")
    func listsSupportedCountries() {
        #expect(FlagMark.Country.allCases.map(\.rawValue) == ["cm", "ci", "sn", "ga", "cd", "bj"])
    }

    @Test("Returns uppercase ISO region codes")
    func returnsUppercaseRegionCodes() {
        #expect(FlagMark.Country.cm.regionCode == "CM")
        #expect(FlagMark.Country.ci.regionCode == "CI")
        #expect(FlagMark.Country.sn.regionCode == "SN")
        #expect(FlagMark.Country.ga.regionCode == "GA")
        #expect(FlagMark.Country.cd.regionCode == "CD")
        #expect(FlagMark.Country.bj.regionCode == "BJ")
    }
}
