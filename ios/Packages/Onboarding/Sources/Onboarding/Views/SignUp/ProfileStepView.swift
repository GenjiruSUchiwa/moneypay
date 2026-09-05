import ApiClient
import DesignSystem
import Money
import SwiftUI

/// Step 4: the name and e-mail the account is created with. The fields write straight into
/// the draft, so stepping back and forward keeps what was typed.
struct ProfileStepView: View {
    @Bindable var model: SignUpModel
    let onFinish: (User) -> Void
    var feedback: AuthenticationFeedback?
    var onRecovery: () -> Void = {}

    /// `Toast` dismisses itself after two seconds; the hand-off waits that long so the
    /// warning is actually readable before KYC replaces this screen.
    private static let toastDwell: Duration = .seconds(2)

    private enum FieldKey: Hashable { case firstName, lastName, email }
    @FocusState private var focus: FieldKey?
    @State private var toast: Toast?

    var body: some View {
        ScrollView { form }
            .scrollIndicators(.hidden)
            .safeAreaInset(edge: .bottom, spacing: 0) {
                MPButton(title: Text("Continue", bundle: .module),
                         tone: .primary,
                         loading: model.isSubmitting || feedback?.isLoading == true,
                         enabled: model.draft.isProfileValid,
                         action: submit)
                    .gutter()
                    .padding(.vertical, Metric.small)
                    .background(Brand.bg)
            }
            .toast($toast)
            .sensoryFeedback(.warning, trigger: model.submissionFailed) { _, failed in failed }
            .onAppear { focus = .firstName }
    }

    private var form: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Your details", bundle: .module)
                .font(.heading1).tight().foregroundStyle(Brand.ink)
                .accessibilityAddTraits(.isHeader)

            Text("The name must match your ID exactly — it is the one your cards will carry.",
                 bundle: .module)
                .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true)
                .padding(.top, Metric.lede)

            fields
                .padding(.top, Metric.block)
                .onSubmit(advanceFocus)
                .disabled(feedback?.isLoading == true)

            if let feedback {
                AuthenticationFeedbackView(feedback: feedback) {
                    onRecovery()
                    if feedback == .emailExists { focus = .email }
                }
            }
        }
        .gutter()
        .padding(.bottom, Metric.small)
    }

    private var fields: some View {
        VStack(spacing: Metric.stack) {
            Field(placeholder: Text("First name", bundle: .module),
                  text: $model.draft.firstName, icon: "person",
                  contentType: .givenName, submit: .next, focused: focus == .firstName)
                .focused($focus, equals: .firstName)

            Field(placeholder: Text("Last name", bundle: .module),
                  text: $model.draft.lastName, icon: "person.text.rectangle",
                  contentType: .familyName, submit: .next, focused: focus == .lastName)
                .focused($focus, equals: .lastName)

            Field(placeholder: Text("Email address", bundle: .module),
                  text: $model.draft.email, icon: "envelope", keyboard: .emailAddress,
                  contentType: .emailAddress, submit: .done, focused: focus == .email)
                .focused($focus, equals: .email)
        }
    }

    private func advanceFocus() {
        switch focus {
        case .firstName: focus = .lastName
        case .lastName: focus = .email
        default: submit()
        }
    }

    private func submit() {
        guard feedback == nil else { onRecovery(); return }
        guard model.draft.isProfileValid else { return }
        focus = nil
        // The package infers main-actor isolation by default, so this task stays on the main
        // actor and `model.submit()` needs no hop of its own.
        Task {
            let user = await model.submit()
            if model.submissionFailed {
                toast = Toast(text: Text("Server unreachable — continuing in demo mode.",
                                         bundle: .module),
                              icon: "wifi.slash")
                try? await Task.sleep(for: Self.toastDwell)
            }
            onFinish(user)
        }
    }
}

#Preview("Profile — fr") {
    ProfileStepView(model: SignUpModel(accounts: PreviewAccountClient()), onFinish: { _ in })
        .page()
        .environment(\.locale, Locale(identifier: "fr"))
}

#Preview("Profile — server down") {
    let model = SignUpModel(accounts: PreviewAccountClient(result: .failure(.unreachable)))
    model.draft.firstName = "Aristide"
    model.draft.lastName = "Mbassi"
    model.draft.email = "aristide@example.cm"
    return ProfileStepView(model: model, onFinish: { _ in })
        .page()
        .environment(\.locale, Locale(identifier: "fr"))
}
