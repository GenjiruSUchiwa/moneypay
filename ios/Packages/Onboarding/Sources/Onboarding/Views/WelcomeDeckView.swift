import DesignSystem
import Money
import SwiftUI

struct WelcomeDeckView: View {
    let model: WelcomeModel
    let slides: [WelcomeSlide]
    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    @Environment(\.locale) private var locale
    @State private var dragOffset: CGFloat = .zero
    @State private var isHolding = false
    @State private var isFloating = false

    var body: some View {
        ZStack {
            ForEach(slides.indices, id: \.self) { index in
                card(at: index)
            }
        }
        .animation(reduceMotion ? nil : Motion.deck, value: model.index)
        .frame(width: Metric.deckWidth)
        .frame(maxWidth: .infinity, maxHeight: .infinity)
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

    @ViewBuilder
    private func card(at index: Int) -> some View {
        let depth = model.depth(of: index)
        let slot = DeckSlot.placement(for: depth)
        let view = VirtualCardView(card: WelcomeSlide.card(for: slides[index], locale: locale))
            .frame(width: Metric.deckWidth)
            .offset(slot.offset)
            .scaleEffect(slot.scale)
            .rotationEffect(slot.rotation)
            .zIndex(Double(slides.count - depth))

        if depth == 0 {
            view
                .offset(x: dragOffset)
                .rotationEffect(.degrees(dragOffset / DeckSlot.dragRotationDivisor))
                .gesture(dragGesture)
        } else {
            view
        }
    }

    private var dragGesture: some Gesture {
        DragGesture(minimumDistance: .zero)
            .onChanged { value in
                if !isHolding {
                    isHolding = true
                    model.hold()
                }
                dragOffset = value.translation.width
            }
            .onEnded { value in
                let distance = value.translation.width
                withAnimation(reduceMotion ? nil : Motion.deck) {
                    model.release(dx: distance)
                    dragOffset = .zero
                }
                isHolding = false
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
        let card = WelcomeSlide.card(for: slides[model.index], locale: locale)
        let position = String(
            localized: "Card \(model.index + 1) of \(slides.count)",
            bundle: .module,
            locale: locale
        )
        return Text(verbatim: "\(card.label), \(position)")
    }
}

/// Geometry mirrors `.we-card[data-depth]` in `prototype/styles.css`.
private enum DeckSlot {
    struct Placement {
        let offset: CGSize
        let scale: CGFloat
        let rotation: Angle
    }

    static let dragRotationDivisor: CGFloat = 18

    static func placement(for depth: Int) -> Placement {
        switch depth {
        case 1:
            Placement(offset: CGSize(width: 12, height: -32), scale: 0.92, rotation: .degrees(4.5))
        case 2:
            Placement(offset: CGSize(width: -13, height: -58), scale: 0.84, rotation: .degrees(-4))
        default:
            Placement(offset: .zero, scale: 1, rotation: .zero)
        }
    }
}
