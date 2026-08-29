import Testing
@testable import Onboarding

@Suite("PasscodeEntry")
struct PasscodeEntryTests {
    @Test("Four digits switch the entry to its confirming phase")
    func fourDigitsSwitchPhase() {
        var entry = PasscodeEntry()

        #expect(entry.setEntry("1234") == nil)
        #expect(entry.phase == .confirming)
        #expect(entry.entry == "")
        #expect(entry.dotsFilled == 0)
    }

    @Test("A matching confirmation reports confirmed")
    func matchingConfirmation() {
        var entry = PasscodeEntry()
        _ = entry.setEntry("1234")

        #expect(entry.setEntry("1234") == .confirmed)
        #expect(entry.first == "1234")
        #expect(entry.isMismatch == false)
    }

    @Test("A wrong confirmation reports a mismatch and clears both entries")
    func wrongConfirmationClearsBoth() {
        var entry = PasscodeEntry()
        _ = entry.setEntry("1234")

        #expect(entry.setEntry("9999") == .mismatch)
        #expect(entry.isMismatch)
        #expect(entry.dotsFilled == 0)
        #expect(entry.phase == .creating)
    }

    @Test("The next edit after a mismatch clears the error")
    func nextEditClearsTheError() {
        var entry = PasscodeEntry()
        _ = entry.setEntry("1234")
        _ = entry.setEntry("9999")

        #expect(entry.setEntry("1") == nil)
        #expect(entry.isMismatch == false)
        #expect(entry.dotsFilled == 1)
    }

    @Test("Non-digits are dropped and a fifth digit is ignored in each phase")
    func nonDigitsAreDropped() {
        var entry = PasscodeEntry()

        _ = entry.setEntry("12a345")
        #expect(entry.first == "1234")
        #expect(entry.phase == .confirming)

        _ = entry.setEntry("12a345")
        #expect(entry.second == "1234")
    }

    @Test("A shorter string deletes — the field's backspace is just another edit")
    func aShorterStringDeletes() {
        var entry = PasscodeEntry()

        _ = entry.setEntry("123")
        _ = entry.setEntry("12")

        #expect(entry.dotsFilled == 2)
        #expect(entry.phase == .creating)
    }
}
