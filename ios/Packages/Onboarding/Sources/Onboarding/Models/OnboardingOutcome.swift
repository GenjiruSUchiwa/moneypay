import Money

public enum OnboardingOutcome: Sendable {
    case signedUp(User)
    case signedIn
}
