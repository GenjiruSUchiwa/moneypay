import ApiClient
import Foundation
import Money

/// Everything the five steps collect, in one value. Going back never loses what was typed —
/// the prototype keeps the same `F` object across the whole flow.
struct SignUpDraft: Hashable, Sendable {
    static let codeLength = 6

    var country: Country = .cameroon
    var phoneDigits = ""
    var code = ""
    /// Held only for the length of the flow. Never persisted, never logged, never sent to the
    /// server — storage belongs to the Sessions epic (`docs/handoff/HANDOFF-2026-08-28-poc.md`).
    var passcode = ""
    var biometricsEnabled = false
    var firstName = ""
    var lastName = ""
    var email = ""

    var isPhoneValid: Bool { phoneDigits.count == country.digitCount }
    var isCodeComplete: Bool { code.count == Self.codeLength }

    /// Deliberately not a regex: both names present, and an `@` with a dot after it.
    var isProfileValid: Bool {
        guard !trimmedFirstName.isEmpty, !trimmedLastName.isEmpty else { return false }
        guard let at = email.firstIndex(of: "@") else { return false }
        return email[email.index(after: at)...].contains(".")
    }

    /// Digits only, country code first, no `+` — the shape `POST /signup` wants (`poc/README.md`).
    var e164Digits: String { country.dialCode.filter(\.isNumber) + phoneDigits }

    /// `+237 6 99 12 34 56`, for the code step's "Sent to %@" line. Display only.
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
