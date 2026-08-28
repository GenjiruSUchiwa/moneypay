import SwiftUI

public enum Tone { case ink, quiet, outline, danger }

public struct MPButton: View {
    public init(
        title: String,
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

    public var title: String
    public var icon: String? = nil
    public var tone: Tone = .ink
    public var loading = false
    public var enabled = true
    public var action: () -> Void

    public var body: some View {
        Button { Haptic.tap(); action() } label: {
            HStack(spacing: 8) {
                if loading { ProgressView().tint(fg).scaleEffect(0.8) }
                else if let icon { Image(systemName: icon).font(.system(size: 15, weight: .semibold)) }
                Text(title).font(.system(size: 16, weight: .medium))
            }
            .frame(maxWidth: .infinity)
            .frame(height: 52)
            .foregroundStyle(fg)
            .background(bg, in: .rect(cornerRadius: Metric.control))
            .overlay {
                if tone == .outline {
                    RoundedRectangle(cornerRadius: Metric.control).stroke(Brand.rule, lineWidth: 1)
                }
            }
            .opacity(enabled ? 1 : 0.35)
        }
        .buttonStyle(Press())
        .disabled(!enabled || loading)
    }

    private var bg: Color {
        switch tone {
        case .ink: Brand.inkFill
        case .quiet: Brand.well
        case .outline: .clear
        case .danger: Brand.debitSoft
        }
    }
    private var fg: Color {
        switch tone {
        case .ink: Brand.onInk
        case .quiet, .outline: Brand.ink
        case .danger: Brand.debit
        }
    }
}

#Preview("MPButton, every tone") {
    VStack(spacing: 12) {
        MPButton(title: "Top up", tone: .ink) {}
        MPButton(title: "Convert", icon: "arrow.left.arrow.right", tone: .quiet) {}
        MPButton(title: "New card", tone: .outline) {}
        MPButton(title: "Delete", tone: .danger) {}
        MPButton(title: "Loading", loading: true) {}
        MPButton(title: "Disabled", enabled: false) {}
    }
    .gutter()
    .page()
}
