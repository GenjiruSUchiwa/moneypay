import SwiftUI

/// Progress across a small, fixed number of steps — a sign-up flow, a story-style carousel.
/// `story` animates the fill of the current segment over its dwell (the welcome deck);
/// `steps` only marks the reached segments (sign-up). For an amount against a cap, use `Meter`.
public struct SegmentedProgress: View {
    /// How the current step is shown.
    public enum Style: Sendable {
        /// Segments up to and including `current` are full. Sign-up steps.
        case steps
        /// Segments before `current` are full; `current` fills linearly over `dwell`. Stories.
        case story(dwell: Duration)
    }

    private let count: Int
    private let current: Int
    private let style: Style

    /// Fill of the running segment, 0…1. Only `story` drives it.
    @State private var runningFill: Double = 0

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
                    .overlay {
                        Capsule()
                            .fill(Brand.ink)
                            .scaleEffect(x: fill(of: index), anchor: .leading)
                    }
            }
        }
        .frame(height: Metric.progressHeight)
        .accessibilityElement(children: .ignore)
        .accessibilityLabel(Text("Step \(current + 1) of \(count)", bundle: .module))
        .accessibilityAddTraits(style.isStory ? .updatesFrequently : [])
        .onAppear(perform: startRunningFill)
        .onChange(of: current, startRunningFill)
    }

    private func fill(of index: Int) -> Double {
        switch style {
        case .steps: index <= current ? 1 : 0
        case .story:
            if index < current { 1 } else if index > current { 0 } else { runningFill }
        }
    }

    /// Restarts the story fill from empty. A state reset rather than `.id(current)`:
    /// recreating the segment would tear down the accessibility element mid-announcement.
    private func startRunningFill() {
        guard case let .story(dwell) = style else { return }
        runningFill = 0
        withAnimation(.linear(duration: Self.seconds(from: dwell))) { runningFill = 1 }
    }

    /// `Duration` is exact; `Animation` takes seconds. One conversion, kept off the public API.
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
    VStack(spacing: 20) {
        SegmentedProgress(count: 5, current: 0)
        SegmentedProgress(count: 5, current: 2)
        SegmentedProgress(count: 5, current: 4)
        SegmentedProgress(count: 3, current: 1, style: .story(dwell: .seconds(4)))
    }
    .padding()
    .page()
}
