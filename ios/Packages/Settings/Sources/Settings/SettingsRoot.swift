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

                    group("Account") {
                        link("Personal details", "person.text.rectangle") { ProfileView() }
                        Rule(inset: 51)
                        link("Caps and limits", "gauge.with.dots.needle.50percent") { LimitsView() }
                        Rule(inset: 51)
                        link("Recurring subscriptions", "arrow.triangle.2.circlepath",
                             value: 4) { SubscriptionsView() }
                        Rule(inset: 51)
                        link("Documents and statements", "doc.text") { DocumentsView() }
                    }

                    group("Security") {
                        link("Passcode", "lock") { SecurityView() }
                        Rule(inset: 51)
                        toggleRow("Face ID", "faceid", $faceIDOn)
                        Rule(inset: 51)
                        link("Connected devices", "iphone", value: 2) { DevicesView() }
                    }

                    group("Preferences") {
                        toggleRow("Push notifications", "bell", $notificationsOn)
                        Rule(inset: 51)
                        Row(icon: "globe", title: Text("Language", bundle: .module), chevron: true) {
                            // The language the app is showing, named in itself.
                            RowValue(text: Text(verbatim: Locale.current.localizedString(
                                forLanguageCode: Locale.current.language.languageCode?.identifier ?? "en"
                            ) ?? ""))
                        }
                        Rule(inset: 51)
                        Row(icon: "coloncurrencysign.circle",
                            title: Text("Display currency", bundle: .module),
                            chevron: true) { RowValue(text: Text(verbatim: "FCFA")) }
                    }

                    group("Help") {
                        link("Help centre", "questionmark.circle") { HelpView() }
                        Rule(inset: 51)
                        Row(icon: "bubble.left.and.bubble.right",
                            title: Text("Chat with an adviser", bundle: .module),
                            chevron: true) {
                            StatusPill(text: Text("Online", bundle: .module), symbol: "circle.fill",
                                       tint: Brand.credit, soft: Brand.creditSoft)
                        }
                        Rule(inset: 51)
                        Row(icon: "doc.plaintext", title: Text("Terms and conditions", bundle: .module),
                            chevron: true)
                    }

                    // The screen gallery (Gallery package) references EVERY
                    // screen, Settings included, so it sits above this package
                    // and its entry point lives in MainTabView (App).

                    Rule()
                    Row(icon: "rectangle.portrait.and.arrow.right",
                        title: Text("Sign out", bundle: .module), destructive: true)
                        .gutter()
                    Rule()

                    Text("MoneyPay · version 0.1 (mock-up)\nNo real data is processed.", bundle: .module)
                        .font(.micro).foregroundStyle(Brand.inkFaint)
                        .gutter().padding(.top, 18)
                }
                .padding(.bottom, 28)
            }
            .scrollIndicators(.hidden)
            .page()
            .safeAreaInset(edge: .top, spacing: 0) {
                HStack {
                    Text("Profile", bundle: .module).font(.heading1).tight(-0.6).foregroundStyle(Brand.ink)
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
                Text(verbatim: store.user.initials)
                    .font(.system(size: 17, weight: .medium))
                    .foregroundStyle(Brand.onInk)
                    .frame(width: 48, height: 48)
                    .background(Brand.inkFill, in: .rect(cornerRadius: 13, style: .continuous))
                VStack(alignment: .leading, spacing: 4) {
                    Text(verbatim: store.user.fullName).font(.bodyMed).foregroundStyle(Brand.ink)
                    Text(verbatim: store.user.phone).font(.sub).foregroundStyle(Brand.inkMuted).monospacedDigit()
                    if store.user.kycVerified {
                        StatusPill(text: Text("Identity verified", bundle: .module), symbol: "checkmark",
                                   tint: Brand.credit, soft: Brand.creditSoft)
                    }
                }
                Spacer(minLength: 0)
                Image(systemName: "chevron.forward").font(.system(size: 13, weight: .semibold))
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
                    Text("Refer a friend, earn \(Fmt.xaf(2_500))", bundle: .module)
                        .font(.bodyReg).foregroundStyle(Brand.ink)
                    Text("For every friend who creates their first card", bundle: .module)
                        .font(.sub).foregroundStyle(Brand.inkMuted)
                }
                Spacer(minLength: 8)
                Image(systemName: "chevron.forward").font(.system(size: 13, weight: .semibold))
                    .foregroundStyle(Brand.inkFaint)
            }
            .padding(.vertical, Metric.rowVertical)
            .contentShape(.rect)
        }
        .buttonStyle(.plain)
        .gutter()
    }

    @ViewBuilder
    private func group<C: View>(_ title: LocalizedStringKey,
                                @ViewBuilder content: () -> C) -> some View {
        VStack(alignment: .leading, spacing: 2) {
            Eyebrow(text: Text(title, bundle: .module)).gutter().padding(.top, 24)
            VStack(spacing: 0) { content() }.gutter()
        }
        .padding(.bottom, 4)
        Rule()
    }

    private func link<D: View>(_ title: LocalizedStringKey, _ icon: String, value: Int? = nil,
                               @ViewBuilder dest: @escaping () -> D) -> some View {
        NavigationLink { dest() } label: {
            Row(icon: icon, title: Text(title, bundle: .module), chevron: true) {
                if let value { RowValue(text: Text(value, format: .number)) }
            }
        }
        .buttonStyle(.plain)
    }

    private func toggleRow(_ title: LocalizedStringKey, _ icon: String,
                           _ value: Binding<Bool>) -> some View {
        HStack(spacing: 13) {
            IconTile(symbol: icon)
            Text(title, bundle: .module).font(.bodyReg).foregroundStyle(Brand.ink)
            Spacer()
            Toggle(isOn: value) { Text(title, bundle: .module) }
                .labelsHidden().tint(Brand.inkFill)
        }
        .padding(.vertical, 11)
    }
}

#Preview("Settings — fr") {
    SettingsView()
        .environment(Store())
        .environment(\.locale, Locale(identifier: "fr"))
}
