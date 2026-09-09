import Foundation
import Observation

@Observable
@MainActor
final class WelcomeModel {
    static let swipeThreshold: CGFloat = 55
    static let tapThreshold: CGFloat = 6

    private(set) var index = 0
    let count: Int
    let dwell: Duration
    private(set) var pausedAt: Date?
    private(set) var generation = 0
    private(set) var cycleStart: Date

    init(count: Int, dwell: Duration = .milliseconds(4200), startedAt: Date = .now) {
        precondition(count >= 1, "A welcome deck needs at least one slide.")
        self.count = count
        self.dwell = dwell
        self.cycleStart = startedAt
    }

    func depth(of i: Int) -> Int {
        (i - index + count) % count
    }

    func advance(at date: Date = .now) {
        index = (index + 1) % count
        restart(at: date)
    }

    func retreat(at date: Date = .now) {
        index = (index - 1 + count) % count
        restart(at: date)
    }

    func hold(at date: Date = .now) {
        pausedAt = date
    }

    func resume(at date: Date = .now) {
        pausedAt = nil
        restart(at: date)
    }

    func release(dx: CGFloat) {
        if dx < -Self.swipeThreshold {
            advance()
        } else if dx > Self.swipeThreshold {
            retreat()
        } else if abs(dx) < Self.tapThreshold {
            advance()
        }
    }

    func autoAdvanceAfterDwell() async {
        guard pausedAt == nil else { return }
        let armed = generation
        try? await Task.sleep(for: remainingDwell(at: .now), tolerance: .zero)
        guard !Task.isCancelled, pausedAt == nil, generation == armed else { return }
        advance()
    }

    func remainingDwell(at date: Date) -> Duration {
        max(.zero, dwell - .seconds(max(0, date.timeIntervalSince(cycleStart))))
    }

    private func restart(at date: Date) {
        cycleStart = date
        generation += 1
    }
}
