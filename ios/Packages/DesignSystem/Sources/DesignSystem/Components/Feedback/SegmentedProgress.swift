import SwiftUI

/// Progress across a small, fixed number of steps — a sign-up flow, a story-style carousel.
/// `story` fills the current segment linearly over its dwell (the welcome deck);
/// `steps` only marks the reached segments (sign-up). For an amount against a cap, use
/// `ProgressView(value:)`.
public struct SegmentedProgress: View {
    /// How the current step is shown.
    public enum Style: Sendable, Equatable {
        /// Segments up to and including `current` are full. Sign-up steps.
        case steps
        /// Segments before `current` are full; `current` fills linearly from `since` over `dwell`.
        /// The fill is computed from the clock, so it stays in step with whatever timer the
        /// caller arms at `since`.
        case story(dwell: Duration, since: Date)
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
    }

    @ViewBuilder
    private func fill(of index: Int) -> some View {
        switch style {
        case .steps:
            ink(scale: index <= current ? 1 : 0)
        case let .story(dwell, since):
            if index == current {
                TimelineView(.animation) { context in
                    ink(scale: min(1, context.date.timeIntervalSince(since) / Self.seconds(from: dwell)))
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
    }
    .padding()
    .page()
}
