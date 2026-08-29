import Testing
@testable import DesignSystem

@Test("PasscodeDots keeps ASCII digits and clamps to the dot count")
func passcodeSanitizeKeepsASCIIDigitsAndClampsLength() {
    #expect(PasscodeDots.sanitize("1 2 3 4 5", total: 4) == "1234")
    #expect(PasscodeDots.sanitize("12ab", total: 4) == "12")
    #expect(PasscodeDots.sanitize("١٢٣", total: 4) == "")
    #expect(PasscodeDots.sanitize("1234", total: 0) == "")
}
