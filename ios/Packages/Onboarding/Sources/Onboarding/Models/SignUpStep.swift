/// The five sign-up steps, in order. `rawValue` is the segment index the progress bar shows.
///
/// Public by exception: `Gallery` → `ScreenCatalog` opens one step directly, and #38 exposes
/// it through `OnboardingRoot.Stage`. Every other type in this package stays internal.
public enum SignUpStep: Int, CaseIterable, Sendable {
    case phone, code, passcode, biometrics, profile
}
