import DesignSystem
import Foundation
import Money
import SwiftUI

public struct WelcomeView: View {
    public init(onStart: @escaping () -> Void, onSignIn: @escaping () -> Void) {
        self.onStart = onStart
        self.onSignIn = onSignIn
    }

    public var onStart: () -> Void
    public var onSignIn: () -> Void
    @State private var page = 0

    /// `title` and `body` are copy and resolve against this package's catalog
    /// at render time; `label` is the name printed on the sample card, data.
    private struct Slide {
        var title: LocalizedStringKey
        var body: LocalizedStringKey
        var theme: CardTheme
        var label: String
    }

    private let slides: [Slide] = [
        .init(title: "One card per use,\ncreated in 30 seconds",
              body: "Visa or Mastercard, its own cap, frozen in one gesture. As many cards as you have needs.",
              theme: .ink, label: "Abonnements"),
        .init(title: "Topped up with\nMobile Money",
              body: "MTN MoMo, Orange Money or an agent deposit. Your FCFA balance funds every dollar payment.",
              theme: .pine, label: "Shopping"),
        .init(title: "Accepted wherever\nVisa and Mastercard are",
              body: "Netflix, AWS, AliExpress. The rate and the margin are shown before every conversion.",
              theme: .clay, label: "Serveurs")
    ]

    public var body: some View {
        VStack(spacing: 0) {
            HStack {
                Wordmark(size: 17)
                Spacer()
                Button(action: onSignIn) {
                    Text("Sign in", bundle: .module).font(.subMed).foregroundStyle(Brand.inkMuted)
                }
            }
            .gutter()
            .padding(.top, 6)

            TabView(selection: $page) {
                ForEach(slides.indices, id: \.self) { i in slide(i).tag(i) }
            }
            .tabViewStyle(.page(indexDisplayMode: .never))

            HStack(spacing: 5) {
                ForEach(slides.indices, id: \.self) { i in
                    Capsule()
                        .fill(i == page ? Brand.ink : Brand.rule)
                        .frame(width: i == page ? 18 : 6, height: 3)
                        .animation(.spring(response: 0.3, dampingFraction: 0.85), value: page)
                }
                Spacer()
            }
            .gutter()
            .padding(.bottom, 22)

            MPButton(title: Text("Create my account", bundle: .module), action: onStart)
                .gutter()

            Text("Cards issued by our licensed partner bank.", bundle: .module)
                .font(.micro)
                .foregroundStyle(Brand.inkFaint)
                .padding(.top, 12)
                .padding(.bottom, 8)
        }
        .page()
    }

    private func slide(_ i: Int) -> some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer(minLength: 12)

            VirtualCardView(card: VirtualCard(
                id: UUID(), label: slides[i].label, theme: slides[i].theme,
                network: i == 1 ? .visa : .mastercard,
                pan: "5399471028834412", cvv: "417", expiry: "09/29",
                createdAt: .now, monthlyLimitUSDCents: 15_000, spentUSDCents: 0
            ))
            .frame(maxWidth: 300)
            .frame(maxWidth: .infinity, alignment: .leading)

            Spacer(minLength: 28)

            Text(slides[i].title, bundle: .module)
                .font(.system(size: 29, weight: .semibold))
                .tight(-0.8)
                .foregroundStyle(Brand.ink)
                .fixedSize(horizontal: false, vertical: true)

            Text(slides[i].body, bundle: .module)
                .font(.bodyReg)
                .foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true)
                .padding(.top, 10)
                .frame(maxWidth: 330, alignment: .leading)

            Spacer(minLength: 12)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .gutter()
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
