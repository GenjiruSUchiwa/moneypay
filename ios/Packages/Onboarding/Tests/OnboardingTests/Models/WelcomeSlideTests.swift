import Testing
@testable import Onboarding

@Suite("WelcomeSlide")
struct WelcomeSlideTests {
    @Test("sample card keeps slide presentation data and fixed preview credentials")
    func sampleCardUsesSlideData() {
        let slide = WelcomeSlide.all[1]
        let card = WelcomeSlide.card(for: slide)

        #expect(card.theme == slide.theme)
        #expect(card.network == slide.network)
        #expect(card.pan == "5399471028834412")
        #expect(card.cvv == "417")
        #expect(card.expiry == "09/29")
        #expect(card.monthlyLimitUSDCents == 15_000)
        #expect(card.spentUSDCents == 0)
        #expect(!card.isFrozen)
    }
}
