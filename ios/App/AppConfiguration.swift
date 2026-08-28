import ApiClient
import Foundation
import Platform

/// Composition root: the only place that knows how the pieces are wired.
/// Packages know each other through protocols only; this is where the real
/// implementations are chosen.
struct AppConfiguration {
    let clock: Clocking
    let logger: Logging
    let preferences: KeyValueStoring
    /// `nil` when `APIBaseURL` is empty: the app stays in mock mode, the way
    /// the prototype does when `poc/server.js` is down (see docs/handoff).
    let api: ApiClient?

    static func live() -> AppConfiguration {
        AppConfiguration(
            clock: SystemClock(),
            logger: OSLogging(category: "app"),
            preferences: UserDefaultsKeyValueStore(),
            api: ApiClient.fromBundle()
        )
    }
}
