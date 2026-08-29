import DesignSystem
import SwiftUI

public struct KYCFlow: View {
    public init(onDone: @escaping () -> Void, onSkip: @escaping () -> Void) {
        self.onDone = onDone
        self.onSkip = onSkip
    }

    public var onDone: () -> Void
    public var onSkip: () -> Void
    @State private var step = 0

    public var body: some View {
        NavigationStack {
            ZStack {
                switch step {
                case 0: KYCIntroView(onStart: { advance() }, onLater: onSkip)
                case 1: KYCDocumentPickerView(onPick: { _ in advance() }, onBack: { back() })
                case 2: KYCCaptureView(mode: .document, onNext: { advance() }, onBack: { back() })
                case 3: KYCCaptureView(mode: .selfie, onNext: { advance() }, onBack: { back() })
                default: KYCReviewView(onDone: onDone)
                }
            }
            .page()
            .animation(.easeOut(duration: 0.22), value: step)
        }
    }

    private func advance() { step += 1 }
    private func back() { step = max(0, step - 1) }
}
