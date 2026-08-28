import DesignSystem
import SwiftUI

public struct WelcomeView: View {
    public init(onStart: @escaping () -> Void, onSignIn: @escaping () -> Void) {
        self.onStart = onStart
        self.onSignIn = onSignIn
    }

    public var onStart: () -> Void
    public var onSignIn: () -> Void
    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    @Environment(\.accessibilityVoiceOverEnabled) private var voiceOverEnabled
    @State private var model = WelcomeModel(count: WelcomeSlide.all.count)
    @State private var appeared = false

    public var body: some View {
        VStack(spacing: .zero) {
            Wordmark(size: 17)
                .frame(maxWidth: .infinity, alignment: .leading)
                .gutter()
                .padding(.top, Metric.rowVertical)
                .modifier(Rise(index: 0, appeared: appeared, reduceMotion: reduceMotion))

            SegmentedProgress(
                count: model.count,
                current: model.index,
                style: .story(dwell: model.dwell)
            )
            .gutter()
            .padding(.top, Metric.rowVertical)
            .modifier(Rise(index: 1, appeared: appeared, reduceMotion: reduceMotion))

            WelcomeTexts(
                slides: WelcomeSlide.all,
                currentIndex: model.index,
                reduceMotion: reduceMotion
            )
            .gutter()
            .padding(.top, Metric.section)
            .modifier(Rise(index: 2, appeared: appeared, reduceMotion: reduceMotion))

            WelcomeDeckView(model: model, slides: WelcomeSlide.all)
                .frame(maxWidth: .infinity, maxHeight: .infinity)
                .modifier(Rise(index: 3, appeared: appeared, reduceMotion: reduceMotion))

            WelcomeActions(onStart: onStart, onSignIn: onSignIn)
                .modifier(Rise(index: 4, appeared: appeared, reduceMotion: reduceMotion))
        }
        .page()
        .onAppear { appeared = true }
        .task(id: voiceOverEnabled) {
            guard !voiceOverEnabled else { return }
            await model.run()
        }
    }
}

private struct WelcomeTexts: View {
    let slides: [WelcomeSlide]
    let currentIndex: Int
    let reduceMotion: Bool

    var body: some View {
        ZStack(alignment: .topLeading) {
            ForEach(slides) { slide in
                text(for: slide)
            }
        }
        .frame(maxWidth: .infinity, alignment: .topLeading)
    }

    @ViewBuilder
    private func text(for slide: WelcomeSlide) -> some View {
        let isCurrent = slide.id == currentIndex

        VStack(alignment: .leading, spacing: Metric.stack) {
            Text(slide.title, bundle: .module)
                .font(.heading1)
                .tight()
                .foregroundStyle(Brand.ink)
                .fixedSize(horizontal: false, vertical: true)
                .accessibilityAddTraits(.isHeader)

            Text(slide.body, bundle: .module)
                .font(.bodyReg)
                .foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .opacity(isCurrent ? 1 : 0)
        .blur(radius: reduceMotion || isCurrent ? .zero : Motion.blur)
        .offset(y: reduceMotion || isCurrent ? .zero : Motion.rise)
        .animation(reduceMotion ? Motion.quick : Motion.screen, value: currentIndex)
        .accessibilityHidden(!isCurrent)
    }
}

private struct WelcomeActions: View {
    let onStart: () -> Void
    let onSignIn: () -> Void

    var body: some View {
        VStack(spacing: Metric.stack) {
            MPButton(title: Text("Create my account", bundle: .module), action: onStart)
                .gutter()

            MPButton(
                title: Text("I already have an account", bundle: .module),
                tone: .quiet,
                action: onSignIn
            )
            .gutter()

            Text("Cards issued by our licensed partner bank.", bundle: .module)
                .font(.micro)
                .foregroundStyle(Brand.inkFaint)
                .multilineTextAlignment(.center)
                .frame(maxWidth: .infinity)
                .padding(.top, Metric.rowVertical)
        }
    }
}

private struct Rise: ViewModifier {
    let index: Int
    let appeared: Bool
    let reduceMotion: Bool

    func body(content: Content) -> some View {
        content
            .opacity(appeared || reduceMotion ? 1 : 0)
            .offset(y: appeared || reduceMotion ? .zero : Motion.rise)
            .animation(
                reduceMotion ? nil : Motion.screen.delay(Double(index) * Motion.stagger),
                value: appeared
            )
    }
}

#Preview("Welcome — fr") {
    WelcomeView(onStart: {}, onSignIn: {})
        .environment(\.locale, Locale(identifier: "fr"))
}

#Preview("Welcome — en") {
    WelcomeView(onStart: {}, onSignIn: {})
        .environment(\.locale, Locale(identifier: "en"))
}

#Preview("Welcome — dark") {
    WelcomeView(onStart: {}, onSignIn: {})
        .preferredColorScheme(.dark)
}
