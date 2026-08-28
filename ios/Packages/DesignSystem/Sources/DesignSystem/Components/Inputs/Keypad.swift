import SwiftUI

/// House keypad. Keys have no background at rest; it appears only on press.
/// A keyboard full of grey pills weighs the screen down.
public struct Keypad: View {
    public init(
        side: SideKey = .none,
        onDigit: @escaping (Int) -> Void,
        onDelete: @escaping () -> Void,
        onSide: @escaping () -> Void = {}
    ) {
        self.side = side
        self.onDigit = onDigit
        self.onDelete = onDelete
        self.onSide = onSide
    }

    /// Public because it appears in `init`: the left-hand key changes with
    /// the screen (decimal separator for an amount, biometrics for unlock).
    public enum SideKey: Equatable, Sendable { case none, decimal, biometric }

    public var side: SideKey = .none
    public var onDigit: (Int) -> Void
    public var onDelete: () -> Void
    public var onSide: () -> Void = {}

    private let rows = [[1, 2, 3], [4, 5, 6], [7, 8, 9]]

    public var body: some View {
        VStack(spacing: 2) {
            ForEach(rows, id: \.self) { row in
                HStack(spacing: 2) {
                    ForEach(row, id: \.self) { d in
                        key(Text(d, format: .number).font(.system(size: 25, weight: .regular))) { onDigit(d) }
                    }
                }
            }
            HStack(spacing: 2) {
                switch side {
                case .none:
                    Color.clear.frame(maxWidth: .infinity, maxHeight: .infinity)
                case .decimal:
                    key(Text(verbatim: Locale.current.decimalSeparator ?? ",").font(.system(size: 25, weight: .regular))) { onSide() }
                case .biometric:
                    key(Image(systemName: "faceid").font(.system(size: 21, weight: .regular))) { onSide() }
                }
                key(Text(0, format: .number).font(.system(size: 25, weight: .regular))) { onDigit(0) }
                key(Image(systemName: "delete.left").font(.system(size: 20, weight: .regular))) { onDelete() }
            }
        }
        .monospacedDigit()
        .foregroundStyle(Brand.ink)
        .frame(height: 248)
    }

    private func key(_ label: some View, action: @escaping () -> Void) -> some View {
        Button { Haptic.soft(); action() } label: {
            label.frame(maxWidth: .infinity, maxHeight: .infinity).contentShape(.rect)
        }
        .buttonStyle(KeyStyle())
    }

    private struct KeyStyle: ButtonStyle {
        func makeBody(configuration: Configuration) -> some View {
            configuration.label
                .background(configuration.isPressed ? Brand.well : .clear,
                            in: .rect(cornerRadius: 10, style: .continuous))
                .animation(.easeOut(duration: 0.1), value: configuration.isPressed)
        }
    }
}

#Preview("Keypad") {
    VStack(spacing: 30) {
        Keypad(side: .decimal, onDigit: { _ in }, onDelete: {})
        Keypad(side: .biometric, onDigit: { _ in }, onDelete: {}, onSide: {})
    }
    .padding()
    .page()
}
