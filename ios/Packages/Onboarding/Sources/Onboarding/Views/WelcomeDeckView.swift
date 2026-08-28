import DesignSystem
import Money
import SwiftUI

struct WelcomeDeckView: View {
    let model: WelcomeModel
    let slides: [WelcomeSlide]
    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    @Environment(\.locale) private var locale
    @State private var dragOffset: CGFloat = .zero
    /// Reset by SwiftUI when the gesture ends *or* is cancelled (an alert, backgrounding,
    /// a system gesture) — `onEnded` only covers the former, `pointercancel` in the prototype.
    @GestureState private var isDragging = false
    @State private var isFloating = false

    var body: some View {
        ZStack {
            ForEach(slides.indices, id: \.self) { index in
                card(at: index)
            }
        }
        .animation(reduceMotion ? nil : Motion.deck, value: model.index)
        // The prototype listens on `.we-deck`, not on the card: the whole stage is the
        // drag surface, and every card keeps one stable identity across depth changes.
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .contentShape(.rect)
        .gesture(dragGesture)
        .onChange(of: isDragging) { _, dragging in
            if dragging {
                model.hold()
            } else {
                withAnimation(reduceMotion ? nil : Motion.deck) {
                    dragOffset = .zero
                    model.resume()
                }
            }
        }
        .offset(y: floatOffset)
        .animation(reduceMotion ? nil : Motion.float, value: isFloating)
        .onAppear { isFloating = true }
        .accessibilityElement(children: .ignore)
        .accessibilityLabel(accessibilityLabel)
        .accessibilityAdjustableAction { direction in
            adjust(direction)
        }
    }

    private var floatOffset: CGFloat {
        guard !reduceMotion else { return .zero }
        return isFloating ? -Motion.floatAmplitude : Motion.floatAmplitude
    }

    private func card(at index: Int) -> some View {
        let depth = model.depth(of: index)
        let pose = DeckPose.at(depth: depth)
        let drag = depth == 0 ? dragOffset : .zero

        // Same order as CSS `translate() scale() rotate()` and `translateX() rotate()`:
        // SwiftUI applies the innermost modifier first, so rotate, scale, then move.
        return VirtualCardView(card: slides[index].card(locale: locale))
            .frame(width: Metric.stage)
            .rotationEffect(pose.rotation)
            .scaleEffect(pose.scale)
            .offset(pose.offset)
            .rotationEffect(.degrees(drag * DeckPose.dragTiltPerPoint))
            .offset(x: drag)
            .zIndex(Double(slides.count - depth))
    }

    private var dragGesture: some Gesture {
        DragGesture(minimumDistance: .zero)
            .updating($isDragging) { _, state, _ in state = true }
            .onChanged { dragOffset = $0.translation.width }
            .onEnded { value in
                withAnimation(reduceMotion ? nil : Motion.deck) {
                    model.release(dx: value.translation.width)
                }
            }
    }

    private func adjust(_ direction: AccessibilityAdjustmentDirection) {
        withAnimation(reduceMotion ? nil : Motion.deck) {
            switch direction {
            case .increment:
                model.advance()
            case .decrement:
                model.retreat()
            @unknown default:
                return
            }
        }
    }

    private var accessibilityLabel: Text {
        let position = String(
            localized: "Card \(model.index + 1) of \(slides.count)",
            bundle: .module,
            locale: locale
        )
        return Text(verbatim: "\(slides[model.index].localizedCardLabel(locale)), \(position)")
    }
}

#Preview("WelcomeDeckView") {
    WelcomeDeckView(model: WelcomeModel(count: WelcomeSlide.all.count), slides: WelcomeSlide.all)
        .page()
}
