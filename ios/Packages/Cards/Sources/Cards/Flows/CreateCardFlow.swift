import DesignSystem
import Foundation
import Money
import SwiftUI
import WalletStore

public struct CreateCardFlow: View {
    public init() {
    }

    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    @State private var label = ""
    @State private var theme: CardTheme = .ink
    @State private var network: CardNetwork = .mastercard
    @State private var limitIndex = 1
    @State private var singleUse = false
    @State private var created: VirtualCard?
    @State private var issuing = false
    @FocusState private var nameFocused: Bool

    private let presets: [Int?] = [5_000, 15_000, 50_000, nil]
    private let suggestions = ["Abonnements", "Shopping", "Publicité", "Serveurs", "Voyage"]

    public var body: some View {
        NavigationStack {
            if let created { CardCreatedView(card: created) { dismiss() } } else { form }
        }
    }

    private var form: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                VirtualCardView(card: preview)
                    .frame(maxWidth: 300)
                    .frame(maxWidth: .infinity, alignment: .center)
                    .padding(.top, 8)

                Rule().padding(.top, 26)
                themePicker
                Rule()
                networkPicker
                Rule()
                nameField
                Rule()
                limitPicker
                Rule()
                singleUseRow
                Rule()

                Text("Le numéro, la date d'expiration et le CVV sont générés par notre processeur au moment de la création.")
                    .font(.micro).foregroundStyle(Brand.inkFaint)
                    .fixedSize(horizontal: false, vertical: true)
                    .gutter().padding(.top, 18)
            }
            .padding(.bottom, 20)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle("Nouvelle carte")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .topBarLeading) {
                Button("Annuler") { dismiss() }.foregroundStyle(Brand.inkMuted)
            }
        }
        .safeAreaInset(edge: .bottom) {
            MPButton(title: "Créer la carte", loading: issuing, action: issue)
                .gutter().padding(.top, 10).padding(.bottom, 8)
                .background(Brand.bg)
        }
    }

    private var preview: VirtualCard {
        VirtualCard(id: UUID(), label: label.isEmpty ? "Ma carte" : label,
                    theme: theme, network: network,
                    pan: "0000", cvv: "•••", expiry: "••/••",
                    createdAt: .now, monthlyLimitUSDCents: presets[limitIndex], spentUSDCents: 0)
    }

    private var themePicker: some View {
        VStack(alignment: .leading, spacing: 12) {
            Eyebrow(text: "Habillage").gutter()
            HStack(spacing: 10) {
                ForEach(CardTheme.allCases) { t in
                    Button {
                        Haptic.tap()
                        withAnimation(.easeOut(duration: 0.18)) { theme = t }
                    } label: {
                        RoundedRectangle(cornerRadius: 7, style: .continuous)
                            .fill(t.fill)
                            .frame(width: 40, height: 28)
                            .overlay {
                                RoundedRectangle(cornerRadius: 7, style: .continuous)
                                    .stroke(Brand.hairline, lineWidth: 1)
                            }
                            .overlay {
                                RoundedRectangle(cornerRadius: 10, style: .continuous)
                                    .stroke(Brand.ink, lineWidth: theme == t ? 1.6 : 0)
                                    .padding(-4)
                            }
                    }
                    .buttonStyle(Press())
                }
                Spacer(minLength: 0)
            }
            .gutter()
        }
        .padding(.vertical, 20)
    }

    private var networkPicker: some View {
        VStack(alignment: .leading, spacing: 12) {
            Eyebrow(text: "Réseau").gutter()
            HStack(spacing: 8) {
                ForEach([CardNetwork.mastercard, .visa], id: \.rawValue) { n in
                    Button {
                        Haptic.tap()
                        withAnimation(.easeOut(duration: 0.18)) { network = n }
                    } label: {
                        HStack(spacing: 9) {
                            NetworkMark(network: n, ink: Brand.ink, scale: 0.7)
                            Text(n == .visa ? "Visa" : "Mastercard")
                                .font(.subMed).foregroundStyle(Brand.ink)
                            Spacer(minLength: 0)
                        }
                        .padding(.horizontal, 13).frame(height: 50)
                        .background(Brand.well, in: .rect(cornerRadius: Metric.control, style: .continuous))
                        .overlay {
                            RoundedRectangle(cornerRadius: Metric.control, style: .continuous)
                                .stroke(Brand.ink, lineWidth: network == n ? 1.4 : 0)
                        }
                    }
                    .buttonStyle(Press())
                }
            }
            .gutter()
        }
        .padding(.vertical, 20)
    }

    private var nameField: some View {
        VStack(alignment: .leading, spacing: 12) {
            Eyebrow(text: "Nom de la carte").gutter()
            Field(placeholder: "Ex. Abonnements", text: $label, focused: nameFocused)
                .focused($nameFocused)
                .gutter()
            ScrollView(.horizontal) {
                HStack(spacing: 7) {
                    ForEach(suggestions, id: \.self) { s in
                        Chip(text: s, selected: label == s) { label = s; nameFocused = false }
                    }
                }
                .gutter()
            }
            .scrollIndicators(.hidden)
        }
        .padding(.vertical, 20)
    }

    private var limitPicker: some View {
        VStack(alignment: .leading, spacing: 12) {
            Eyebrow(text: "Plafond mensuel").gutter()
            HStack(spacing: 7) {
                ForEach(presets.indices, id: \.self) { i in
                    Chip(text: presets[i].map { Fmt.usd($0) } ?? "Illimité",
                         selected: i == limitIndex) { limitIndex = i }
                }
                Spacer(minLength: 0)
            }
            .gutter()
        }
        .padding(.vertical, 20)
    }

    private var singleUseRow: some View {
        HStack(spacing: 13) {
            IconTile(symbol: "1.circle")
            VStack(alignment: .leading, spacing: 2) {
                Text("Carte à usage unique").font(.bodyReg).foregroundStyle(Brand.ink)
                Text("Se supprime après le premier paiement").font(.sub).foregroundStyle(Brand.inkMuted)
            }
            Spacer(minLength: 8)
            Toggle("", isOn: $singleUse).labelsHidden().tint(Brand.inkFill)
        }
        .padding(.vertical, 14)
        .gutter()
    }

    private func issue() {
        guard !issuing else { return }
        issuing = true
        Task {
            try? await Task.sleep(for: .milliseconds(850))
            let card = store.createCard(label: label, theme: theme, network: network,
                                        limitUSDCents: presets[limitIndex], singleUse: singleUse)
            Haptic.success()
            withAnimation(.easeOut(duration: 0.25)) { created = card; issuing = false }
        }
    }
}

public struct CardCreatedView: View {
    public init(card: VirtualCard, onDone: @escaping () -> Void) {
        self.card = card
        self.onDone = onDone
    }

    public var card: VirtualCard
    public var onDone: () -> Void
    @State private var shown = false

    public var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer()
            VirtualCardView(card: card)
                .frame(maxWidth: 300)
                .frame(maxWidth: .infinity, alignment: .center)
                .opacity(shown ? 1 : 0)
                .offset(y: shown ? 0 : 14)

            Text("Votre carte est prête")
                .font(.system(size: 26, weight: .semibold)).tight(-0.6)
                .foregroundStyle(Brand.ink)
                .gutter().padding(.top, 36)

            Text("Utilisez-la immédiatement en ligne. Chaque paiement est financé par votre solde FCFA au moment de l'autorisation.")
                .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true)
                .gutter().padding(.top, 8)

            HStack(spacing: 7) {
                Image(systemName: "bolt.fill").font(.system(size: 11))
                Text("Émise en 0,9 s par le processeur").font(.sub)
                Spacer()
                Text("sandbox").font(.eyebrow).foregroundStyle(Brand.inkFaint)
            }
            .foregroundStyle(Brand.inkMuted)
            .gutter().padding(.top, 22)

            Spacer()
            VStack(spacing: 9) {
                MPButton(title: "Ajouter à Apple Wallet", icon: "wallet.pass", tone: .quiet) {}
                MPButton(title: "Terminé", action: onDone)
            }
            .gutter().padding(.bottom, 16)
        }
        .page()
        .navigationBarBackButtonHidden()
        .task { withAnimation(.easeOut(duration: 0.45)) { shown = true } }
    }
}
