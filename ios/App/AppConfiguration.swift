import ApiClient
import Foundation
import Platform

struct AppConfiguration {
    let clock: Clocking
    let logger: Logging
    let preferences: KeyValueStoring
    let api: ApiClient?
    var accounts: any AccountCreating { api ?? PreviewAccountClient() }

    static func live() -> AppConfiguration {
        AppConfiguration(
            clock: SystemClock(),
            logger: OSLogging(category: "app"),
            preferences: UserDefaultsKeyValueStore(),
            api: ApiClient.fromBundle()
        )
    }
}
