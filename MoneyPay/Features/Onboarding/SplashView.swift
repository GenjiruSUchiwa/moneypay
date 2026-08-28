import SwiftUI

struct SplashView: View {
    var onFinish: () -> Void
    @State private var shown = false

    var body: some View {
        ZStack {
            Brand.inkFill.ignoresSafeArea()
            VStack(spacing: 14) {
                LogoMark(size: 56, tint: Brand.onInk, glyph: Brand.inkFill)
                Text("MoneyPay")
                    .font(.system(size: 22, weight: .semibold))
                    .tight(-0.3)
                    .foregroundStyle(Brand.onInk)
            }
            .opacity(shown ? 1 : 0)
            .offset(y: shown ? 0 : 8)

            VStack {
                Spacer()
                Text("Établissement de paiement agréé · zone CEMAC")
                    .font(.micro)
                    .foregroundStyle(Brand.onInk.opacity(0.45))
                    .padding(.bottom, 40)
                    .opacity(shown ? 1 : 0)
            }
        }
        .task {
            withAnimation(.easeOut(duration: 0.5)) { shown = true }
            try? await Task.sleep(for: .milliseconds(1300))
            onFinish()
        }
    }
}
