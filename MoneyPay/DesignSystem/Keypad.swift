import SwiftUI

/// Pavé numérique maison. Touches sans fond au repos — le fond n'apparaît
/// qu'à l'appui. Un clavier plein de pilules grises alourdit l'écran.
struct Keypad: View {
    enum SideKey: Equatable { case none, decimal, biometric }

    var side: SideKey = .none
    var onDigit: (Int) -> Void
    var onDelete: () -> Void
    var onSide: () -> Void = {}

    private let rows = [[1, 2, 3], [4, 5, 6], [7, 8, 9]]

    var body: some View {
        VStack(spacing: 2) {
            ForEach(rows, id: \.self) { row in
                HStack(spacing: 2) {
                    ForEach(row, id: \.self) { d in
                        key(Text("\(d)").font(.system(size: 25, weight: .regular))) { onDigit(d) }
                    }
                }
            }
            HStack(spacing: 2) {
                switch side {
                case .none:
                    Color.clear.frame(maxWidth: .infinity, maxHeight: .infinity)
                case .decimal:
                    key(Text(",").font(.system(size: 25, weight: .regular))) { onSide() }
                case .biometric:
                    key(Image(systemName: "faceid").font(.system(size: 21, weight: .regular))) { onSide() }
                }
                key(Text("0").font(.system(size: 25, weight: .regular))) { onDigit(0) }
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

/// Saisie de montant : chiffre courant + curseur, devise posée en suffixe.
struct AmountEntry: View {
    var digits: String
    var currency: String
    var size: CGFloat = 46

    @State private var blink = true

    var body: some View {
        HStack(alignment: .firstTextBaseline, spacing: 3) {
            Text(digits.isEmpty ? "0" : digits)
                .font(.system(size: size, weight: .semibold))
                .tracking(-1)
                .foregroundStyle(digits.isEmpty ? Brand.inkFaint : Brand.ink)
                .monospacedDigit()
                .contentTransition(.numericText())
            Rectangle()
                .fill(Brand.ink)
                .frame(width: 2, height: size * 0.78)
                .opacity(blink ? 1 : 0)
            Text(currency)
                .font(.system(size: size * 0.4, weight: .medium))
                .foregroundStyle(Brand.inkMuted)
        }
        .animation(.spring(response: 0.22, dampingFraction: 0.85), value: digits)
        .task {
            while !Task.isCancelled {
                try? await Task.sleep(for: .milliseconds(560))
                blink.toggle()
            }
        }
    }
}

/// Points d'un code secret.
struct PasscodeDots: View {
    var filled: Int
    var total: Int = 4
    var error = false

    var body: some View {
        HStack(spacing: 16) {
            ForEach(0..<total, id: \.self) { i in
                Circle()
                    .fill(i < filled ? (error ? Brand.debit : Brand.ink) : .clear)
                    .frame(width: 12, height: 12)
                    .overlay { Circle().stroke(i < filled ? .clear : Brand.rule, lineWidth: 1.2) }
                    .animation(.spring(response: 0.2, dampingFraction: 0.6), value: filled)
            }
        }
        .modifier(Shake(shakes: error ? 1 : 0))
        .animation(.default, value: error)
    }
}

struct Shake: GeometryEffect {
    var shakes: CGFloat
    var animatableData: CGFloat { get { shakes } set { shakes = newValue } }
    func effectValue(size: CGSize) -> ProjectionTransform {
        ProjectionTransform(CGAffineTransform(translationX: sin(shakes * .pi * 4) * 11, y: 0))
    }
}

/// Cases de code OTP : trait sous le chiffre, pas de boîte.
struct OTPBoxes: View {
    var code: String
    var length: Int = 6

    var body: some View {
        HStack(spacing: 12) {
            ForEach(0..<length, id: \.self) { i in
                let ch = i < code.count ? String(Array(code)[i]) : ""
                let active = i == code.count
                VStack(spacing: 9) {
                    Text(ch.isEmpty ? " " : ch)
                        .font(.system(size: 27, weight: .medium))
                        .monospacedDigit()
                        .foregroundStyle(Brand.ink)
                        .contentTransition(.numericText())
                    Rectangle()
                        .fill(active ? Brand.ink : Brand.rule)
                        .frame(height: active ? 2 : 1)
                }
            }
        }
        .animation(.spring(response: 0.22, dampingFraction: 0.85), value: code)
    }
}
