import SwiftUI

struct KYCFlow: View {
    var onDone: () -> Void
    var onSkip: () -> Void
    @State private var step = 0

    var body: some View {
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

    private func advance() { Haptic.tap(); step += 1 }
    private func back() { Haptic.tap(); step = max(0, step - 1) }
}

// MARK: - Intro

struct KYCIntroView: View {
    var onStart: () -> Void
    var onLater: () -> Void

    private let steps: [(String, String)] = [
        ("Choisissez votre pièce", "CNI, passeport ou permis en cours de validité."),
        ("Photographiez-la", "Recto et verso, à plat, dans un endroit bien éclairé."),
        ("Prenez un selfie", "Pour confirmer que la pièce vous appartient.")
    ]

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            ScrollView {
                VStack(alignment: .leading, spacing: 0) {
                    Eyebrow(text: "≈ 3 minutes")

                    Text("Vérifions votre identité")
                        .font(.system(size: 28, weight: .semibold)).tight(-0.7)
                        .foregroundStyle(Brand.ink).padding(.top, 12)
                    Text("La réglementation CEMAC nous impose de vous identifier avant d'émettre une carte à votre nom.")
                        .font(.body).foregroundStyle(Brand.inkMuted)
                        .fixedSize(horizontal: false, vertical: true).padding(.top, 10)

                    VStack(spacing: 0) {
                        ForEach(steps.indices, id: \.self) { i in
                            HStack(alignment: .top, spacing: 14) {
                                Text("\(i + 1)")
                                    .font(.system(size: 12, weight: .medium, design: .monospaced))
                                    .foregroundStyle(Brand.inkMuted)
                                    .frame(width: 22, alignment: .leading)
                                    .padding(.top, 2)
                                VStack(alignment: .leading, spacing: 3) {
                                    Text(steps[i].0).font(.bodyMed).foregroundStyle(Brand.ink)
                                    Text(steps[i].1).font(.sub).foregroundStyle(Brand.inkMuted)
                                        .fixedSize(horizontal: false, vertical: true)
                                }
                                Spacer(minLength: 0)
                            }
                            .padding(.vertical, 15)
                            if i < steps.count - 1 { Rule(inset: 36) }
                        }
                    }
                    .padding(.top, 26)

                    Rule()

                    HStack(alignment: .top, spacing: 10) {
                        Image(systemName: "lock.shield").font(.system(size: 13))
                            .foregroundStyle(Brand.inkMuted).padding(.top, 2)
                        Text("Vos documents sont chiffrés et transmis uniquement à notre partenaire d'identification.")
                            .font(.sub).foregroundStyle(Brand.inkMuted)
                            .fixedSize(horizontal: false, vertical: true)
                    }
                    .padding(.top, 18)
                }
                .gutter()
            }

            VStack(spacing: 9) {
                MPButton(title: "Commencer la vérification", action: onStart)
                Button(action: onLater) {
                    Text("Plus tard").font(.bodyMed).foregroundStyle(Brand.inkMuted)
                        .frame(maxWidth: .infinity).frame(height: 44)
                }
            }
            .gutter()
        }
        .padding(.top, 24)
        .padding(.bottom, 10)
    }
}

// MARK: - Document

struct KYCDocumentPickerView: View {
    var onPick: (String) -> Void
    var onBack: () -> Void

    private let docs: [(String, String, String, Bool)] = [
        ("CNI camerounaise", "Recto et verso", "person.text.rectangle", true),
        ("Passeport", "Page avec photo", "book.pages", true),
        ("Permis de conduire", "Recto et verso", "car", false),
        ("Récépissé CNI", "Vérification manuelle sous 48 h", "doc.text", false)
    ]

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            NavBar(onBack: onBack)

            Text("Quelle pièce\nutilisez-vous ?")
                .font(.system(size: 28, weight: .semibold)).tight(-0.7)
                .foregroundStyle(Brand.ink).gutter()
            Text("Elle doit être en cours de validité et parfaitement lisible.")
                .font(.body).foregroundStyle(Brand.inkMuted).gutter().padding(.top, 10)

            VStack(spacing: 0) {
                ForEach(docs.indices, id: \.self) { i in
                    Button { onPick(docs[i].0) } label: {
                        HStack(spacing: 13) {
                            IconTile(symbol: docs[i].2)
                            VStack(alignment: .leading, spacing: 3) {
                                HStack(spacing: 6) {
                                    Text(docs[i].0).font(.body).foregroundStyle(Brand.ink)
                                    if docs[i].3 {
                                        StatusPill(text: "Instantané", symbol: "bolt.fill",
                                                   tint: Brand.credit, soft: Brand.creditSoft)
                                    }
                                }
                                Text(docs[i].1).font(.sub).foregroundStyle(Brand.inkMuted)
                            }
                            Spacer(minLength: 8)
                            Image(systemName: "chevron.right").font(.system(size: 13, weight: .semibold))
                                .foregroundStyle(Brand.inkFaint)
                        }
                        .padding(.vertical, Metric.rowVertical)
                        .contentShape(.rect)
                    }
                    .buttonStyle(.plain)
                    if i < docs.count - 1 { Rule(inset: 51) }
                }
            }
            .gutter()
            .padding(.top, 24)

            Spacer()
        }
    }
}

// MARK: - Capture

struct KYCCaptureView: View {
    enum Mode { case document, selfie }
    var mode: Mode
    var onNext: () -> Void
    var onBack: () -> Void
    @State private var captured = false

    var body: some View {
        VStack(spacing: 0) {
            NavBar(title: mode == .document ? "Pièce d'identité" : "Selfie", onBack: onBack)

            Text(mode == .document ? "Cadrez le recto de votre pièce" : "Placez votre visage dans le cercle")
                .font(.title3).foregroundStyle(Brand.ink)
                .multilineTextAlignment(.center).gutter()

            Spacer()

            ZStack {
                if mode == .document {
                    RoundedRectangle(cornerRadius: 14, style: .continuous)
                        .fill(Brand.well)
                        .aspectRatio(1.586, contentMode: .fit)
                        .overlay {
                            Image(systemName: "person.text.rectangle")
                                .font(.system(size: 54, weight: .ultraLight))
                                .foregroundStyle(Brand.inkFaint)
                        }
                    Corners()
                        .stroke(captured ? Brand.credit : Brand.ink,
                                style: .init(lineWidth: 2.5, lineCap: .round))
                        .aspectRatio(1.586, contentMode: .fit)
                } else {
                    Circle().fill(Brand.well).frame(height: 300)
                        .overlay {
                            Image(systemName: "person.crop.circle")
                                .font(.system(size: 64, weight: .ultraLight))
                                .foregroundStyle(Brand.inkFaint)
                        }
                    Circle()
                        .trim(from: 0, to: captured ? 1 : 0.72)
                        .stroke(captured ? Brand.credit : Brand.ink,
                                style: .init(lineWidth: 2.5, lineCap: .round))
                        .rotationEffect(.degrees(-90))
                        .frame(height: 300)
                }

                if captured {
                    Image(systemName: "checkmark")
                        .font(.system(size: 32, weight: .semibold))
                        .foregroundStyle(Brand.onInk)
                        .frame(width: 70, height: 70)
                        .background(Brand.credit, in: .circle)
                        .transition(.scale.combined(with: .opacity))
                }
            }
            .gutter()

            VStack(alignment: .leading, spacing: 8) {
                ForEach(tips, id: \.self) { tip in
                    HStack(spacing: 8) {
                        Image(systemName: "checkmark").font(.system(size: 10, weight: .bold))
                            .foregroundStyle(Brand.inkFaint)
                        Text(tip).font(.sub).foregroundStyle(Brand.inkMuted)
                        Spacer()
                    }
                }
            }
            .gutter()
            .padding(.top, 28)

            Spacer()

            if captured {
                MPButton(title: "Continuer", action: onNext).gutter()
            } else {
                Button {
                    Haptic.success()
                    withAnimation(.spring(response: 0.35, dampingFraction: 0.75)) { captured = true }
                } label: {
                    ZStack {
                        Circle().stroke(Brand.rule, lineWidth: 2).frame(width: 70, height: 70)
                        Circle().fill(Brand.inkFill).frame(width: 57, height: 57)
                    }
                }
                .buttonStyle(Press())
            }
        }
        .padding(.bottom, 20)
    }

    private var tips: [String] {
        mode == .document
        ? ["Sur fond uni, sans reflet", "Les quatre coins visibles", "Texte net et lisible"]
        : ["Retirez lunettes et couvre-chef", "Regardez droit vers l'objectif", "Lumière naturelle de préférence"]
    }
}

/// Quatre coins de viseur.
struct Corners: Shape {
    var len: CGFloat = 30
    var radius: CGFloat = 14

    func path(in r: CGRect) -> Path {
        var p = Path()
        p.move(to: .init(x: r.minX, y: r.minY + radius + len))
        p.addLine(to: .init(x: r.minX, y: r.minY + radius))
        p.addArc(center: .init(x: r.minX + radius, y: r.minY + radius), radius: radius,
                 startAngle: .degrees(180), endAngle: .degrees(270), clockwise: false)
        p.addLine(to: .init(x: r.minX + radius + len, y: r.minY))
        p.move(to: .init(x: r.maxX - radius - len, y: r.minY))
        p.addLine(to: .init(x: r.maxX - radius, y: r.minY))
        p.addArc(center: .init(x: r.maxX - radius, y: r.minY + radius), radius: radius,
                 startAngle: .degrees(270), endAngle: .degrees(0), clockwise: false)
        p.addLine(to: .init(x: r.maxX, y: r.minY + radius + len))
        p.move(to: .init(x: r.maxX, y: r.maxY - radius - len))
        p.addLine(to: .init(x: r.maxX, y: r.maxY - radius))
        p.addArc(center: .init(x: r.maxX - radius, y: r.maxY - radius), radius: radius,
                 startAngle: .degrees(0), endAngle: .degrees(90), clockwise: false)
        p.addLine(to: .init(x: r.maxX - radius - len, y: r.maxY))
        p.move(to: .init(x: r.minX + radius + len, y: r.maxY))
        p.addLine(to: .init(x: r.minX + radius, y: r.maxY))
        p.addArc(center: .init(x: r.minX + radius, y: r.maxY - radius), radius: radius,
                 startAngle: .degrees(90), endAngle: .degrees(180), clockwise: false)
        p.addLine(to: .init(x: r.minX, y: r.maxY - radius - len))
        return p
    }
}

// MARK: - Vérification

struct KYCReviewView: View {
    var onDone: () -> Void
    @State private var done = false
    @State private var spin = false

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Spacer()
            Group {
                if done {
                    Image(systemName: "checkmark")
                        .font(.system(size: 30, weight: .semibold))
                        .foregroundStyle(Brand.onInk)
                        .frame(width: 60, height: 60)
                        .background(Brand.credit, in: .circle)
                } else {
                    Circle()
                        .trim(from: 0, to: 0.28)
                        .stroke(Brand.ink, style: .init(lineWidth: 2.5, lineCap: .round))
                        .frame(width: 60, height: 60)
                        .rotationEffect(.degrees(spin ? 360 : 0))
                }
            }

            Text(done ? "Identité vérifiée" : "Vérification en cours")
                .font(.system(size: 26, weight: .semibold)).tight(-0.6)
                .foregroundStyle(Brand.ink).padding(.top, 26)
            Text(done ? "Votre compte est actif. Vous pouvez créer votre première carte virtuelle."
                 : "Notre partenaire compare votre selfie à votre pièce. Cela prend moins d'une minute.")
                .font(.body).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true).padding(.top, 8)

            if done {
                VStack(spacing: 0) {
                    Rule().padding(.top, 26)
                    HStack {
                        Text("Plafond mensuel").font(.body).foregroundStyle(Brand.inkMuted)
                        Spacer()
                        Text("3 000 000 FCFA").font(.body).foregroundStyle(Brand.ink).monospacedDigit()
                    }
                    .padding(.vertical, Metric.rowVertical)
                    Rule()
                    HStack {
                        Text("Cartes simultanées").font(.body).foregroundStyle(Brand.inkMuted)
                        Spacer()
                        Text("Jusqu'à 5").font(.body).foregroundStyle(Brand.ink)
                    }
                    .padding(.vertical, Metric.rowVertical)
                    Rule()
                }
                .transition(.opacity)
            }

            Spacer()
            if done { MPButton(title: "Accéder à mon compte", action: onDone) }
        }
        .gutter()
        .padding(.bottom, 18)
        .task {
            withAnimation(.linear(duration: 1).repeatForever(autoreverses: false)) { spin = true }
            try? await Task.sleep(for: .milliseconds(1700))
            Haptic.success()
            withAnimation(.easeOut(duration: 0.3)) { done = true }
        }
    }
}
