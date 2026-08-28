import SwiftUI

struct WelcomeView: View {
    var onStart: () -> Void
    var onSignIn: () -> Void
    @State private var page = 0

    private struct Slide { var title: String, body: String, theme: CardTheme, label: String }

    private let slides: [Slide] = [
        .init(title: "Une carte par usage,\ncréée en 30 secondes",
              body: "Visa ou Mastercard, son propre plafond, gelée d'un geste. Autant de cartes que de besoins.",
              theme: .ink, label: "Abonnements"),
        .init(title: "Rechargée en\nMobile Money",
              body: "MTN MoMo, Orange Money ou dépôt agent. Votre solde FCFA finance chaque paiement en dollars.",
              theme: .pine, label: "Shopping"),
        .init(title: "Acceptée là où\nVisa et Mastercard le sont",
              body: "Netflix, AWS, AliExpress. Le taux et la marge sont affichés avant chaque conversion.",
              theme: .clay, label: "Serveurs")
    ]

    var body: some View {
        VStack(spacing: 0) {
            HStack {
                Wordmark(size: 17)
                Spacer()
                Button(action: onSignIn) {
                    Text("Se connecter").font(.subMed).foregroundStyle(Brand.inkMuted)
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

            MPButton(title: "Créer mon compte", action: onStart)
                .gutter()

            Text("Cartes émises par notre banque partenaire agréée.")
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

            Text(slides[i].title)
                .font(.system(size: 29, weight: .semibold))
                .tight(-0.8)
                .foregroundStyle(Brand.ink)
                .fixedSize(horizontal: false, vertical: true)

            Text(slides[i].body)
                .font(.body)
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
