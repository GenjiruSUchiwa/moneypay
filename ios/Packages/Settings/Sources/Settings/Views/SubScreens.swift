import DesignSystem
import Foundation
import SwiftUI
import WalletStore

// MARK: - Personal details

public struct ProfileView: View {
    public init() {
    }

    @Environment(Store.self) private var store

    public var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                HStack(spacing: 14) {
                    Text(verbatim: store.user.initials)
                        .font(.system(size: 22, weight: .medium))
                        .foregroundStyle(Brand.onInk)
                        .frame(width: 64, height: 64)
                        .background(Brand.inkFill, in: .rect(cornerRadius: 17, style: .continuous))
                    VStack(alignment: .leading, spacing: 5) {
                        Text(verbatim: store.user.fullName).font(.heading3)
                            .foregroundStyle(Brand.ink)
                        StatusPill(text: Text("Account verified · level \(2)", bundle: .module),
                                   symbol: "checkmark",
                                   tint: Brand.credit, soft: Brand.creditSoft)
                    }
                    Spacer(minLength: 0)
                }
                .gutter().padding(.vertical, 18)

                Rule()
                Eyebrow(text: Text("Identity", bundle: .module)).gutter().padding(.top, 22).padding(.bottom, 2)
                VStack(spacing: 0) {
                    kv(Text("Full name", bundle: .module), Text(verbatim: store.user.fullName))
                    Rule()
                    kv(Text("Date of birth", bundle: .module),
                       Text(Self.birthDate, format: .dateTime.day().month(.wide).year()))
                    Rule()
                    kv(Text("Nationality", bundle: .module),
                       Text("Cameroonian", bundle: .module))
                    Rule()
                    kv(Text("Identity document", bundle: .module),
                       Text(verbatim: "CNI ••• 4821"))
                }
                .gutter()

                Rule()
                Eyebrow(text: Text("Contact", bundle: .module)).gutter().padding(.top, 22).padding(.bottom, 2)
                VStack(spacing: 0) {
                    Row(icon: "phone", title: Text("Phone", bundle: .module),
                        subtitle: Text(verbatim: store.user.phone), chevron: true)
                    Rule(inset: 51)
                    Row(icon: "envelope", title: Text("Email", bundle: .module),
                        subtitle: Text(verbatim: store.user.email), chevron: true)
                    Rule(inset: 51)
                    Row(icon: "house", title: Text("Address", bundle: .module),
                        subtitle: Text(verbatim: "Bonapriso, Douala"), chevron: true)
                }
                .gutter()

                Rule()
                Text("Changing your name or your date of birth needs a new identity check.",
                     bundle: .module)
                    .font(.micro).foregroundStyle(Brand.inkFaint)
                    .fixedSize(horizontal: false, vertical: true)
                    .gutter().padding(.top, 18)
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle(Text("Details", bundle: .module))
        .navigationBarTitleDisplayMode(.inline)
    }

    /// Sample date of birth, built from components rather than typed out as
    /// "12 mars 1994", so it renders in the reader's locale.
    private static let birthDate = DateComponents(
        calendar: .current, timeZone: TimeZone(identifier: "Africa/Douala"),
        year: 1994, month: 3, day: 12
    ).date ?? .now

    private func kv(_ label: Text, _ value: Text) -> some View {
        HStack {
            label.font(.bodyReg).foregroundStyle(Brand.inkMuted)
            Spacer()
            value.font(.bodyReg).foregroundStyle(Brand.ink)
        }
        .padding(.vertical, Metric.rowVertical)
    }
}

// MARK: - Limits

public struct LimitsView: View {
    public init() {
    }

    private struct Limit: Identifiable {
        let id = UUID()
        var title: LocalizedStringKey
        var usage: Text
        var ratio: Double
        var note: LocalizedStringKey
    }

    private var limits: [Limit] {
        [
            .init(title: "Monthly top-up",
                  usage: Text("\(Fmt.xaf(1_240_000)) of \(Fmt.xaf(3_000_000))", bundle: .module),
                  ratio: 0.41, note: "Renews on the 1st of next month"),
            .init(title: "Card payments per month",
                  usage: Text("\(Fmt.usd(48_600)) of \(Fmt.usd(200_000))", bundle: .module),
                  ratio: 0.24, note: "Across every card"),
            .init(title: "Member-to-member transfer",
                  usage: Text("\(Fmt.xaf(150_000)) of \(Fmt.xaf(500_000))", bundle: .module),
                  ratio: 0.30, note: "Per 30-day period"),
            .init(title: "Active cards",
                  usage: Text("\(3) of \(5)", bundle: .module),
                  ratio: 0.60, note: "Level 2 · verified"),
        ]
    }

    public var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                Eyebrow(text: Text("Level \(2)", bundle: .module)).gutter().padding(.top, 18)
                Text("Your current caps", bundle: .module)
                    .font(.system(size: 24, weight: .semibold)).tight(-0.5)
                    .foregroundStyle(Brand.ink).gutter().padding(.top, 8)
                Text("They follow from your verification level and from CEMAC regulation.",
                     bundle: .module)
                    .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                    .fixedSize(horizontal: false, vertical: true)
                    .gutter().padding(.top, 8)

                Rule().padding(.top, 24)

                VStack(spacing: 0) {
                    ForEach(Array(limits.enumerated()), id: \.element.id) { i, limit in
                        VStack(alignment: .leading, spacing: 9) {
                            HStack {
                                Text(limit.title, bundle: .module).font(.bodyReg)
                                    .foregroundStyle(Brand.ink)
                                Spacer()
                                Text(limit.ratio, format: .percent.precision(.fractionLength(0)))
                                    .font(.subMed).foregroundStyle(Brand.inkMuted).monospacedDigit()
                            }
                            Meter(value: limit.ratio, tint: Brand.ink, height: 4)
                            HStack {
                                limit.usage.font(.sub).foregroundStyle(Brand.inkMuted)
                                    .monospacedDigit()
                                Spacer()
                                Text(limit.note, bundle: .module).font(.micro)
                                    .foregroundStyle(Brand.inkFaint)
                            }
                        }
                        .padding(.vertical, 18)
                        if i < limits.count - 1 { Rule() }
                    }
                }
                .gutter()

                Rule()

                VStack(alignment: .leading, spacing: 9) {
                    Text("Move up to level \(3)", bundle: .module).font(.bodyMed)
                        .foregroundStyle(Brand.ink)
                    Text("Add a proof of address and a proof of income to raise your cap to \(Fmt.xaf(10_000_000)).",
                         bundle: .module)
                        .font(.sub).foregroundStyle(Brand.inkMuted)
                        .fixedSize(horizontal: false, vertical: true)
                    MPButton(title: Text("Raise my caps", bundle: .module), tone: .quiet) {}
                        .padding(.top, 6)
                }
                .gutter().padding(.top, 22)
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle(Text("Caps", bundle: .module))
        .navigationBarTitleDisplayMode(.inline)
    }
}

// MARK: - Security

public struct SecurityView: View {
    public init() {
    }

    @State private var twoFA = true
    @State private var confirmEach = false

    public var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                VStack(spacing: 0) {
                    Row(icon: "lock.rotation", title: Text("Change the passcode", bundle: .module),
                        chevron: true)
                    Rule(inset: 51)
                    Row(icon: "key.horizontal", title: Text("Forgotten passcode", bundle: .module),
                        subtitle: Text("Reset by SMS", bundle: .module), chevron: true)
                }
                .gutter().padding(.top, 8)

                Rule()
                Eyebrow(text: Text("Confirmation", bundle: .module)).gutter().padding(.top, 22).padding(.bottom, 2)
                VStack(spacing: 0) {
                    toggle("Two-factor authentication", "shield.lefthalf.filled", $twoFA)
                    Rule(inset: 51)
                    toggle("Confirm every payment", "hand.raised", $confirmEach)
                }
                .gutter()
                Text("""
                    With systematic confirmation, every authorization waits for your approval in \
                    the app. Without an answer it is declined after 20 seconds.
                    """, bundle: .module)
                    .font(.micro).foregroundStyle(Brand.inkFaint)
                    .fixedSize(horizontal: false, vertical: true)
                    .gutter().padding(.top, 10)

                Rule().padding(.top, 20)
                Eyebrow(text: Text("Recent activity", bundle: .module)).gutter().padding(.top, 22).padding(.bottom, 2)
                VStack(spacing: 0) {
                    log("Successful sign-in", Text(verbatim: "iPhone 16 Pro · Douala"),
                        hoursAgo(2), Brand.credit)
                    Rule(inset: 20)
                    log("Passcode changed", Text(verbatim: "iPhone 16 Pro · Douala"),
                        hoursAgo(24 * 16), Brand.inkFaint)
                    Rule(inset: 20)
                    log("Attempt blocked", Text("Unknown device · Lagos", bundle: .module),
                        hoursAgo(24 * 25), Brand.debit)
                }
                .gutter()
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle(Text("Security", bundle: .module))
        .navigationBarTitleDisplayMode(.inline)
    }

    private func hoursAgo(_ hours: Int) -> Date {
        Date.now.addingTimeInterval(-Double(hours) * 3600)
    }

    private func toggle(_ title: LocalizedStringKey, _ icon: String,
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

    private func log(_ title: LocalizedStringKey, _ device: Text,
                     _ date: Date, _ tint: Color) -> some View {
        HStack(spacing: 12) {
            Circle().fill(tint).frame(width: 7, height: 7)
            VStack(alignment: .leading, spacing: 2) {
                Text(title, bundle: .module).font(.bodyReg).foregroundStyle(Brand.ink)
                device.font(.sub).foregroundStyle(Brand.inkMuted)
            }
            Spacer()
            Text(date, format: .relative(presentation: .named))
                .font(.micro).foregroundStyle(Brand.inkFaint)
        }
        .padding(.vertical, 13)
    }
}

// MARK: - Devices

public struct DevicesView: View {
    public init() {
    }

    public var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                VStack(spacing: 0) {
                    Row(icon: "iphone", title: Text(verbatim: "iPhone 16 Pro"),
                        subtitle: Text("This device · Douala", bundle: .module)) {
                        StatusPill(text: Text("Active", bundle: .module), symbol: "circle.fill",
                                   tint: Brand.credit, soft: Brand.creditSoft)
                    }
                    Rule(inset: 51)
                    Row(icon: "ipad", title: Text(verbatim: "iPad Air"),
                        subtitle: Text("Last seen \(Self.lastSeen.formatted(.dateTime.day().month(.wide)))",
                                       bundle: .module)) {
                        Text("Revoke", bundle: .module).font(.subMed).foregroundStyle(Brand.debit)
                    }
                }
                .gutter().padding(.top, 8)
                Rule()
                Text("Revoking a device signs it out at once and invalidates its sessions.",
                     bundle: .module)
                    .font(.micro).foregroundStyle(Brand.inkFaint)
                    .fixedSize(horizontal: false, vertical: true)
                    .gutter().padding(.top, 14)
            }
        }
        .page()
        .navigationTitle(Text("Devices", bundle: .module))
        .navigationBarTitleDisplayMode(.inline)
    }

    private static let lastSeen = Date.now.addingTimeInterval(-10 * 24 * 3600)
}

// MARK: - Documents

public struct DocumentsView: View {
    public init() {
    }

    /// The last five months, formatted per locale rather than typed out.
    private var months: [Date] {
        (0..<5).compactMap { Calendar.current.date(byAdding: .month, value: -$0, to: .now) }
    }

    public var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                Eyebrow(text: Text("Monthly statements", bundle: .module)).gutter().padding(.top, 18).padding(.bottom, 2)
                VStack(spacing: 0) {
                    ForEach(months.indices, id: \.self) { i in
                        Row(icon: "doc.text",
                            title: Text(months[i], format: .dateTime.month(.wide).year()),
                            subtitle: Text("PDF · full statement", bundle: .module)) {
                            Image(systemName: "arrow.down.circle")
                                .font(.system(size: 17)).foregroundStyle(Brand.mark)
                        }
                        if i < months.count - 1 { Rule(inset: 51) }
                    }
                }
                .gutter()

                Rule()
                Eyebrow(text: Text("Supporting documents", bundle: .module)).gutter().padding(.top, 22).padding(.bottom, 2)
                VStack(spacing: 0) {
                    Row(icon: "checkmark.seal",
                        title: Text("Account certificate", bundle: .module),
                        subtitle: Text("Generated on request", bundle: .module), chevron: true)
                    Rule(inset: 51)
                    Row(icon: "person.text.rectangle",
                        title: Text("Identity document", bundle: .module),
                        subtitle: Text("National ID · verified on \(Self.idVerifiedOn.formatted(.dateTime.day().month(.wide).year()))",
                                       bundle: .module),
                        chevron: true)
                }
                .gutter()
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle(Text("Documents", bundle: .module))
        .navigationBarTitleDisplayMode(.inline)
    }

    private static let idVerifiedOn = Date.now.addingTimeInterval(-117 * 24 * 3600)
}

// MARK: - Referrals

public struct ReferralView: View {
    public init() {
    }

    @State private var toastMsg: Toast?
    private let code = "ARISTIDE237"

    public var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                Text("\(Fmt.xaf(2_500)) per friend", bundle: .module)
                    .font(.system(size: 28, weight: .semibold)).tight(-0.7)
                    .foregroundStyle(Brand.ink).gutter().padding(.top, 20)
                Text("Your friend also gets \(Fmt.xaf(2_500)) as soon as they create their first virtual card.",
                     bundle: .module)
                    .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                    .fixedSize(horizontal: false, vertical: true)
                    .gutter().padding(.top, 8)

                Button {
                    UIPasteboard.general.string = code
                    Haptic.success()
                    toastMsg = Toast(text: Text("Code copied", bundle: .module), icon: "doc.on.doc")
                } label: {
                    HStack {
                        VStack(alignment: .leading, spacing: 5) {
                            Eyebrow(text: Text("Your code", bundle: .module))
                            Text(verbatim: code)
                                .font(.system(size: 22, weight: .medium, design: .monospaced))
                                .foregroundStyle(Brand.ink)
                        }
                        Spacer()
                        Image(systemName: "doc.on.doc").font(.system(size: 16))
                            .foregroundStyle(Brand.inkMuted)
                    }
                    .padding(16)
                    .background(Brand.well, in: .rect(cornerRadius: Metric.card, style: .continuous))
                    .contentShape(.rect)
                }
                .buttonStyle(Press())
                .gutter().padding(.top, 26)

                Rule().padding(.top, 26)
                HStack(spacing: 0) {
                    stat(Text("Friends referred", bundle: .module), Text(4, format: .number))
                    Rectangle().fill(Brand.hairline).frame(width: 1, height: 38)
                    stat(Text("Earnings so far", bundle: .module),
                         Text(verbatim: Fmt.xaf(10_000))).padding(.leading, 18)
                }
                .gutter().padding(.vertical, 20)
                Rule()

                MPButton(title: Text("Share my code", bundle: .module),
                         icon: "square.and.arrow.up") {}
                    .gutter().padding(.top, 24)
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle(Text("Referrals", bundle: .module))
        .navigationBarTitleDisplayMode(.inline)
        .toast($toastMsg)
    }

    private func stat(_ label: Text, _ value: Text) -> some View {
        VStack(alignment: .leading, spacing: 5) {
            label.font(.micro).foregroundStyle(Brand.inkMuted)
            value.font(.system(size: 22, weight: .semibold)).monospacedDigit()
                .foregroundStyle(Brand.ink)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }
}

// MARK: - Help

public struct HelpView: View {
    public init() {
    }

    @State private var query = ""
    @State private var openIndex: Int?

    private struct Entry: Identifiable {
        let id: Int
        var question: String
        var answer: String
    }

    /// Resolved to `String`, not held as keys: the search box matches on the
    /// question the reader actually sees.
    private var faq: [Entry] {
        let pairs: [(String.LocalizationValue, String.LocalizationValue)] = [
            ("Why was my payment declined?",
             """
             Most often your FCFA balance did not cover the converted amount at authorization. \
             Top up and try again. Every decline is charged at 220 FCFA.
             """),
            ("How long does a Mobile Money top-up take?",
             """
             MTN MoMo and Orange Money credit your wallet in seconds. A bank transfer takes \
             1 to 2 business days.
             """),
            ("Does my card work on every site?",
             """
             Anywhere Visa and Mastercard are accepted online. Merchants that need a deposit \
             hold — car rental, hotels — refuse virtual cards.
             """),
            ("Which exchange rate is applied?",
             """
             The interbank rate of the day plus a 3 % MoneyPay margin. The breakdown is shown \
             before every conversion and on every receipt.
             """),
            ("What happens if I freeze a card?",
             """
             Every authorization is declined at once. Running subscriptions will fail while the \
             card stays frozen. You can unfreeze it whenever you like.
             """),
        ]
        return pairs.enumerated().map { index, pair in
            Entry(id: index,
                  question: String(localized: pair.0, bundle: .module),
                  answer: String(localized: pair.1, bundle: .module))
        }
    }

    private var filtered: [Entry] {
        faq.filter { query.isEmpty || $0.question.localizedCaseInsensitiveContains(query) }
    }

    public var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                Field(placeholder: Text("Search a question", bundle: .module),
                      text: $query, icon: "magnifyingglass")
                    .gutter().padding(.top, 12)

                VStack(spacing: 0) {
                    ForEach(filtered) { item in
                        VStack(alignment: .leading, spacing: 8) {
                            Button {
                                Haptic.tap()
                                withAnimation(.easeOut(duration: 0.2)) {
                                    openIndex = openIndex == item.id ? nil : item.id
                                }
                            } label: {
                                HStack(alignment: .top, spacing: 10) {
                                    Text(verbatim: item.question).font(.bodyReg)
                                        .foregroundStyle(Brand.ink)
                                        .multilineTextAlignment(.leading)
                                        .fixedSize(horizontal: false, vertical: true)
                                    Spacer(minLength: 8)
                                    Image(systemName: "chevron.down")
                                        .font(.system(size: 11, weight: .bold))
                                        .foregroundStyle(Brand.inkFaint)
                                        .rotationEffect(.degrees(openIndex == item.id ? 180 : 0))
                                }
                                .contentShape(.rect)
                            }
                            .buttonStyle(.plain)

                            if openIndex == item.id {
                                Text(verbatim: item.answer).font(.sub)
                                    .foregroundStyle(Brand.inkMuted)
                                    .fixedSize(horizontal: false, vertical: true)
                            }
                        }
                        .padding(.vertical, 16)
                        if item.id != filtered.last?.id { Rule() }
                    }
                }
                .gutter().padding(.top, 12)

                Rule()
                VStack(alignment: .leading, spacing: 9) {
                    HStack(spacing: 7) {
                        Circle().fill(Brand.credit).frame(width: 7, height: 7)
                        Text("Advisers online", bundle: .module).font(.bodyMed)
                            .foregroundStyle(Brand.ink)
                    }
                    Text("Monday to Saturday, 8 am – 8 pm (WAT). Average reply in 4 minutes.",
                         bundle: .module)
                        .font(.sub).foregroundStyle(Brand.inkMuted)
                    MPButton(title: Text("Start a chat", bundle: .module), tone: .quiet) {}
                        .padding(.top, 6)
                }
                .gutter().padding(.top, 22)
            }
            .padding(.bottom, 28)
        }
        .scrollIndicators(.hidden)
        .page()
        .navigationTitle(Text("Help centre", bundle: .module))
        .navigationBarTitleDisplayMode(.inline)
    }
}

// MARK: - Notifications

public struct NotificationsView: View {
    public init() {
    }

    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    public var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 0) {
                    ForEach(Array(store.notifications.enumerated()), id: \.element.id) { i, n in
                        HStack(alignment: .top, spacing: 12) {
                            IconTile(symbol: n.symbol, tint: n.tint)
                            VStack(alignment: .leading, spacing: 3) {
                                HStack(spacing: 6) {
                                    Text(verbatim: n.title).font(.bodyMed)
                                        .foregroundStyle(Brand.ink)
                                    if n.unread { Circle().fill(Brand.mark).frame(width: 6, height: 6) }
                                }
                                Text(verbatim: n.body).font(.sub).foregroundStyle(Brand.inkMuted)
                                    .fixedSize(horizontal: false, vertical: true)
                                Text(verbatim: "\(Fmt.relativeDay(n.date)) · \(Fmt.time(n.date))")
                                    .font(.micro).foregroundStyle(Brand.inkFaint)
                            }
                            Spacer(minLength: 0)
                        }
                        .padding(.vertical, 16)
                        if i < store.notifications.count - 1 { Rule(inset: 51) }
                    }
                }
                .gutter()
                .padding(.bottom, 28)
            }
            .scrollIndicators(.hidden)
            .page()
            .navigationTitle(Text("Notifications", bundle: .module))
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .topBarLeading) {
                    Button { dismiss() } label: { Text("Close", bundle: .module) }
                        .foregroundStyle(Brand.inkMuted)
                }
                ToolbarItem(placement: .topBarTrailing) {
                    Button { store.markAllNotificationsRead() } label: {
                        Text("Mark all read", bundle: .module)
                    }
                    .font(.subMed).foregroundStyle(Brand.ink)
                }
            }
        }
    }
}
