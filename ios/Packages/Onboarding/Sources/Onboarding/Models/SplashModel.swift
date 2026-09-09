import Foundation
import Observation

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
