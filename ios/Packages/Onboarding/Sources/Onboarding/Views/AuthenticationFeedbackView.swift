import DesignSystem
import SwiftUI

struct AuthenticationFeedbackView: View {
    let feedback: AuthenticationFeedback
    var onRecovery: () -> Void = {}

    var body: some View {
        EmptyNote(
            title: Text(feedback.title, bundle: .module),
            message: feedback.message,
            actionTitle: feedback.actionTitle.map { Text($0, bundle: .module) },
            action: onRecovery
        )
        .accessibilityElement(children: .contain)
    }
}

#Preview("Verification feedback — fr") {
    ScrollView {
        VStack {
            AuthenticationFeedbackView(feedback: .queued)
            AuthenticationFeedbackView(feedback: .invalidCode(attemptsRemaining: 3))
            AuthenticationFeedbackView(feedback: .deliveryFailed)
            AuthenticationFeedbackView(feedback: .locked)
        }
        .gutter()
    }
    .page()
    .environment(\.locale, Locale(identifier: "fr"))
}
