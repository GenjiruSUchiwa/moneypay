import SwiftUI
import Testing
@testable import DesignSystem

struct SegmentedProgressTests {
    @Test("Story progress reaches completion at the shared deadline and clamps outside it")
    func progressUsesTheCycleDeadline() {
        let start = Date(timeIntervalSince1970: 0)
        #expect(SegmentedProgress.fraction(at: start, since: start, dwell: .seconds(4)) == 0)
        #expect(SegmentedProgress.fraction(at: start.addingTimeInterval(2), since: start, dwell: .seconds(4)) == 0.5)
        #expect(SegmentedProgress.fraction(at: start.addingTimeInterval(4), since: start, dwell: .seconds(4)) == 1)
        #expect(SegmentedProgress.fraction(at: start.addingTimeInterval(-1), since: start, dwell: .seconds(4)) == 0)
        #expect(SegmentedProgress.fraction(at: start.addingTimeInterval(8), since: start, dwell: .seconds(4)) == 1)
        #expect(SegmentedProgress.fraction(at: start, since: start, dwell: .zero) == 1)
    }
}
