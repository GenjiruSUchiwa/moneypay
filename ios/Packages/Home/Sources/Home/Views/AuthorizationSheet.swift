import DesignSystem
import Money
import SwiftUI
import WalletStore

/// The pivotal moment of the whole setup: the processor asks over a webhook
/// whether we approve a payment, and we have seconds to answer. This is that
/// decision as the cardholder sees it.
public struct AuthorizationSheet: View {
    public init(merchant: String, category: TxCategory, amountUSDCents: Int, card: VirtualCard) {
        self.merchant = merchant
        self.category = category
        self.amountUSDCents = amountUSDCents
        self.card = card
    }

    public var merchant: String
    public var category: TxCategory
    public var amountUSDCents: Int
    public var card: VirtualCard

    @Environment(Store.self) private var store
    @Environment(\.dismiss) private var dismiss

    @State private var remaining = 20.0
    @State private var outcome: Outcome?

    private enum Outcome { case approved, declined, expired }

    private var amountXAF: Int { store.fx.xaf(fromUSDCents: amountUSDCents) }
    private var after: Int { store.balanceXAF - amountXAF }
    private var enough: Bool { after >= 0 }

    public var body: some View {
        Group { if let outcome { result(outcome) } else { request } }
            .frame(maxWidth: .infinity, maxHeight: .infinity)
            .page()
            .sensoryFeedback(.warning, trigger: outcome) { _, new in new == .declined }
    }

    private var request: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack(spacing: 7) {
                Circle().fill(Brand.pending).frame(width: 6, height: 6)
                Eyebrow(text: Text("Authorization pending", bundle: .module))
                Spacer()
                Text("\(Int(remaining.rounded())) s", bundle: .module)
                    .font(.eyebrow).monospacedDigit()
                    .foregroundStyle(remaining < 6 ? Brand.debit : Brand.inkMuted)
            }
            .padding(.top, 22)

            // Countdown: a bar that drains, not a decorative ring.
            GeometryReader { geo in
                ZStack(alignment: .leading) {
                    Rectangle().fill(Brand.hairline)
                    Rectangle().fill(remaining < 6 ? Brand.debit : Brand.ink)
                        .frame(width: geo.size.width * (remaining / 20))
                }
            }
            .frame(height: 2)
            .padding(.top, 10)

            IconTile(symbol: category.symbol, tint: category.tint, size: 44).padding(.top, 26)

            Text(verbatim: merchant).font(.heading3).foregroundStyle(Brand.inkMuted).padding(.top, 14)
            MoneyText.usd(amountUSDCents, size: 40).padding(.top, 2)
            Text("that is \(Fmt.xaf(amountXAF)) at today's rate", bundle: .module)
                .font(.sub).foregroundStyle(Brand.inkMuted).padding(.top, 6)

            Rule().padding(.top, 26)
            kv(Text("Card", bundle: .module),
               Text(verbatim: "\(card.label) · •• \(card.last4)"))
            Rule()
            kv(Text("Current balance", bundle: .module), Text(verbatim: Fmt.xaf(store.balanceXAF)))
            Rule()
            kv(Text("Balance after payment", bundle: .module),
               enough ? Text(verbatim: Fmt.xaf(after)) : Text("Not enough", bundle: .module),
               tint: enough ? Brand.ink : Brand.debit)
            Rule()

            Spacer(minLength: 16)

            VStack(spacing: 9) {
                MPButton(title: Text(enough ? "Approve the payment" : "Insufficient balance",
                                     bundle: .module),
                         enabled: enough) {
                    withAnimation(.easeOut(duration: 0.25)) { outcome = .approved }
                }
                MPButton(title: Text("Decline", bundle: .module), tone: .danger) {
                    withAnimation(.easeOut(duration: 0.25)) { outcome = .declined }
                }
            }

            Text("A decline costs \(Fmt.xaf(220)) from the processor.", bundle: .module)
                .font(.micro).foregroundStyle(Brand.inkFaint).padding(.top, 10)
        }
        .gutter()
        .padding(.bottom, 18)
        .task {
            while remaining > 0, outcome == nil, !Task.isCancelled {
                try? await Task.sleep(for: .milliseconds(100))
                remaining -= 0.1
            }
            if outcome == nil {
                withAnimation(.easeOut(duration: 0.25)) { outcome = .expired }
            }
        }
    }

    private func kv(_ label: Text, _ value: Text, tint: Color = Brand.ink) -> some View {
        HStack {
            label.font(.bodyReg).foregroundStyle(Brand.inkMuted)
            Spacer()
            value.font(.bodyReg).foregroundStyle(tint).monospacedDigit()
        }
        .padding(.vertical, 13)
    }

    @ViewBuilder
    private func result(_ o: Outcome) -> some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer()
            if o == .approved {
                SuccessMark()
            } else {
                Image(systemName: o == .expired ? "clock" : "xmark")
                    .font(.system(size: 24, weight: .semibold))
                    .foregroundStyle(Brand.onAction)
                    .frame(width: 56, height: 56)
                    .background(Brand.debit, in: .circle)
            }

            Group {
                switch o {
                case .approved: Text("Payment approved", bundle: .module)
                case .expired: Text("Authorization expired", bundle: .module)
                case .declined: Text("Payment declined", bundle: .module)
                }
            }
                .font(.system(size: 26, weight: .semibold)).tight(-0.6)
                .foregroundStyle(Brand.ink).padding(.top, 24)

            Group {
                switch o {
                case .approved:
                    Text("\(Fmt.xaf(amountXAF)) debited from your wallet and paid to \(merchant).",
                         bundle: .module)
                case .expired:
                    Text("You did not answer in time. The merchant got an automatic decline.",
                         bundle: .module)
                case .declined:
                    Text("\(merchant) got a decline. Nothing was debited.", bundle: .module)
                }
            }
                .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true).padding(.top, 8)

            if o != .approved {
                HStack(alignment: .top, spacing: 10) {
                    Image(systemName: "exclamationmark.triangle.fill")
                        .font(.system(size: 12)).foregroundStyle(Brand.pending).padding(.top, 2)
                    Text("\(card.declineCount + 1) declines on this card this month. It blocks automatically at 3.",
                         bundle: .module)
                        .font(.sub).foregroundStyle(Brand.inkMuted)
                        .fixedSize(horizontal: false, vertical: true)
                }
                .padding(.top, 22)
            }

            Spacer()
            MPButton(title: Text("Close", bundle: .module)) { dismiss() }
        }
        .gutter()
        .padding(.bottom, 18)
    }
}

// MARK: - Unlock

public struct LockScreenView: View {
    public init(onUnlock: @escaping () -> Void = {}) {
        self.onUnlock = onUnlock
    }

    public var onUnlock: () -> Void = {}
    @Environment(Store.self) private var store
    @State private var code = ""
    @State private var error = false
    @State private var unlocked = false

    public var body: some View {
        VStack(spacing: 0) {
            Spacer()
            LogoMark(size: 44)
            Text("Welcome back, \(store.user.firstName)", bundle: .module)
                .font(.heading3).foregroundStyle(Brand.ink).padding(.top, 18)
            Group {
                if error {
                    Text("Wrong code · \(2) attempts left", bundle: .module)
                } else {
                    Text("Enter your passcode", bundle: .module)
                }
            }
                .font(.sub)
                .foregroundStyle(error ? Brand.debit : Brand.inkMuted)
                .padding(.top, 6)

            PasscodeDots(code: $code, error: error).padding(.top, 32)

            Spacer()

            HStack(spacing: Metric.large) {
                Button { unlock() } label: {
                    Label {
                        Text("Unlock with Face ID", bundle: .module)
                    } icon: {
                        Image(systemName: "faceid")
                    }
                    .font(.subMed).foregroundStyle(Brand.mark)
                }
                Button {} label: {
                    Text("Forgot your code?", bundle: .module).font(.subMed).foregroundStyle(Brand.mark)
                }
            }
            .padding(.top, Metric.small)
        }
        .gutter()
        .padding(.bottom, 20)
        .page()
        // A typed digit clears the error; a cleared field after a wrong code keeps it.
        .onChange(of: code) { old, new in
            if new.count > old.count { error = false }
            if new.count == 4 { check() }
        }
        .sensoryFeedback(.success, trigger: unlocked)
        .sensoryFeedback(.warning, trigger: error) { _, new in new }
    }

    private func check() {
        if code == "1234" { unlock() }
        else {
            withAnimation { error = true }
            code = ""
        }
    }

    private func unlock() {
        unlocked = true
        onUnlock()
    }
}
