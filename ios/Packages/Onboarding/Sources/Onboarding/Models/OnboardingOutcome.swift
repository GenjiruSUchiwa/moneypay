import Money

/// Carries the result to the composition root; an existing-account sign-in remains a
/// separate sessions flow and goes straight to the main tabs.
public enum OnboardingOutcome {
    case signedUp(User)
    case signedIn
}
