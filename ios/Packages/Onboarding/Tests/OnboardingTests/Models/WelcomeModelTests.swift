import Foundation
import Testing
@testable import Onboarding

@Suite("WelcomeModel")
struct WelcomeModelTests {
    @Test("Slide changes reset progress before the next task is scheduled")
    func slideAndProgressRestartTogether() {
        let start = Date(timeIntervalSince1970: 0)
        let model = WelcomeModel(count: 3, dwell: .seconds(4), startedAt: start)

        model.advance(at: start.addingTimeInterval(4))
        #expect(model.index == 1)
        #expect(model.cycleStart == start.addingTimeInterval(4))
        #expect(model.remainingDwell(at: start.addingTimeInterval(5)) == .seconds(3))

        model.retreat(at: start.addingTimeInterval(5))
        #expect(model.index == 0)
        #expect(model.cycleStart == start.addingTimeInterval(5))
    }

    @Test("A delayed timer uses the remaining dwell instead of restarting the progress clock")
    func lateTimerDoesNotAddAnotherDwell() async {
        let model = WelcomeModel(count: 3, startedAt: .now.addingTimeInterval(-10))
        #expect(model.remainingDwell(at: .now) == .zero)
        await model.autoAdvanceAfterDwell()
        #expect(model.index == 1)
    }

    @Test("Holding freezes the progress date and release restarts one shared cycle")
    func pauseAndResumeShareTheProgressClock() {
        let start = Date(timeIntervalSince1970: 0)
        let model = WelcomeModel(count: 3, startedAt: start)
        model.hold(at: start.addingTimeInterval(2))
        #expect(model.pausedAt == start.addingTimeInterval(2))

        model.resume(at: start.addingTimeInterval(8))
        #expect(model.pausedAt == nil)
        #expect(model.cycleStart == start.addingTimeInterval(8))
        #expect(model.remainingDwell(at: start.addingTimeInterval(8)) == model.dwell)
    }

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
        #expect(shortDrag.generation == 0)
    }

    @Test("autoAdvanceAfterDwell() pages forward once and bumps the generation")
    func autoAdvancePagesOnce() async {
        let model = WelcomeModel(count: 3, dwell: .zero)

        await model.autoAdvanceAfterDwell()

        #expect(model.index == 1)
        #expect(model.generation == 1)
    }

    @Test("a held deck does not auto-advance; resume re-arms a fresh dwell")
    func holdSkipsAutoAdvance() async {
        let model = WelcomeModel(count: 3, dwell: .zero)
        model.hold()

        await model.autoAdvanceAfterDwell()
        #expect(model.index == 0)

        model.resume()
        #expect(model.generation == 1)

        await model.autoAdvanceAfterDwell()
        #expect(model.index == 1)
    }

    @Test("a dwell re-armed while sleeping does not page when it expires")
    func staleDwellIsDropped() async {
        let model = WelcomeModel(count: 3, dwell: .milliseconds(200))
        let stale = Task { await model.autoAdvanceAfterDwell() }
        try? await Task.sleep(for: .milliseconds(20))

        model.advance()
        await stale.value

        #expect(model.index == 1)
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
