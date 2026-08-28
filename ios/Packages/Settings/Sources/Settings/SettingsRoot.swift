import DesignSystem
import SwiftUI
import WalletStore

public struct SettingsView: View {
    public init() {
    }

    @Environment(Store.self) private var store
    @State private var notificationsOn = true
    @State private var faceIDOn = true

    public var body: some View {
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 0) {
                    identity
                    Rule()
                    referral
                    Rule()

                    group("Compte") {
                        link("Informations personnelles", "person.text.rectangle") { ProfileView() }
                        Rule(inset: 51)
                        link("Plafonds et limites", "gauge.with.dots.needle.50percent") { LimitsView() }
                        Rule(inset: 51)
                        link("Abonnements récurrents", "arrow.triangle.2.circlepath",
                             value: "4") { SubscriptionsView() }
                        Rule(inset: 51)
                        link("Documents et relevés", "doc.text") { DocumentsView() }
                    }

                    group("Sécurité") {
                        link("Code secret", "lock") { SecurityView() }
                        Rule(inset: 51)
                        toggleRow("Face ID", "faceid", $faceIDOn)
                        Rule(inset: 51)
                        link("Appareils connectés", "iphone", value: "2") { DevicesView() }
                    }

                    group("Préférences") {
                        toggleRow("Notifications push", "bell", $notificationsOn)
                        Rule(inset: 51)
                        Row(icon: "globe", title: "Langue", chevron: true) { RowValue(text: "Français") }
                        Rule(inset: 51)
                        Row(icon: "coloncurrencysign.circle", title: "Devise d'affichage",
                            chevron: true) { RowValue(text: "FCFA") }
                    }

                    group("Aide") {
                        link("Centre d'aide", "questionmark.circle") { HelpView() }
                        Rule(inset: 51)
                        Row(icon: "bubble.left.and.bubble.right", title: "Discuter avec un conseiller",
                            chevron: true) {
                            StatusPill(text: "En ligne", symbol: "circle.fill",
                                       tint: Brand.credit, soft: Brand.creditSoft)
                        }
                        Rule(inset: 51)
                        Row(icon: "doc.plaintext", title: "Conditions générales", chevron: true)
                    }

                    // The screen gallery (Gallery package) references EVERY
                    // screen, Settings included, so it sits above this package
                    // and its entry point lives in MainTabView (App).

                    Rule()
                    Row(icon: "rectangle.portrait.and.arrow.right", title: "Se déconnecter",
                        destructive: true)
                        .gutter()
                    Rule()

                    Text("MoneyPay · version 0.1 (maquette)\nAucune donnée réelle n'est traitée.")
                        .font(.micro).foregroundStyle(Brand.inkFaint)
                        .gutter().padding(.top, 18)
                }
                .padding(.bottom, 28)
            }
            .scrollIndicators(.hidden)
            .page()
            .safeAreaInset(edge: .top, spacing: 0) {
                HStack {
                    Text("Profil").font(.heading1).tight(-0.6).foregroundStyle(Brand.ink)
                    Spacer()
                }
                .gutter()
                .padding(.top, 4)
                .padding(.bottom, 12)
                .background(Brand.bg)
            }
        }
    }

    private var identity: some View {
        NavigationLink { ProfileView() } label: {
            HStack(spacing: 14) {
                Text(store.user.initials)
                    .font(.system(size: 17, weight: .medium))
                    .foregroundStyle(Brand.onInk)
                    .frame(width: 48, height: 48)
                    .background(Brand.inkFill, in: .rect(cornerRadius: 13, style: .continuous))
                VStack(alignment: .leading, spacing: 4) {
                    Text(store.user.fullName).font(.bodyMed).foregroundStyle(Brand.ink)
                    Text(store.user.phone).font(.sub).foregroundStyle(Brand.inkMuted).monospacedDigit()
                    if store.user.kycVerified {
                        StatusPill(text: "Identité vérifiée", symbol: "checkmark",
                                   tint: Brand.credit, soft: Brand.creditSoft)
                    }
                }
                Spacer(minLength: 0)
                Image(systemName: "chevron.right").font(.system(size: 13, weight: .semibold))
                    .foregroundStyle(Brand.inkFaint)
            }
            .padding(.vertical, 18)
            .contentShape(.rect)
        }
        .buttonStyle(.plain)
        .gutter()
    }

    private var referral: some View {
        NavigationLink { ReferralView() } label: {
            HStack(spacing: 13) {
                IconTile(symbol: "gift")
                VStack(alignment: .leading, spacing: 2) {
                    Text("Parrainez, gagnez 2 500 FCFA").font(.bodyReg).foregroundStyle(Brand.ink)
                    Text("Pour chaque ami qui crée sa première carte")
                        .font(.sub).foregroundStyle(Brand.inkMuted)
                }
                Spacer(minLength: 8)
                Image(systemName: "chevron.right").font(.system(size: 13, weight: .semibold))
                    .foregroundStyle(Brand.inkFaint)
            }
            .padding(.vertical, Metric.rowVertical)
            .contentShape(.rect)
        }
        .buttonStyle(.plain)
        .gutter()
    }

    @ViewBuilder
    private func group<C: View>(_ title: String, @ViewBuilder content: () -> C) -> some View {
        VStack(alignment: .leading, spacing: 2) {
            Eyebrow(text: title).gutter().padding(.top, 24)
            VStack(spacing: 0) { content() }.gutter()
        }
        .padding(.bottom, 4)
        Rule()
    }

    private func link<D: View>(_ title: String, _ icon: String, value: String? = nil,
                               @ViewBuilder dest: @escaping () -> D) -> some View {
        NavigationLink { dest() } label: {
            Row(icon: icon, title: title, chevron: true) {
                if let value { RowValue(text: value) }
            }
        }
        .buttonStyle(.plain)
    }

    private func toggleRow(_ title: String, _ icon: String, _ value: Binding<Bool>) -> some View {
        HStack(spacing: 13) {
            IconTile(symbol: icon)
            Text(title).font(.bodyReg).foregroundStyle(Brand.ink)
            Spacer()
            Toggle("", isOn: value).labelsHidden().tint(Brand.inkFill)
        }
        .padding(.vertical, 11)
    }
}
