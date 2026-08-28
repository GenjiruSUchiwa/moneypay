import Foundation

extension LocalizedStringResource {
    /// A key from `Money`'s own catalog.
    ///
    /// `LocalizedStringResource` resolves against `Bundle.main` by default — the
    /// app bundle, which holds no package's strings — so every key a package
    /// vends has to name its own bundle. Handing out a *resource* rather than a
    /// resolved `String` also keeps the choice of locale with the view that
    /// renders it.
    ///
    /// These are the names of domain states (a transaction status, a spending
    /// category). They belong to a presentation layer, not to a domain package;
    /// they sit here only until `Money` stops importing `DesignSystem` (see
    /// follow-up 3 of docs/handoff/HANDOFF-2026-08-28-setup.md), because the
    /// alternative today is the same copy triplicated across three features.
    static func money(_ key: String.LocalizationValue) -> LocalizedStringResource {
        LocalizedStringResource(key, bundle: .atURL(Bundle.module.bundleURL))
    }
}
