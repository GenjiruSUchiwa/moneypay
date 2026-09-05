import SwiftUI

enum AuthenticationFeedback: Equatable {
    case sending, queued, verifying, deliveryFailed, locked, expiredCode
    case invalidCode(attemptsRemaining: Int)
    case resendTooSoon(seconds: Int)
    case resendLimit, expiredSignUp, accountExists, noAccount
    case submitting, connectionFailed, completionFailed, emailExists

    var isLoading: Bool { self == .sending || self == .queued || self == .verifying || self == .submitting }
    var allowsCodeEntry: Bool {
        switch self {
        case .invalidCode, .resendTooSoon, .resendLimit: true
        default: false
        }
    }

    var title: LocalizedStringKey {
        switch self {
        case .sending, .queued: "Sending your code"
        case .verifying: "Checking your code"
        case .deliveryFailed: "Your code could not be delivered"
        case .invalidCode: "Incorrect code"
        case .locked: "Too many attempts"
        case .expiredCode: "This code has expired"
        case .resendTooSoon: "Please wait before requesting another code"
        case .resendLimit: "Code request limit reached"
        case .expiredSignUp: "Your verification has expired"
        case .accountExists: "This phone already has an account"
        case .noAccount: "This phone has no account yet"
        case .submitting: "Creating your account"
        case .connectionFailed: "Unable to connect"
        case .completionFailed: "Your account could not be created"
        case .emailExists: "This email is already in use"
        }
    }

    var message: Text {
        switch self {
        case .sending, .queued: Text("Your SMS is on its way. This may take a moment.", bundle: .module)
        case .verifying: Text("Please wait while we check your code.", bundle: .module)
        case .deliveryFailed: Text("Check your number or request a new code.", bundle: .module)
        case .invalidCode(let attempts): Text("You have \(attempts) attempts remaining.", bundle: .module)
        case .locked: Text("For your security, verification is temporarily unavailable. Try again later.", bundle: .module)
        case .expiredCode: Text("Request a new code to continue.", bundle: .module)
        case .resendTooSoon(let seconds): Text("You can request another code in \(seconds) s.", bundle: .module)
        case .resendLimit: Text("You have requested too many codes. Try again later.", bundle: .module)
        case .expiredSignUp: Text("Enter your phone number again to start a new verification.", bundle: .module)
        case .accountExists: Text("Sign in with this number to access your account.", bundle: .module)
        case .noAccount: Text("Create an account with this number to get started.", bundle: .module)
        case .submitting: Text("Please wait while we save your details.", bundle: .module)
        case .connectionFailed, .completionFailed:
            Text("Check your connection and try again. Your details are still here.", bundle: .module)
        case .emailExists: Text("Enter another email address to continue.", bundle: .module)
        }
    }

    var actionTitle: LocalizedStringKey? {
        switch self {
        case .deliveryFailed, .expiredCode: "Resend the code"
        case .expiredSignUp: "Start again"
        case .accountExists: "Sign in instead"
        case .noAccount: "Create an account"
        case .connectionFailed, .completionFailed: "Try again"
        case .emailExists: "Change email address"
        default: nil
        }
    }
}
