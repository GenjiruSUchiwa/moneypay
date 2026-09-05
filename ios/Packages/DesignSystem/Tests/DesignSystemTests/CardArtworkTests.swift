import SwiftUI
import Testing
@testable import DesignSystem

struct CardArtworkTests {
    @Test("Ndop preserves its SVG geometry when the card scales")
    func ndopScalesWithTheCard() {
        let reference = CardArtworkTokens.ndop(in: CGSize(width: 200, height: 126)).boundingRect
        let doubled = CardArtworkTokens.ndop(in: CGSize(width: 400, height: 252)).boundingRect
        #expect(reference == CGRect(x: 0, y: 0, width: 224, height: 140))
        #expect(doubled.width == reference.width * 2)
        #expect(doubled.height == reference.height * 2)
        #expect(CardArtworkTokens.ndop(in: .zero).isEmpty)
    }

    @Test("The pine rosette keeps the prototype's fixed size and bottom-right anchoring")
    func pineAnchoring() {
        let reference = CardArtworkTokens.pine(in: CGSize(width: 296, height: 186)).boundingRect
        let wider = CardArtworkTokens.pine(in: CGSize(width: 396, height: 236)).boundingRect
        #expect(reference == CGRect(x: 160, y: 56, width: 164, height: 164))
        #expect(wider == reference.offsetBy(dx: 100, dy: 50))
    }
}
