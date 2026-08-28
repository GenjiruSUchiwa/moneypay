import DesignSystem
import SwiftUI

public struct SignUpFlow: View {
    public init(onDone: @escaping () -> Void) {
        self.onDone = onDone
    }

    public var onDone: () -> Void
    @State private var step = 0
    private let total = 5

    public var body: some View {
        VStack(spacing: 0) {
            HStack(spacing: 16) {
                Button {
                    Haptic.tap()
                    withAnimation(Motion.quick) { step = max(0, step - 1) }
                } label: {
                    Image(systemName: "chevron.backward")
                        .font(.system(size: 17, weight: .semibold))
                        .foregroundStyle(Brand.ink)
                        .frame(width: 36, height: 36)
                        .contentShape(.rect)
                }
                .opacity(step == 0 ? 0 : 1).disabled(step == 0)

                SegmentedProgress(count: total, current: step)
                    .animation(Motion.quick, value: step)
            }
            .padding(.horizontal, Metric.gutter - 10)
            .padding(.trailing, 10)
            .frame(height: 46)

            ZStack {
                switch step {
                case 0: PhoneStep(next: next)
                case 1: OTPStep(next: next)
                case 2: PasscodeStep(next: next)
                case 3: BiometricStep(next: next)
                default: ProfileStep(next: onDone)
                }
            }
            .transition(.opacity)
        }
        .page()
    }

    private func next() {
        Haptic.tap()
        withAnimation(Motion.quick) { step += 1 }
    }
}
