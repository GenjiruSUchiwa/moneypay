import SwiftUI

/// The monogram. Defaults mirror the prototype's `logoMark()`: the action green tile with
/// the `onAction` glyph — the prototype hardcodes white, which fails contrast on the dark
/// green. Pass `tint`/`glyph` only for a translucent variant on a coloured surface
/// (splash, card art).
public struct LogoMark: View {
    public init(size: CGFloat = 40, tint: Color = Brand.action, glyph: Color = Brand.onAction) {
        self.size = size
        self.tint = tint
        self.glyph = glyph
    }

    public var size: CGFloat = 40
    public var tint: Color = Brand.action
    public var glyph: Color = Brand.onAction

    public var body: some View {
        ZStack {
            RoundedRectangle(cornerRadius: size * 0.28, style: .continuous).fill(tint)
            Path { p in
                p.move(to: .init(x: 0.24 * size, y: 0.72 * size))
                p.addLine(to: .init(x: 0.24 * size, y: 0.30 * size))
                p.addLine(to: .init(x: 0.50 * size, y: 0.56 * size))
                p.addLine(to: .init(x: 0.76 * size, y: 0.30 * size))
                p.addLine(to: .init(x: 0.76 * size, y: 0.72 * size))
            }
            .stroke(glyph, style: .init(lineWidth: size * 0.095, lineCap: .square, lineJoin: .miter))
        }
        .frame(width: size, height: size)
    }
}

public struct Wordmark: View {
    public init(size: CGFloat = 18) {
        self.size = size
    }

    public var size: CGFloat = 18
    public var body: some View {
        HStack(spacing: 8) {
            LogoMark(size: size * 1.35)
            Text(verbatim: "MoniPay")
                .font(.system(size: size, weight: .semibold))
                .tight(-0.3)
                .foregroundStyle(Brand.ink)
        }
    }
}

#Preview("LogoMark and Wordmark") {
    VStack(spacing: 20) {
        HStack(spacing: 16) {
            LogoMark(size: 28)
            LogoMark(size: 40)
            LogoMark(size: 56)
        }
        Wordmark(size: 18)
        LogoMark(size: 60, tint: Brand.deepInkFill, glyph: Brand.deepInk)
            .frame(maxWidth: .infinity)
            .padding()
            .background(Brand.greenDeep)
    }
    .padding()
    .page()
}
