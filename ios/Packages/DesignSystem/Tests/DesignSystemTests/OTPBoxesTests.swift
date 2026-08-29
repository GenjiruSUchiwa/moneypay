import Testing
@testable import DesignSystem

@Test("OTPBoxes keeps ASCII digits and clamps length")
func otpSanitizeKeepsASCIIDigitsAndClampsLength() {
    #expect(OTPBoxes.sanitize("12 34 56 78", length: 6) == "123456")
    #expect(OTPBoxes.sanitize("12ab", length: 6) == "12")
    #expect(OTPBoxes.sanitize("١٢٣", length: 6) == "")
}
