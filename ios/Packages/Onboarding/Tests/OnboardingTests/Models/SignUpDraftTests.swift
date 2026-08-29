import Testing
@testable import Onboarding

@Suite("SignUpDraft")
struct SignUpDraftTests {
    private static func profileDraft() -> SignUpDraft {
        var draft = SignUpDraft()
        draft.phoneDigits = "699123456"
        draft.firstName = "  Aristide "
        draft.lastName = " Mbassi  "
        draft.email = " aristide@example.cm "
        return draft
    }

    @Test("A phone is valid only at exactly the country's digit count",
          arguments: [("69912345", false), ("699123456", true), ("6991234567", false)])
    func phoneIsValidAtExactLength(digits: String, isValid: Bool) {
        var draft = SignUpDraft()
        draft.phoneDigits = digits

        #expect(draft.isPhoneValid == isValid)
    }

    @Test("e164Digits is the dial code without its plus, then the local digits")
    func e164DropsThePlus() {
        var draft = SignUpDraft()
        draft.phoneDigits = "699123456"

        #expect(draft.e164Digits == "237699123456")
    }

    @Test("displayPhone reads as +237 6 99 12 34 56")
    func displayPhoneIsGrouped() {
        var draft = SignUpDraft()
        draft.phoneDigits = "699123456"

        #expect(draft.displayPhone == "+237 6 99 12 34 56")
    }

    @Test("The signup request sends one name field and a phone with no plus")
    func signupRequestIsThePocShape() {
        let request = Self.profileDraft().signupRequest

        #expect(request.name == "Aristide Mbassi")
        #expect(request.phone == "237699123456")
        #expect(request.email == "aristide@example.cm")
    }

    @Test("The user keeps the plus, trims the names and starts unverified")
    func userKeepsThePlus() {
        let user = Self.profileDraft().user

        #expect(user.phone == "+237699123456")
        #expect(user.fullName == "Aristide Mbassi")
        #expect(user.email == "aristide@example.cm")
        #expect(user.kycVerified == false)
    }

    @Test("A profile needs both names and an email with a dot after the at sign",
          arguments: [("Aristide", "Mbassi", "a@example.cm", true),
                      ("", "Mbassi", "a@example.cm", false),
                      ("  ", "Mbassi", "a@example.cm", false),
                      ("Aristide", "", "a@example.cm", false),
                      ("Aristide", "Mbassi", "aexample.cm", false),
                      ("Aristide", "Mbassi", "a@examplecm", false),
                      ("Aristide", "Mbassi", "a@", false)])
    func profileValidity(firstName: String, lastName: String, email: String, isValid: Bool) {
        var draft = SignUpDraft()
        draft.firstName = firstName
        draft.lastName = lastName
        draft.email = email

        #expect(draft.isProfileValid == isValid)
    }

    @Test("A six-digit code is complete, five is not")
    func codeCompletion() {
        var draft = SignUpDraft()
        draft.code = "12345"
        #expect(draft.isCodeComplete == false)

        draft.code = "123456"
        #expect(draft.isCodeComplete)
    }
}
