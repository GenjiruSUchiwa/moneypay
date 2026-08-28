import DesignSystem
import Money
import SwiftUI

public struct PhoneStep: View {
    public init(next: @escaping () -> Void) {
        self.next = next
    }

    public var next: () -> Void
    @State private var digits = ""
    @State private var showCountries = false
    @State private var country = Country.cameroon

    struct Country: Identifiable, Equatable {
        let id: String, flag: String, dial: String, length: Int

        /// Foundation already carries every region name in every language, so
        /// the country list is not ours to translate.
        var name: String { Locale.current.localizedString(forRegionCode: id) ?? id }

        static let cameroon = Country(id: "CM", flag: "🇨🇲", dial: "+237", length: 9)
        static let all: [Country] = [
            .cameroon,
            .init(id: "CI", flag: "🇨🇮", dial: "+225", length: 10),
            .init(id: "SN", flag: "🇸🇳", dial: "+221", length: 9),
            .init(id: "GA", flag: "🇬🇦", dial: "+241", length: 8),
            .init(id: "CD", flag: "🇨🇩", dial: "+243", length: 9),
            .init(id: "BJ", flag: "🇧🇯", dial: "+229", length: 8)
        ]
    }

    private var formatted: String {
        digits.enumerated().map { i, c in i > 0 && i % 2 == 1 && i < 9 ? " \(c)" : String(c) }.joined()
    }
    private var valid: Bool { digits.count == country.length }

    public var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("What is your\nnumber?", bundle: .module)
                .font(.system(size: 28, weight: .semibold)).tight(-0.7)
                .foregroundStyle(Brand.ink)
            Text("A six-digit code goes out to this line. It is also the line your Mobile Money top-ups arrive on.",
                 bundle: .module)
                .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true)
                .padding(.top, 10)

            HStack(spacing: 0) {
                Button { Haptic.tap(); showCountries = true } label: {
                    HStack(spacing: 6) {
                        Text(verbatim: country.flag).font(.system(size: 20))
                        Text(verbatim: country.dial).font(.bodyMed).foregroundStyle(Brand.ink).monospacedDigit()
                        Image(systemName: "chevron.down").font(.system(size: 9, weight: .bold))
                            .foregroundStyle(Brand.inkFaint)
                    }
                    .padding(.trailing, 14)
                }
                .buttonStyle(Press())

                Rectangle().fill(Brand.rule).frame(width: 1, height: 24)

                Text(verbatim: digits.isEmpty ? "6 XX XX XX XX" : formatted)
                    .font(.system(size: 20, weight: .medium))
                    .monospacedDigit()
                    .foregroundStyle(digits.isEmpty ? Brand.inkFaint : Brand.ink)
                    .padding(.leading, 14)
                Spacer(minLength: 0)
            }
            .padding(.top, 30)
            .padding(.bottom, 14)
            .overlay(alignment: .bottom) {
                Rectangle().fill(valid ? Brand.ink : Brand.rule).frame(height: valid ? 1.5 : 1)
            }

            Spacer()

            Keypad(onDigit: { d in if digits.count < country.length { digits.append("\(d)") } },
                   onDelete: { if !digits.isEmpty { digits.removeLast() } })

            MPButton(title: Text("Send me the code", bundle: .module), enabled: valid, action: next)
                .padding(.top, 8)

            Text("By continuing you accept MoneyPay's terms and privacy policy.", bundle: .module)
                .font(.micro).foregroundStyle(Brand.inkFaint)
                .fixedSize(horizontal: false, vertical: true)
                .padding(.top, 12)
        }
        .gutter()
        .padding(.bottom, 10)
        .sheet(isPresented: $showCountries) {
            CountrySheet(selection: $country)
                .presentationDetents([.medium]).presentationBackground(Brand.bg)
        }
    }
}

// Internal: its signature exposes `PhoneStep.Country`, an implementation
// detail of the phone step. Nothing else presents this sheet.
internal struct CountrySheet: View {
    internal init(selection: Binding<PhoneStep.Country>) {
        self._selection = selection
    }

    @Binding internal var selection: PhoneStep.Country
    @Environment(\.dismiss) private var dismiss

    internal var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Country", bundle: .module).font(.heading2).tight().foregroundStyle(Brand.ink)
                .gutter().padding(.top, 20).padding(.bottom, 12)
            Rule()
            ScrollView {
                VStack(spacing: 0) {
                    ForEach(Array(PhoneStep.Country.all.enumerated()), id: \.element.id) { i, c in
                        Button { Haptic.tap(); selection = c; dismiss() } label: {
                            HStack(spacing: 13) {
                                Text(verbatim: c.flag).font(.system(size: 24))
                                Text(verbatim: c.name).font(.bodyReg).foregroundStyle(Brand.ink)
                                Spacer()
                                Text(verbatim: c.dial).font(.bodyReg).foregroundStyle(Brand.inkMuted).monospacedDigit()
                                Image(systemName: "checkmark")
                                    .font(.system(size: 13, weight: .bold))
                                    .foregroundStyle(Brand.ink)
                                    .opacity(c == selection ? 1 : 0)
                            }
                            .padding(.vertical, Metric.rowVertical)
                            .contentShape(.rect)
                        }
                        .buttonStyle(.plain)
                        if i < PhoneStep.Country.all.count - 1 { Rule(inset: 37) }
                    }
                }
                .gutter()
            }
        }
    }
}

public struct OTPStep: View {
    public init(next: @escaping () -> Void) {
        self.next = next
    }

    public var next: () -> Void
    @State private var code = ""
    @State private var seconds = 42
    @State private var verifying = false

    public var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Enter the code", bundle: .module)
                .font(.system(size: 28, weight: .semibold)).tight(-0.7).foregroundStyle(Brand.ink)
            Text("Sent to \("+237 6 99 12 34 56")", bundle: .module)
                .font(.bodyReg).foregroundStyle(Brand.inkMuted).padding(.top, 10)

            OTPBoxes(code: code).padding(.top, 34)

            Group {
                if seconds > 0 {
                    Text("Resend the code in \(seconds) s", bundle: .module)
                        .font(.sub).foregroundStyle(Brand.inkMuted).monospacedDigit()
                } else {
                    Button { Haptic.tap(); seconds = 42 } label: {
                        Text("Resend the code", bundle: .module).font(.subMed).foregroundStyle(Brand.mark)
                    }
                }
            }
            .padding(.top, 20)

            Spacer()

            Keypad(onDigit: { d in
                       guard code.count < 6 else { return }
                       code.append("\(d)")
                       if code.count == 6 { verify() }
                   },
                   onDelete: { if !code.isEmpty { code.removeLast() } })

            MPButton(title: Text("Verify", bundle: .module), loading: verifying,
                     enabled: code.count == 6, action: verify)
                .padding(.top, 8)
        }
        .gutter()
        .padding(.bottom, 10)
        .task {
            while seconds > 0, !Task.isCancelled {
                try? await Task.sleep(for: .seconds(1)); seconds -= 1
            }
        }
    }

    private func verify() {
        guard !verifying else { return }
        verifying = true
        Task {
            try? await Task.sleep(for: .milliseconds(650))
            Haptic.success(); next()
        }
    }
}

public struct PasscodeStep: View {
    public init(next: @escaping () -> Void) {
        self.next = next
    }

    public var next: () -> Void
    @State private var first = ""
    @State private var confirm = ""
    @State private var error = false

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

            PasscodeDots(filled: current.count, error: error).padding(.top, 36)

            Spacer()

            Keypad(onDigit: { d in
                       error = false
                       if confirming {
                           guard confirm.count < 4 else { return }
                           confirm.append("\(d)")
                           if confirm.count == 4 { check() }
                       } else {
                           guard first.count < 4 else { return }
                           first.append("\(d)")
                       }
                   },
                   onDelete: {
                       if confirming, !confirm.isEmpty { confirm.removeLast() }
                       else if !confirming, !first.isEmpty { first.removeLast() }
                   })
            .padding(.bottom, 18)
        }
        .gutter()
        .frame(maxWidth: .infinity)
    }

    private func check() {
        if confirm == first { Haptic.success(); next() }
        else {
            Haptic.warning()
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
