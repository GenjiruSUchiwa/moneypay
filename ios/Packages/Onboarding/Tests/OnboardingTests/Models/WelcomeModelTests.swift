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

    @Test("run() auto-advances and stops after cancellation")
    func runAdvancesUntilCancelled() async {
        let model = WelcomeModel(count: 3, dwell: .zero)
        let task = Task { await model.run() }

        var attempts = 0
        while model.index == 0 && attempts < 100 {
            await Task.yield()
            attempts += 1
        }
        #expect(model.index != 0)

        task.cancel()
        await task.value
    }

    @Test("a held deck does not auto-advance until release")
    func holdPausesAutoAdvance() async {
        let model = WelcomeModel(count: 3, dwell: .zero)
        model.hold()
        let task = Task { await model.run() }

        for _ in 0..<10 { await Task.yield() }
        #expect(model.index == 0)

        model.release(dx: 0)
        #expect(model.index == 1)

        // The loop must resume after the release: wait for an auto-advance past the tap.
        var attempts = 0
        while model.index == 1 && attempts < 100 {
            await Task.yield()
            attempts += 1
        }
        #expect(model.index != 1)

        task.cancel()
        await task.value
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
