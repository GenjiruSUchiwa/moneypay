import SwiftUI

/// Lightweight navigation bar. The centre is either nothing, a title, or the progress
/// of a short flow; the two buttons are `Metric.navButton` glass circles (prototype `.nb-btn`).
public struct NavBar: View {
    /// What sits between the two buttons.
    public enum Center {
        case none
        case title(Text)
        /// A flow's progress. Rendered with `SegmentedProgress`, which owns the announcement.
        case progress(count: Int, current: Int)
    }

    public init(
        center: Center = .none,
        onBack: (() -> Void)? = nil,
        onClose: (() -> Void)? = nil
    ) {
        self.center = center
        self.onBack = onBack
        self.onClose = onClose
    }

    /// Convenience for the common title bar. `title` has no default **on purpose**:
    /// with one, `NavBar(onBack:)` would match both initializers and stop compiling.
    public init(title: Text, onBack: (() -> Void)? = nil, onClose: (() -> Void)? = nil) {
        self.init(center: .title(title), onBack: onBack, onClose: onClose)
    }

    private let center: Center
    private let onBack: (() -> Void)?
    private let onClose: (() -> Void)?

    public var body: some View {
        GlassEffectContainer(spacing: Metric.small) {
            HStack(spacing: 0) {
                slot(
                    symbol: "chevron.backward",
                    label: Text("Back", bundle: .module),
                    action: onBack
                )
                Spacer(minLength: Metric.small)
                centerView
                Spacer(minLength: Metric.small)
                slot(
                    symbol: "xmark",
                    label: Text("Close", bundle: .module),
                    action: onClose
                )
            }
        }
        .gutter()
        .frame(height: 48)
    }

    @ViewBuilder
    private var centerView: some View {
        switch center {
        case .none:
            EmptyView()
        case .title(let text):
            text.font(.heading3).foregroundStyle(Brand.ink).lineLimit(1)
        case .progress(let count, let current):
            SegmentedProgress(count: count, current: current)
        }
    }

    @ViewBuilder
    private func slot(symbol: String, label: Text, action: (() -> Void)?) -> some View {
        if let action {
            Button {
                Haptic.tap()
                action()
            } label: {
                Image(systemName: symbol)
                    .font(.heading3)
                    .foregroundStyle(Brand.ink)
                    .frame(width: Metric.navButton, height: Metric.navButton)
                    .glassEffect(.regular.interactive(), in: .circle)
                    .contentShape(.circle)
            }
            .buttonStyle(.plain)
            .accessibilityLabel(label)
        } else {
            Color.clear.frame(width: Metric.navButton, height: Metric.navButton)
        }
    }
}

private struct NavBarGlassCanvas: View {
    var body: some View {
        VStack(spacing: Metric.small) {
            NavBar(title: Text(verbatim: "Top up"))
            NavBar(title: Text(verbatim: "Top up"), onBack: {})
            NavBar(title: Text(verbatim: "New card"), onBack: {}, onClose: {})
            NavBar(center: .progress(count: 5, current: 0))
            NavBar(center: .progress(count: 5, current: 2), onBack: {})
        }
        .padding(.vertical, Metric.small)
        // Glass is fill + blur + edge; it only reads against something behind it.
        .background(Brand.markSoft)
    }
}

#Preview("NavBar — glass, light") {
    NavBarGlassCanvas()
        .preferredColorScheme(.light)
}

#Preview("NavBar — glass, dark") {
    NavBarGlassCanvas()
        .preferredColorScheme(.dark)
}
