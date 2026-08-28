import DesignSystem
import SwiftUI

public struct SplashView: View {
    public init(onFinish: @escaping () -> Void) {
        self.onFinish = onFinish
    }

    public var onFinish: () -> Void
    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    @State private var model = SplashModel()

    private var shown: Bool { model.phase != .hidden }
    private var rise: CGFloat { shown || reduceMotion ? 0 : Motion.rise }

    public var body: some View {
        ZStack {
            Brand.greenDeep.ignoresSafeArea()
            VStack(spacing: Metric.rowVertical) {
                LogoMark(size: 60, tint: Brand.deepInkFill, glyph: Brand.deepInk)
                Text(verbatim: "MoniPay")
                    .font(.titleLarge).tight()
                    .foregroundStyle(Brand.deepInk)
            }
            .opacity(shown ? 1 : 0)
            .offset(y: rise)

            VStack {
                Spacer()
                Text("Licensed payment institution · CEMAC zone", bundle: .module)
                    .font(.micro)
                    .foregroundStyle(Brand.deepInkMuted)
                    .padding(.bottom, Metric.section)
                    .opacity(shown ? 1 : 0)
            }
        }
        .animation(reduceMotion ? nil : Motion.screen, value: shown)
        .accessibilityElement(children: .combine)
        .task {
            await model.run()
            if model.phase == .finished { onFinish() }
        }
    }
}

#Preview("Splash — light") {
    SplashView(onFinish: {})
}

#Preview("Splash — dark") {
    SplashView(onFinish: {})
        .preferredColorScheme(.dark)
}
