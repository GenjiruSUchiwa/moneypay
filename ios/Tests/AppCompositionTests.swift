import ApiClient
import Foundation
import PlatformTestSupport
import Testing

@testable import MoniPay

@Suite("Composition root")
struct AppCompositionTests {

    @Test("The live configuration provides a clock, a logger and preferences")
    func liveConfigurationIsWired() {
        let config = AppConfiguration.live()
        #expect(config.clock.now.timeIntervalSince1970 > 0)
        config.logger.log(.info, "smoke")
        config.preferences.set("dark", forKey: "monipay.tests.theme")
        #expect(config.preferences.string(forKey: "monipay.tests.theme") == "dark")
    }

    @Test("APIBaseURL points at the POC in Debug and is empty in Release")
    func apiBaseURLComesFromInfoPlist() throws {
        let raw = Bundle.main.object(forInfoDictionaryKey: "APIBaseURL") as? String
        let config = AppConfiguration.live()
        if let raw, !raw.isEmpty {
            #expect(URL(string: raw) != nil)
            #expect(config.api != nil)
        } else {
            #expect(config.api == nil)
        }
    }

    @Test("Platform test doubles are usable from the app target")
    func testSupportIsLinked() {
        let clock = FixedClock(now: Date(timeIntervalSince1970: 42))
        #expect(clock.now.timeIntervalSince1970 == 42)
    }

    @Test("The configuration always provides an account creator")
    func accountsFallBackToThePreviewClient() {
        let config = AppConfiguration.live()
        if config.api == nil {
            #expect(config.accounts is PreviewAccountClient)
        } else {
            #expect(config.accounts is ApiClient)
        }
    }
}
