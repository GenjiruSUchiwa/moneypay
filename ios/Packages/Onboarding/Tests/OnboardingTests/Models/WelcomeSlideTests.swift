import DesignSystem
import Testing
@testable import Onboarding

struct WelcomeSlideTests {
    @Test("Welcome cards preserve the prototype themes and networks")
    func prototypeCardArt() {
        #expect(WelcomeSlide.all.map(\.theme) == [.pine, .ndop, .ink])
        #expect(WelcomeSlide.all.map(\.network) == [.mastercard, .visa, .mastercard])
    }
}
