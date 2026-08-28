import Testing
@testable import Onboarding

@Suite("WelcomeModel")
struct WelcomeModelTests {
    @Test("advance wraps from the last slide to the first, retreat from the first to the last")
    func advanceAndRetreatWrap() {
        let model = WelcomeModel(count: 3)

        model.advance()
        model.advance()
        #expect(model.index == 2)

        model.advance()
        #expect(model.index == 0)

        model.retreat()
        #expect(model.index == 2)
    }

    @Test("depth(of:) is the distance from the front slide, wrapping forward")
    func depthWrapsForward() {
        let model = WelcomeModel(count: 3)
        model.advance()

        #expect(model.depth(of: 0) == 2)
        #expect(model.depth(of: 1) == 0)
        #expect(model.depth(of: 2) == 1)
    }

    @Test("release maps a swipe or a tap onto advance, retreat or stay")
    func releaseInterpretsTravel() {
        let swipedLeft = WelcomeModel(count: 3)
        swipedLeft.release(dx: -60)
        #expect(swipedLeft.index == 1)

        let swipedRight = WelcomeModel(count: 3)
        swipedRight.release(dx: 60)
        #expect(swipedRight.index == 2)

        let tapped = WelcomeModel(count: 3)
        tapped.release(dx: 3)
        #expect(tapped.index == 1)

        let shortDrag = WelcomeModel(count: 3)
        shortDrag.release(dx: 30)
        #expect(shortDrag.index == 0)
    }

    @Test("autoAdvanceAfterDwell() pages forward once and bumps the generation")
    func autoAdvancePagesOnce() async {
        let model = WelcomeModel(count: 3, dwell: .zero)

        await model.autoAdvanceAfterDwell()

        #expect(model.index == 1)
        #expect(model.generation == 1)
    }

    @Test("a held deck does not auto-advance; the release re-arms a fresh dwell")
    func holdSkipsAutoAdvance() async {
        let model = WelcomeModel(count: 3, dwell: .zero)
        model.hold()

        await model.autoAdvanceAfterDwell()
        #expect(model.index == 0)

        model.release(dx: 30)   // short drag: stays put, but the dwell restarts
        #expect(model.index == 0)
        #expect(model.generation == 1)

        await model.autoAdvanceAfterDwell()
        #expect(model.index == 1)
    }

    @Test("every user action re-arms the dwell by bumping the generation")
    func userActionsBumpGeneration() {
        let model = WelcomeModel(count: 3)

        model.release(dx: -60)
        #expect(model.generation == 2)   // release + advance
        model.retreat()
        #expect(model.generation == 3)
    }

    @Test("a single-slide deck is a no-op for every direction")
    func singleSlideDeckNeverMoves() {
        let model = WelcomeModel(count: 1)

        model.advance()
        model.retreat()
        model.release(dx: -60)

        #expect(model.index == 0)
        #expect(model.depth(of: 0) == 0)
    }
}
