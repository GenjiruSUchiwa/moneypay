import ApiClient
import Foundation
import Money

struct SignUpDraft: Hashable, Sendable {
    static let codeLength = 6

    var country: Country = .cameroon
    var phoneDigits = ""
    var code = ""
    var passcode = ""
    var biometricsEnabled = false
    var firstName = ""
    var lastName = ""
    var email = ""

    var isPhoneValid: Bool { phoneDigits.count == country.digitCount }
    var isCodeComplete: Bool { code.count == Self.codeLength }

    var isProfileValid: Bool {
        guard !trimmedFirstName.isEmpty, !trimmedLastName.isEmpty else { return false }
        guard let at = email.firstIndex(of: "@") else { return false }
        return email[email.index(after: at)...].contains(".")
    }

    var e164Digits: String { country.dialCode.filter(\.isNumber) + phoneDigits }

    var displayPhone: String { "\(country.dialCode) \(country.grouped(phoneDigits))" }

    var user: User {
        User(firstName: trimmedFirstName,
             lastName: trimmedLastName,
             phone: "+" + e164Digits,
             email: trimmedEmail,
             kycVerified: false)
    }

    var signupRequest: SignupRequest {
        SignupRequest(name: "\(trimmedFirstName) \(trimmedLastName)",
                      phone: e164Digits,
                      email: trimmedEmail)
    }

    private var trimmedFirstName: String { firstName.trimmingCharacters(in: .whitespacesAndNewlines) }
    private var trimmedLastName: String { lastName.trimmingCharacters(in: .whitespacesAndNewlines) }
    private var trimmedEmail: String { email.trimmingCharacters(in: .whitespacesAndNewlines) }
}
