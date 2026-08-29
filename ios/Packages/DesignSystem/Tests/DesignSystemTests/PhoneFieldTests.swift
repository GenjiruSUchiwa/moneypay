import Testing
@testable import DesignSystem

@Suite("PhoneField")
struct PhoneFieldTests {
    @Test("Keeps digits and drops grouping spaces")
    func keepsDigitsAndDropsGroupingSpaces() {
        #expect(PhoneField.nationalDigits(from: "6 90 12 34 56") == "690123456")
    }

    @Test("Drops letters, signs and punctuation")
    func dropsLettersSignsAndPunctuation() {
        #expect(PhoneField.nationalDigits(from: "+237-6xx") == "2376")
        #expect(PhoneField.nationalDigits(from: "") == "")
    }
}
