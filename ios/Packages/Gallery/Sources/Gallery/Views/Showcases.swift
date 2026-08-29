import DesignSystem
import Foundation
import Money
import SwiftUI

public struct EmptyStatesShowcase: View {
    public init() {
    }

    public var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                Rule()
                EmptyNote(title: Text(verbatim: "No card yet"),
                          message: Text(verbatim: "Create a card for each use."),
                          actionTitle: Text(verbatim: "Create a card"))
                Rule()
                EmptyNote(title: Text(verbatim: "No transaction"),
                          message: Text(verbatim: "Payments with this card will show up here."))
                Rule()
                EmptyNote(title: Text(verbatim: "No result"),
                          message: Text(verbatim: "Try another filter or merchant name."))
                Rule()
                EmptyNote(title: Text(verbatim: "No connection"),
                          message: Text(verbatim: "Check your network. Data syncs when it returns."),
                          actionTitle: Text(verbatim: "Try again"))
                Rule()
            }
            .gutter()
        }
        .page()
        .navigationTitle(Text(verbatim: "Empty states"))
        .navigationBarTitleDisplayMode(.inline)
    }
}

public struct CardThemeShowcase: View {
    public init() {
    }

    public var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                ForEach(Array(CardTheme.allCases.enumerated()), id: \.element.id) { i, t in
                    VStack(alignment: .leading, spacing: 10) {
                        Eyebrow(text: Text(verbatim: t.label))
                        VirtualCardView(card: VirtualCard(
                            id: UUID(), label: "Abonnements", theme: t,
                            network: i % 2 == 0 ? .mastercard : .visa,
                            pan: "5399471028834412", cvv: "417", expiry: "09/29",
                            createdAt: .now, monthlyLimitUSDCents: 15_000, spentUSDCents: 4_780
                        ), revealed: true)
                        .frame(maxWidth: 300)
                    }
                    .padding(.vertical, 20)
                    if i < CardTheme.allCases.count - 1 { Rule() }
                }
            }
            .gutter()
        }
        .page()
        .navigationTitle("Habillages")
        .navigationBarTitleDisplayMode(.inline)
    }
}

public struct ComponentsShowcase: View {
    public init() {
    }

    @State private var seg = 0
    @State private var toastMsg: Toast?
    @State private var text = ""
    @State private var otp = "418"
    @State private var phone = ""
    @State private var passcode = "12"
    @State private var amount = "25000"
    @State private var storyStart = Date.now

    public var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                block("Buttons") {
                    MPButton(title: Text(verbatim: "Primary action")) {
                        toastMsg = Toast(text: Text(verbatim: "Tapped"))
                    }
                    MPButton(title: Text(verbatim: "Neutral action"), tone: .quiet) {}
                    MPButton(title: Text(verbatim: "Secondary action"), tone: .outline) {}
                    MPButton(title: Text(verbatim: "Ghost action"), tone: .ghost) {}
                    MPButton(title: Text(verbatim: "Destructive action"), tone: .danger) {}
                    MPButton(title: Text(verbatim: "Loading"), loading: true) {}
                    MPButton(title: Text(verbatim: "Disabled"), enabled: false) {}
                }
                block("Logo") {
                    HStack(spacing: 16) {
                        LogoMark(size: 28)
                        LogoMark(size: 44)
                        Wordmark(size: 18)
                    }
                    LogoMark(size: 44, tint: Brand.deepInkFill, glyph: Brand.deepInk)
                        .padding()
                        .background(Brand.greenDeep)
                }
                block("Marks") {
                    HStack(spacing: 14) {
                        ForEach(FlagMark.Country.allCases, id: \.self) { FlagMark($0, size: 24) }
                    }
                    HStack(spacing: 14) {
                        ForEach(FlagMark.Country.allCases, id: \.self) { FlagMark($0, size: 30) }
                    }
                    HStack(spacing: 18) {
                        NetworkMark(network: .visa, ink: Brand.ink)
                        NetworkMark(network: .mastercard, ink: Brand.ink)
                    }
                }
                block("Quick actions") {
                    HStack(spacing: 4) {
                        QuickAction(icon: "plus", label: Text(verbatim: "Top up"))
                        QuickAction(icon: "arrow.left.arrow.right", label: Text(verbatim: "Convert"))
                        QuickAction(icon: "snowflake", label: Text(verbatim: "Freeze"))
                        QuickAction(icon: "trash", label: Text(verbatim: "Delete"))
                    }
                }
                block("Segments and chips") {
                    Segments(items: [Text(verbatim: "All"), Text(verbatim: "Cards"),
                                     Text(verbatim: "Top-ups")],
                             selection: $seg)
                    HStack(spacing: 7) {
                        Chip(text: Text(verbatim: "Selected"), selected: true)
                        Chip(text: Text(verbatim: "Normal"))
                        Chip(text: Text(verbatim: "Unlimited"))
                    }
                    HStack(spacing: 7) {
                        StatusPill(text: Text(verbatim: "Approved"), symbol: "checkmark",
                                   tint: Brand.credit, soft: Brand.creditSoft)
                        StatusPill(text: Text(verbatim: "Pending"), symbol: "clock",
                                   tint: Brand.pending, soft: Brand.pendingSoft)
                        StatusPill(text: Text(verbatim: "Declined"), symbol: "xmark",
                                   tint: Brand.debit, soft: Brand.debitSoft)
                    }
                }
                block("Segmented progress") {
                    SegmentedProgress(count: 5, current: 0)
                    SegmentedProgress(count: 5, current: 2)
                    SegmentedProgress(count: 5, current: 4)
                    SegmentedProgress(count: 3, current: 1, style: .story(dwell: .seconds(4), since: storyStart))
                }
                block("Amounts") {
                    MoneyText.xaf(428_500, size: 34)
                    MoneyText.usd(109_945, size: 28)
                    HStack(spacing: 18) {
                        MoneyText.xaf(98_500, size: 17, weight: .medium,
                                      color: Brand.credit, signed: true)
                        MoneyText.xaf(-6_907, size: 17, weight: .medium, signed: true)
                    }
                }
                block("Inputs") {
                    OTPBoxes(code: $otp, autofocus: false)
                    PasscodeDots(code: $passcode, autofocus: false)
                    PasscodeDots(filled: 2)
                    AmountEntry(digits: $amount, display: Fmt.group(Int(amount) ?? 0),
                                currency: "FCFA", autofocus: false)
                    Field(placeholder: Text(verbatim: "Card name"), text: $text, icon: "creditcard")
                    PhoneField(flag: .cm, dialCode: "+237", digits: $phone, groupedDigits: phone,
                               placeholder: "6 XX XX XX XX", isValid: phone.count == 9,
                               onCountryTap: {})
                }
                block("Rows") {
                    VStack(spacing: 0) {
                        Row(icon: "creditcard", title: Text(verbatim: "With an icon"),
                            subtitle: Text(verbatim: "And a subtitle"), chevron: true)
                        Rule(inset: 51)
                        Row(glyph: "🇨🇲", title: Text(verbatim: "With a flag"), chevron: true)
                        Rule(inset: 51)
                        Row(icon: "trash", title: Text(verbatim: "Destructive"), destructive: true)
                    }
                }
                block("Visualization palette") {
                    HStack(spacing: 5) {
                        ForEach(Viz.categorical.indices, id: \.self) { i in
                            VStack(spacing: 5) {
                                RoundedRectangle(cornerRadius: 6, style: .continuous)
                                    .fill(Viz.categorical[i]).frame(height: 38)
                                Text(i + 1, format: .number).font(.eyebrow)
                                    .foregroundStyle(Brand.inkFaint)
                            }
                        }
                    }
                    Text(verbatim: """
                        Fixed order, never recycled. Script-checked: ΔE colour-blind 8.4 · \
                        normal vision 19.3 · contrast ≥ 3:1.
                        """)
                        .font(.micro).foregroundStyle(Brand.inkFaint)
                        .fixedSize(horizontal: false, vertical: true)
                }
            }
            .gutter()
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle(Text(verbatim: "Components"))
        .navigationBarTitleDisplayMode(.inline)
        .toast($toastMsg)
    }

    @ViewBuilder
    private func block<C: View>(_ title: String, @ViewBuilder content: () -> C) -> some View {
        VStack(alignment: .leading, spacing: 12) {
            Eyebrow(text: Text(verbatim: title))
            content()
        }
        .padding(.vertical, 22)
        Rule()
    }
}
