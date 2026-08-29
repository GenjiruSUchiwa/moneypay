import DesignSystem
import SwiftUI

public struct PasscodeStep: View {
    public init(next: @escaping () -> Void) {
        self.next = next
    }

    public var next: () -> Void
    @State private var first = ""
    @State private var confirm = ""
    @State private var error = false
    @State private var matched = false

    private var confirming: Bool { first.count == 4 }
    private var current: String { confirming ? confirm : first }

    public var body: some View {
        VStack(spacing: 0) {
            VStack(spacing: 8) {
                Text(confirming ? "Confirm your passcode" : "Create a passcode", bundle: .module)
                    .font(.system(size: 24, weight: .semibold)).tight(-0.5)
                    .foregroundStyle(Brand.ink)
                Text(error ? "The passcodes do not match"
                     : "It unlocks the app and confirms your sensitive payments.", bundle: .module)
                    .font(.sub)
                    .foregroundStyle(error ? Brand.debit : Brand.inkMuted)
                    .multilineTextAlignment(.center)
                    .frame(maxWidth: 290)
            }
            .padding(.top, 30)

            PasscodeDots(code: entry, error: error).padding(.top, 36)

            Spacer()
        }
        .gutter()
        .frame(maxWidth: .infinity)
        // A typed digit clears the mismatch; the clearing that follows one keeps it.
        .onChange(of: first) { old, new in
            if new.count > old.count { error = false }
        }
        .onChange(of: confirm) { old, new in
            if new.count > old.count { error = false }
            if new.count == 4 { check() }
        }
        .sensoryFeedback(.success, trigger: matched)
        .sensoryFeedback(.warning, trigger: error) { _, new in new }
    }

    /// The step edits one of two codes in place, so the keyboard binds to whichever is current.
    private var entry: Binding<String> {
        Binding(
            get: { current },
            set: { value in
                if confirming { confirm = value } else { first = value }
            }
        )
    }

    private func check() {
        if confirm == first { matched = true; next() }
        else {
            withAnimation { error = true }
            confirm = ""; first = ""
        }
    }
}

public struct BiometricStep: View {
    public init(next: @escaping () -> Void) {
        self.next = next
    }

    public var next: () -> Void

    public var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer()
            Image(systemName: "faceid")
                .font(.system(size: 44, weight: .light))
                .foregroundStyle(Brand.ink)

            Text("Use Face ID?", bundle: .module)
                .font(.system(size: 26, weight: .semibold)).tight(-0.6)
                .foregroundStyle(Brand.ink).padding(.top, 26)
            Text("Open the app and confirm payments without typing your passcode.", bundle: .module)
                .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true).padding(.top, 8)

            HStack(spacing: 9) {
                Image(systemName: "lock.shield").font(.system(size: 13))
                Text("Your biometric data never leaves your iPhone.", bundle: .module).font(.sub)
            }
            .foregroundStyle(Brand.inkMuted)
            .padding(.top, 22)

            Spacer()
            VStack(spacing: 9) {
                MPButton(title: Text("Turn on Face ID", bundle: .module), action: next)
                MPButton(title: Text("Later", bundle: .module), tone: .outline, action: next)
            }
        }
        .gutter()
        .padding(.bottom, 14)
    }
}

public struct ProfileStep: View {
    public init(next: @escaping () -> Void) {
        self.next = next
    }

    public var next: () -> Void
    @State private var first = ""
    @State private var last = ""
    @State private var email = ""
    @FocusState private var focus: FieldKey?
    private enum FieldKey { case first, last, email }

    private var valid: Bool { !first.isEmpty && !last.isEmpty && email.contains("@") }

    public var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Your details", bundle: .module)
                .font(.system(size: 28, weight: .semibold)).tight(-0.7).foregroundStyle(Brand.ink)
            Text("The name must match your ID exactly — it is the one your cards will carry.", bundle: .module)
                .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true).padding(.top, 10)

            VStack(spacing: 10) {
                Field(placeholder: Text("First name", bundle: .module), text: $first,
                      icon: "person", focused: focus == .first)
                    .focused($focus, equals: .first)
                Field(placeholder: Text("Last name", bundle: .module), text: $last,
                      icon: "person.text.rectangle", focused: focus == .last)
                    .focused($focus, equals: .last)
                Field(placeholder: Text("Email address", bundle: .module), text: $email,
                      icon: "envelope", keyboard: .emailAddress, focused: focus == .email)
                    .focused($focus, equals: .email)
            }
            .padding(.top, 28)

            Spacer()
            MPButton(title: Text("Continue", bundle: .module), enabled: valid, action: next)
        }
        .gutter()
        .padding(.bottom, 10)
        .onAppear { focus = .first }
    }
}

#Preview("Profile step — fr, XXL") {
    ProfileStep(next: {})
        .environment(\.locale, Locale(identifier: "fr"))
        .environment(\.dynamicTypeSize, .accessibility3)
}
