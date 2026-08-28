import SwiftUI

struct SignUpFlow: View {
    var onDone: () -> Void
    @State private var step = 0
    private let total = 5

    var body: some View {
        VStack(spacing: 0) {
            HStack(spacing: 16) {
                Button {
                    Haptic.tap()
                    withAnimation(.easeOut(duration: 0.22)) { step = max(0, step - 1) }
                } label: {
                    Image(systemName: "chevron.left")
                        .font(.system(size: 17, weight: .semibold))
                        .foregroundStyle(Brand.ink)
                        .frame(width: 36, height: 36)
                        .contentShape(.rect)
                }
                .opacity(step == 0 ? 0 : 1).disabled(step == 0)

                // Progression en segments : on voit combien d'étapes restent.
                HStack(spacing: 4) {
                    ForEach(0..<total, id: \.self) { i in
                        Capsule()
                            .fill(i <= step ? Brand.ink : Brand.rule.opacity(0.5))
                            .frame(height: 2)
                    }
                }
                .animation(.easeOut(duration: 0.22), value: step)
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
        withAnimation(.easeOut(duration: 0.22)) { step += 1 }
    }
}

// MARK: - 1. Téléphone

struct PhoneStep: View {
    var next: () -> Void
    @State private var digits = ""
    @State private var showCountries = false
    @State private var country = Country.cameroon

    struct Country: Identifiable, Equatable {
        let id: String, flag: String, name: String, dial: String, length: Int
        static let cameroon = Country(id: "CM", flag: "🇨🇲", name: "Cameroun", dial: "+237", length: 9)
        static let all: [Country] = [
            .cameroon,
            .init(id: "CI", flag: "🇨🇮", name: "Côte d'Ivoire", dial: "+225", length: 10),
            .init(id: "SN", flag: "🇸🇳", name: "Sénégal", dial: "+221", length: 9),
            .init(id: "GA", flag: "🇬🇦", name: "Gabon", dial: "+241", length: 8),
            .init(id: "CD", flag: "🇨🇩", name: "RD Congo", dial: "+243", length: 9),
            .init(id: "BJ", flag: "🇧🇯", name: "Bénin", dial: "+229", length: 8)
        ]
    }

    private var formatted: String {
        digits.enumerated().map { i, c in i > 0 && i % 2 == 1 && i < 9 ? " \(c)" : String(c) }.joined()
    }
    private var valid: Bool { digits.count == country.length }

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Quel est votre\nnuméro ?")
                .font(.system(size: 28, weight: .semibold)).tight(-0.7)
                .foregroundStyle(Brand.ink)
            Text("Un code à six chiffres part sur cette ligne. C'est aussi elle qui recevra vos rechargements Mobile Money.")
                .font(.body).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true)
                .padding(.top, 10)

            HStack(spacing: 0) {
                Button { Haptic.tap(); showCountries = true } label: {
                    HStack(spacing: 6) {
                        Text(country.flag).font(.system(size: 20))
                        Text(country.dial).font(.bodyMed).foregroundStyle(Brand.ink).monospacedDigit()
                        Image(systemName: "chevron.down").font(.system(size: 9, weight: .bold))
                            .foregroundStyle(Brand.inkFaint)
                    }
                    .padding(.trailing, 14)
                }
                .buttonStyle(Press())

                Rectangle().fill(Brand.rule).frame(width: 1, height: 24)

                Text(digits.isEmpty ? "6 XX XX XX XX" : formatted)
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

            MPButton(title: "Recevoir le code", enabled: valid, action: next).padding(.top, 8)

            Text("En continuant, vous acceptez les conditions générales et la politique de confidentialité de MoneyPay.")
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

struct CountrySheet: View {
    @Binding var selection: PhoneStep.Country
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Pays").font(.title2).tight().foregroundStyle(Brand.ink)
                .gutter().padding(.top, 20).padding(.bottom, 12)
            Rule()
            ScrollView {
                VStack(spacing: 0) {
                    ForEach(Array(PhoneStep.Country.all.enumerated()), id: \.element.id) { i, c in
                        Button { Haptic.tap(); selection = c; dismiss() } label: {
                            HStack(spacing: 13) {
                                Text(c.flag).font(.system(size: 24))
                                Text(c.name).font(.body).foregroundStyle(Brand.ink)
                                Spacer()
                                Text(c.dial).font(.body).foregroundStyle(Brand.inkMuted).monospacedDigit()
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

// MARK: - 2. Code

struct OTPStep: View {
    var next: () -> Void
    @State private var code = ""
    @State private var seconds = 42
    @State private var verifying = false

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Entrez le code")
                .font(.system(size: 28, weight: .semibold)).tight(-0.7).foregroundStyle(Brand.ink)
            Text("Envoyé au +237 6 99 12 34 56")
                .font(.body).foregroundStyle(Brand.inkMuted).padding(.top, 10)

            OTPBoxes(code: code).padding(.top, 34)

            Group {
                if seconds > 0 {
                    Text("Renvoyer le code dans \(seconds) s")
                        .font(.sub).foregroundStyle(Brand.inkMuted).monospacedDigit()
                } else {
                    Button { Haptic.tap(); seconds = 42 } label: {
                        Text("Renvoyer le code").font(.subMed).foregroundStyle(Brand.mark)
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

            MPButton(title: "Vérifier", loading: verifying, enabled: code.count == 6, action: verify)
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

// MARK: - 3. Code secret

struct PasscodeStep: View {
    var next: () -> Void
    @State private var first = ""
    @State private var confirm = ""
    @State private var error = false

    private var confirming: Bool { first.count == 4 }
    private var current: String { confirming ? confirm : first }

    var body: some View {
        VStack(spacing: 0) {
            VStack(spacing: 8) {
                Text(confirming ? "Confirmez votre code" : "Créez un code secret")
                    .font(.system(size: 24, weight: .semibold)).tight(-0.5)
                    .foregroundStyle(Brand.ink)
                Text(error ? "Les codes ne correspondent pas"
                     : "Il déverrouille l'application et valide vos paiements sensibles.")
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

// MARK: - 4. Face ID

struct BiometricStep: View {
    var next: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer()
            Image(systemName: "faceid")
                .font(.system(size: 44, weight: .light))
                .foregroundStyle(Brand.ink)

            Text("Activer Face ID ?")
                .font(.system(size: 26, weight: .semibold)).tight(-0.6)
                .foregroundStyle(Brand.ink).padding(.top, 26)
            Text("Ouvrez l'application et confirmez vos paiements sans saisir votre code.")
                .font(.body).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true).padding(.top, 8)

            HStack(spacing: 9) {
                Image(systemName: "lock.shield").font(.system(size: 13))
                Text("Vos données biométriques ne quittent pas votre iPhone.").font(.sub)
            }
            .foregroundStyle(Brand.inkMuted)
            .padding(.top, 22)

            Spacer()
            VStack(spacing: 9) {
                MPButton(title: "Activer Face ID", action: next)
                MPButton(title: "Plus tard", tone: .outline, action: next)
            }
        }
        .gutter()
        .padding(.bottom, 14)
    }
}

// MARK: - 5. Profil

struct ProfileStep: View {
    var next: () -> Void
    @State private var first = ""
    @State private var last = ""
    @State private var email = ""
    @FocusState private var focus: FieldKey?
    private enum FieldKey { case first, last, email }

    private var valid: Bool { !first.isEmpty && !last.isEmpty && email.contains("@") }

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Vos informations")
                .font(.system(size: 28, weight: .semibold)).tight(-0.7).foregroundStyle(Brand.ink)
            Text("Le nom doit correspondre exactement à votre pièce d'identité : c'est celui qui sera porté par vos cartes.")
                .font(.body).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true).padding(.top, 10)

            VStack(spacing: 10) {
                Field(placeholder: "Prénom", text: $first, icon: "person", focused: focus == .first)
                    .focused($focus, equals: .first)
                Field(placeholder: "Nom", text: $last, icon: "person.text.rectangle", focused: focus == .last)
                    .focused($focus, equals: .last)
                Field(placeholder: "Adresse e-mail", text: $email, icon: "envelope",
                      keyboard: .emailAddress, focused: focus == .email)
                    .focused($focus, equals: .email)
            }
            .padding(.top, 28)

            Spacer()
            MPButton(title: "Continuer", enabled: valid, action: next)
        }
        .gutter()
        .padding(.bottom, 10)
        .onAppear { focus = .first }
    }
}
