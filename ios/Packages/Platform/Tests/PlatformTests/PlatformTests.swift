import Foundation
import Platform
import PlatformTestSupport
import Testing

@Suite("Platform")
struct PlatformTests {

    @Test("FixedClock does not move between two reads")
    func fixedClockIsStable() {
        let clock = FixedClock(now: Date(timeIntervalSince1970: 1_700_000_000))
        #expect(clock.now == clock.now)
        #expect(clock.now.timeIntervalSince1970 == 1_700_000_000)
    }

    @Test("InMemoryKeyValueStoring reads back what was written to it")
    func inMemoryStoreRoundTrips() {
        let store = InMemoryKeyValueStoring()
        #expect(store.string(forKey: "theme") == nil)
        #expect(store.bool(forKey: "onboarded") == false)
        store.set("dark", forKey: "theme")
        store.set(true, forKey: "onboarded")
        #expect(store.string(forKey: "theme") == "dark")
        #expect(store.bool(forKey: "onboarded"))
    }
}
