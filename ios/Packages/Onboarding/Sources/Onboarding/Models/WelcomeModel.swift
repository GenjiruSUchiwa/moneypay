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
    /// Bumped by every change that re-arms the dwell — an advance, a retreat, a
    /// resume. The view runs one `autoAdvanceAfterDwell()` task per generation, so a
    /// user action always restarts a full dwell, as the prototype's `clearTimeout` does.
    private(set) var generation = 0
    /// When the running dwell started sleeping; the story bar fills from here.
    private(set) var cycleStart = Date.now

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

    /// Finger down: the auto-advance stops counting until `resume`.
    func hold() {
        paused = true
    }

    /// Finger up or gesture cancelled: re-arms a full dwell, whether or not the deck paged.
    func resume() {
        paused = false
        generation += 1
    }

    /// Finger up after a horizontal travel of `dx` points. Past the swipe
    /// threshold it pages in the drag's direction; a near-zero travel is a tap
    /// and pages forward; anything in between leaves the deck where it is.
    func release(dx: CGFloat) {
        if dx < -Self.swipeThreshold {
            advance()
        } else if dx > Self.swipeThreshold {
            retreat()
        } else if abs(dx) < Self.tapThreshold {
            advance()
        }
    }

    /// One dwell, then one auto-advance — unless the deck is held or the dwell was re-armed
    /// meanwhile. Run it with `.task(id: generation)` so every user action restarts it; the
    /// generation check covers the gap between the sleep expiring and SwiftUI cancelling
    /// the stale task, which would otherwise page twice.
    func autoAdvanceAfterDwell() async {
        let armed = generation
        cycleStart = .now
        try? await Task.sleep(for: dwell)
        guard !Task.isCancelled, !paused, generation == armed else { return }
        advance()
    }
}
