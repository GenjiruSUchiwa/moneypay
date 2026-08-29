import SwiftUI

/// Button emphasis: `.primary` is green, `.ink` is monochrome, and `.danger` is destructive.
/// `.quiet` fills a well, `.outline` draws a border, and `.ghost` uses muted ink without either.
public enum Tone { case primary, ink, quiet, outline, ghost, danger }

/// Full-width button with standard MoniPay emphasis and loading states.
public struct MPButton: View {
    public init(
        title: Text,
        icon: String? = nil,
        tone: Tone = .ink,
        loading: Bool = false,
        enabled: Bool = true,
        action: @escaping () -> Void
    ) {
        self.title = title
        self.icon = icon
        self.tone = tone
        self.loading = loading
        self.enabled = enabled
        self.action = action
    }

    public var title: Text
    public var icon: String? = nil
    public var tone: Tone = .ink
    public var loading = false
    public var enabled = true
    public var action: () -> Void

    public var body: some View {
        Button(action: action) {
            HStack(spacing: 8) {
                if loading { ProgressView().controlSize(.small).tint(fg) }
                else if let icon { Image(systemName: icon).font(.system(size: 15, weight: .semibold)) }
                title.font(.system(size: 16, weight: .medium))
            }
            .frame(maxWidth: .infinity)
            .frame(height: 52)
            .foregroundStyle(fg)
            .background(bg, in: .capsule)
            .overlay {
                if tone == .outline {
                    Capsule().stroke(Brand.rule, lineWidth: 1)
                }
            }
            .opacity(enabled ? 1 : 0.35)
        }
        .buttonStyle(Press())
        .disabled(!enabled || loading)
    }

    private var bg: Color {
        switch tone {
        case .primary: Brand.action
        case .ink: Brand.inkFill
        case .quiet: Brand.well
        case .outline: .clear
        case .ghost: .clear
        case .danger: Brand.debitSoft
        }
    }
    private var fg: Color {
        switch tone {
        case .primary: Brand.onAction
        case .ink: Brand.onInk
        case .quiet, .outline: Brand.ink
        case .ghost: Brand.inkMuted
        case .danger: Brand.debit
        }
    }
}

#Preview("MPButton, every tone") {
    VStack(spacing: 12) {
        MPButton(title: Text(verbatim: "Primary action"), tone: .primary) {}
        MPButton(title: Text(verbatim: "Top up"), tone: .ink) {}
        MPButton(title: Text(verbatim: "Convert"), icon: "arrow.left.arrow.right", tone: .quiet) {}
        MPButton(title: Text(verbatim: "New card"), tone: .outline) {}
        MPButton(title: Text(verbatim: "Later"), tone: .ghost) {}
        MPButton(title: Text(verbatim: "Delete"), tone: .danger) {}
        MPButton(title: Text(verbatim: "Loading"), loading: true) {}
        MPButton(title: Text(verbatim: "Disabled"), enabled: false) {}
    }
    .gutter()
    .page()
}
