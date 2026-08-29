import DesignSystem
import Money
import SwiftUI
import WalletStore

public struct CardControlsView: View {
    public var card: VirtualCard
    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    @State private var draft: VirtualCard
    @State private var limitIndex: Int
    @State private var saved = false

    private static let presets: [Int?] = [5_000, 15_000, 50_000, 100_000, nil]

    public init(card: VirtualCard) {
        self.card = card
        _draft = State(initialValue: card)
        _limitIndex = State(initialValue: Self.presets.firstIndex { $0 == card.monthlyLimitUSDCents } ?? 4)
    }

    public var body: some View {
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 0) {
                    VirtualCardView(card: draft, compact: true)
                        .frame(maxWidth: 210)
                        .frame(maxWidth: .infinity, alignment: .center)
                        .padding(.top, 6)

                    Rule().padding(.top, 26)
                    limitSection
                    Rule()
                    permissions
                    Rule()
                    security
                }
                .padding(.bottom, 26)
            }
            .scrollIndicators(.hidden)
            .page()
            .navigationTitle(Text("Controls", bundle: .module))
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .topBarLeading) {
                    Button { dismiss() } label: { Text("Cancel", bundle: .module) }
                        .foregroundStyle(Brand.inkMuted)
                }
                ToolbarItem(placement: .topBarTrailing) {
                    Button {
                        draft.monthlyLimitUSDCents = Self.presets[limitIndex]
                        store.update(draft); saved = true; dismiss()
                    } label: {
                        Text("Save", bundle: .module)
                    }
                    .font(.bodyMed).foregroundStyle(Brand.ink)
                }
            }
            .sensoryFeedback(.success, trigger: saved)
        }
    }

    private var limitSection: some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: Text("Monthly cap", bundle: .module)).gutter().padding(.top, 22)

            Group {
                if let v = Self.presets[limitIndex] { MoneyText.usd(v, size: 30) }
                else {
                    Text("No cap", bundle: .module)
                        .font(.system(size: 30, weight: .semibold)).tight(-0.6)
                }
            }
            .foregroundStyle(Brand.ink)
            .gutter().padding(.top, 10)

            ScrollView(.horizontal) {
                HStack(spacing: 7) {
                    ForEach(Self.presets.indices, id: \.self) { i in
                        Chip(text: Self.presets[i].map { Text(verbatim: Fmt.usd($0)) }
                                ?? Text("Unlimited", bundle: .module),
                             selected: i == limitIndex) {
                            withAnimation(.easeOut(duration: 0.18)) { limitIndex = i }
                        }
                    }
                }
                .gutter()
            }
            .scrollIndicators(.hidden)
            .padding(.top, 16)

            Text("Past that, every authorization is declined automatically. A decline costs \(Fmt.xaf(220)).",
                 bundle: .module)
                .font(.micro).foregroundStyle(Brand.inkFaint)
                .fixedSize(horizontal: false, vertical: true)
                .gutter().padding(.top, 12).padding(.bottom, 22)
        }
    }

    private var permissions: some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: Text("Permissions", bundle: .module)).gutter().padding(.top, 22).padding(.bottom, 2)
            VStack(spacing: 0) {
                toggleRow("Online payments", "globe", $draft.onlineAllowed)
                Rule(inset: 51)
                toggleRow("Recurring subscriptions", "arrow.triangle.2.circlepath",
                          $draft.subscriptionsAllowed)
                Rule(inset: 51)
                toggleRow("Single use", "1.circle", $draft.singleUse)
            }
            .gutter()
            Text("A single-use card deletes itself after the first successful payment.", bundle: .module)
                .font(.micro).foregroundStyle(Brand.inkFaint)
                .fixedSize(horizontal: false, vertical: true)
                .gutter().padding(.top, 10).padding(.bottom, 22)
        }
    }

    private var security: some View {
        VStack(alignment: .leading, spacing: 0) {
            Eyebrow(text: Text("Security", bundle: .module)).gutter().padding(.top, 22).padding(.bottom, 2)
            VStack(spacing: 0) {
                toggleRow("Freeze the card", "snowflake", $draft.isFrozen)
                Rule(inset: 51)
                Row(icon: "globe.europe.africa", title: Text("Allowed countries", bundle: .module),
                    chevron: true) {
                    RowValue(text: Text("All", bundle: .module))
                }
                Rule(inset: 51)
                Row(icon: "bell.badge", title: Text("Alert on every payment", bundle: .module),
                    chevron: true) {
                    RowValue(text: Text("On", bundle: .module))
                }
            }
            .gutter()
        }
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
