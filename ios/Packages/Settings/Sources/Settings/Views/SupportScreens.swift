import DesignSystem
import Foundation
import SwiftUI
import WalletStore

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
             """)
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
