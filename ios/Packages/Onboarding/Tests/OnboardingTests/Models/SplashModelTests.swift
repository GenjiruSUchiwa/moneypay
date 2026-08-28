import Testing
@testable import Onboarding

@Suite("SplashModel")
struct SplashModelTests {
    @Test("A fresh model is hidden")
    func freshModelIsHidden() {
        let model = SplashModel()

        #expect(model.phase == .hidden)
    }

    @Test("Running with a zero hold reveals then finishes")
    func zeroHoldFinishes() async {
        let model = SplashModel(hold: .zero)

        await model.run()

        #expect(model.phase == .finished)
    }

    @Test("Cancelling during the hold never finishes")
    func cancellationLeavesModelRevealed() async {
        let model = SplashModel(hold: .seconds(10))
        let task = Task { await model.run() }

        await Task.yield()
        task.cancel()
        await task.value

        #expect(model.phase == .revealed)
    }
}
