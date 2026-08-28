import Foundation
import Observation

/// The hold is a product decision (the prototype dwells 1.5 s), so it lives here, not in the view.
@Observable
@MainActor
final class SplashModel {
    enum Phase: Equatable {
        case hidden
        case revealed
        case finished
    }

    private(set) var phase: Phase = .hidden
    let hold: Duration

    init(hold: Duration = .milliseconds(1500)) {
        self.hold = hold
    }

    func run() async {
        phase = .revealed
        try? await Task.sleep(for: hold)
        guard !Task.isCancelled else { return }
        phase = .finished
    }
}
