import Testing
@testable import DesignSystem

@Test("AmountEntry keeps ASCII digits and clamps to maxDigits")
func amountSanitizeKeepsASCIIDigitsAndClampsLength() {
    #expect(AmountEntry.sanitize("25 000", maxDigits: 8) == "25000")
    #expect(AmountEntry.sanitize("12.50", maxDigits: 8) == "1250")
    #expect(AmountEntry.sanitize("123456789", maxDigits: 8) == "12345678")
    #expect(AmountEntry.sanitize("٢٥", maxDigits: 8) == "")
}
