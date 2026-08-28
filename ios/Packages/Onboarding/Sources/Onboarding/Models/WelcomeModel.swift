import Foundation
import Observation

/// Drives the welcome deck: which slide is in front, and the auto-advance dwell.
/// The dwell, the swipe threshold and the tap threshold are product decisions
/// taken from the prototype, so they live here, not in the view.
@Observable
@MainActor
final class WelcomeModel {
    /// Horizontal travel that counts as a swipe, in points (prototype `55`).
    static let swipeThreshold: CGFloat = 55
    /// Travel below which a release is a tap rather than a drag (prototype `6`).
    static let tapThreshold: CGFloat = 6

    private(set) var index = 0
    let count: Int
    let dwell: Duration
    private var paused = false
    /// Bumped by every user interaction. The run loop compares it after each
    /// sleep and, on a mismatch, skips that auto-advance and sleeps a fresh
    /// dwell, so a swipe or a release never stacks an auto-advance on top.
    private var generation = 0

    init(count: Int, dwell: Duration = .milliseconds(4200)) {
        precondition(count >= 1, "A welcome deck needs at least one slide.")
        self.count = count
        self.dwell = dwell
    }

    /// Slide `i`'s depth in the deck: 0 = front, 1 = the card behind it, …
    func depth(of i: Int) -> Int {
        (i - index + count) % count
    }

    func advance() {
        index = (index + 1) % count
        generation += 1
    }

    func retreat() {
        index = (index - 1 + count) % count
        generation += 1
    }

    /// Finger down: the auto-advance stops counting until `release`.
    func hold() {
        paused = true
    }

    /// Finger up after a horizontal travel of `dx` points. Past the swipe
    /// threshold it pages in the drag's direction; a near-zero travel is a tap
    /// and pages forward; anything in between leaves the deck where it is.
    func release(dx: CGFloat) {
        paused = false
        generation += 1   // even a short drag re-arms a full dwell, as the prototype does
        if dx < -Self.swipeThreshold {
            advance()
        } else if dx > Self.swipeThreshold {
            retreat()
        } else if abs(dx) < Self.tapThreshold {
            advance()
        }
    }

    /// Auto-advance loop. Runs until the task is cancelled; each dwell is
    /// restarted by a user-driven change of `index` and skipped while held.
    func run() async {
        while !Task.isCancelled {
            let generationAtSleep = generation
            try? await Task.sleep(for: dwell)
            // Re-check after the sleep, like SplashModel: cancellation, a hold
            // or a manual swipe may have landed while we were suspended.
            guard !Task.isCancelled, !paused else { continue }
            guard generation == generationAtSleep else { continue }
            advance()
        }
    }
}
