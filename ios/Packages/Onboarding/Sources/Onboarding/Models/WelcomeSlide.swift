import DesignSystem
import SwiftUI

/// One slide of the welcome deck: its copy as catalog keys, and the card it
/// shows. The keys' French values land with the view PR (#20); until then an
/// unresolved key renders as its English source, which is what previews show.
struct WelcomeSlide: Identifiable, Sendable {
    let id: Int
    let title: LocalizedStringKey
    let body: LocalizedStringKey
    let cardLabel: LocalizedStringKey
    let theme: CardTheme
    let network: CardNetwork

    static let all: [WelcomeSlide] = [
        WelcomeSlide(
            id: 0,
            title: "One card per use,\ncreated in 30 seconds",
            body: "Visa or Mastercard, its own cap, frozen in one gesture. As many cards as you have needs.",
            cardLabel: "Subscriptions",
            theme: .pine,
            network: .mastercard
        ),
        WelcomeSlide(
            id: 1,
            title: "Topped up with\nMobile Money",
            body: "MTN MoMo, Orange Money or an agent deposit. Your FCFA balance funds every dollar payment.",
            cardLabel: "Shopping",
            theme: .cobalt,
            network: .visa
        ),
        WelcomeSlide(
            id: 2,
            title: "Accepted everywhere\nonline",
            body: "Netflix, OpenAI, AliExpress. The rate and the margin are shown before every conversion.",
            cardLabel: "Servers",
            theme: .ink,
            network: .mastercard
        )
    ]
}
