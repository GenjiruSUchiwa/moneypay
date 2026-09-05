import SwiftUI

/// The prototype's decorative card patterns. Place behind card content; use the theme fill for plain cards.
public struct CardArtwork: View {
    public init(theme: CardTheme) {
        self.theme = theme
    }

    private let theme: CardTheme

    public var body: some View {
        GeometryReader { geometry in
            switch theme {
            case .ndop:
                let width = CardArtworkTokens.diamondStroke * CardArtworkTokens.scale(for: geometry.size)
                CardArtworkTokens.ndop(in: geometry.size)
                    .stroke(CardArtworkTokens.diamondInk, lineWidth: width)
                CardArtworkTokens.ndop(in: geometry.size, ticks: true)
                    .stroke(CardArtworkTokens.tickInk, lineWidth: width)
            case .pine:
                CardArtworkTokens.pine(in: geometry.size)
                    .stroke(CardArtworkTokens.pineInk, lineWidth: CardArtworkTokens.pineStroke)
            default:
                EmptyView()
            }
        }
        .clipped()
        .allowsHitTesting(false)
        .accessibilityHidden(true)
    }
}

#Preview("Card artwork — welcome themes") {
    VStack(spacing: Metric.stack) {
        ForEach([CardTheme.pine, .ndop, .ink]) { theme in
            CardArtwork(theme: theme)
                .background(theme.fill)
                .aspectRatio(CardArtworkTokens.viewBox.width / CardArtworkTokens.viewBox.height, contentMode: .fit)
        }
    }
    .frame(width: Metric.stage)
    .page()
}
