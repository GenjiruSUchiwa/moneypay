import DesignSystem
import Foundation
import Money
import SwiftUI

/// One slide of the welcome deck: its copy as catalog keys and its card presentation data.
struct WelcomeSlide: Identifiable, Sendable {
    let id: Int
    let title: LocalizedStringKey
    let body: LocalizedStringKey
    let cardLabel: String
    let theme: CardTheme
    let network: CardNetwork
    /// Stable for the process, so the deck's `VirtualCardView`s keep their identity across renders.
    let cardID = UUID()

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
            theme: .ndop,
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

    func localizedCardLabel(_ locale: Locale) -> String {
        String(localized: String.LocalizationValue(cardLabel), bundle: .module, locale: locale)
    }

    func card(locale: Locale = .current) -> VirtualCard {
        VirtualCard(
            id: cardID,
            label: localizedCardLabel(locale),
            theme: theme,
            network: network,
            pan: "5399471028834412",
            cvv: "417",
            expiry: "09/29",
            createdAt: Date.distantPast,
            monthlyLimitUSDCents: 15_000,
            spentUSDCents: 0
        )
    }
}
