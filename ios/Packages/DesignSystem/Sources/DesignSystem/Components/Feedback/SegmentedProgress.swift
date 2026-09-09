import SwiftUI

public struct SegmentedProgress: View {
    public enum Style: Sendable, Equatable {
        case steps
        case story(dwell: Duration, since: Date, pausedAt: Date? = nil)
    }

    private let count: Int
    private let current: Int
    private let style: Style

    public init(count: Int, current: Int, style: Style = .steps) {
        self.count = count
        self.current = current
        self.style = style
    }

    public var body: some View {
        HStack(spacing: Metric.progressGap) {
            ForEach(0..<count, id: \.self) { index in
                Capsule()
                    .fill(Brand.well)
                    .overlay { fill(of: index) }
            }
        }
        .frame(height: Metric.progressHeight)
        .accessibilityElement(children: .ignore)
        .accessibilityLabel(Text("Step \(current + 1) of \(count)", bundle: .module))
        .accessibilityAddTraits(style.isStory ? .updatesFrequently : [])
        .transaction { if style.isStory { $0.animation = nil } }
    }

    @ViewBuilder
    private func fill(of index: Int) -> some View {
        switch style {
        case .steps:
            ink(scale: index <= current ? 1 : 0)
        case let .story(dwell, since, pausedAt):
            if index == current {
                if let pausedAt {
                    ink(scale: Self.fraction(at: pausedAt, since: since, dwell: dwell))
                } else {
                    TimelineView(.animation) { context in
                        ink(scale: Self.fraction(at: context.date, since: since, dwell: dwell))
                    }
                }
            } else {
                ink(scale: index < current ? 1 : 0)
            }
        }
    }

    private func ink(scale: Double) -> some View {
        Capsule()
            .fill(Brand.ink)
            .scaleEffect(x: max(0, scale), anchor: .leading)
    }

    private static func seconds(from dwell: Duration) -> TimeInterval {
        let parts = dwell.components
        return TimeInterval(parts.seconds) + TimeInterval(parts.attoseconds) / 1e18
    }

    static func fraction(at date: Date, since start: Date, dwell: Duration) -> Double {
        guard dwell > .zero else { return 1 }
        return min(1, max(0, date.timeIntervalSince(start) / seconds(from: dwell)))
    }
}

private extension SegmentedProgress.Style {
    var isStory: Bool {
        if case .story = self { return true }
        return false
    }
}

#Preview("SegmentedProgress — all styles") {
    @Previewable @State var since = Date.now
    VStack(spacing: 20) {
        SegmentedProgress(count: 5, current: 0)
        SegmentedProgress(count: 5, current: 2)
        SegmentedProgress(count: 5, current: 4)
        SegmentedProgress(count: 3, current: 1, style: .story(dwell: .seconds(4), since: since))
        SegmentedProgress(
            count: 3,
            current: 1,
            style: .story(dwell: .seconds(4), since: since, pausedAt: since.addingTimeInterval(2))
        )
    }
    .padding()
    .page()
}
