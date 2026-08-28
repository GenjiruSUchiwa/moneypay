import Foundation

extension LocalizedStringResource {
    /// Key from `Money`'s catalog. Domain-state labels live here until `Money` stops importing `DesignSystem` (handoff follow-up 3).
    static func money(_ key: String.LocalizationValue) -> LocalizedStringResource {
        LocalizedStringResource(key, bundle: .atURL(Bundle.module.bundleURL))
    }
}
