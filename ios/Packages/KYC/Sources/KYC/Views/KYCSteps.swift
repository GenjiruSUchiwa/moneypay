import DesignSystem
import SwiftUI

public struct KYCIntroView: View {
    public init(onStart: @escaping () -> Void, onLater: @escaping () -> Void) {
        self.onStart = onStart
        self.onLater = onLater
    }

    public var onStart: () -> Void
    public var onLater: () -> Void

    private let steps: [(title: LocalizedStringKey, detail: LocalizedStringKey)] = [
        ("Pick your ID", "A national ID card, a passport or a licence that is still valid."),
        ("Photograph it", "Front and back, laid flat, somewhere well lit."),
        ("Take a selfie", "So we can confirm the document is yours.")
    ]

    public var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            ScrollView {
                VStack(alignment: .leading, spacing: 0) {
                    Eyebrow(text: Text("≈ 3 minutes", bundle: .module))

                    Text("Let's verify your identity", bundle: .module)
                        .font(.system(size: 28, weight: .semibold)).tight(-0.7)
                        .foregroundStyle(Brand.ink).padding(.top, 12)
                    Text("CEMAC rules require us to identify you before issuing a card in your name.",
                         bundle: .module)
                        .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                        .fixedSize(horizontal: false, vertical: true).padding(.top, 10)

                    VStack(spacing: 0) {
                        ForEach(steps.indices, id: \.self) { i in
                            HStack(alignment: .top, spacing: 14) {
                                Text(i + 1, format: .number)
                                    .font(.system(size: 12, weight: .medium, design: .monospaced))
                                    .foregroundStyle(Brand.inkMuted)
                                    .frame(width: 22, alignment: .leading)
                                    .padding(.top, 2)
                                VStack(alignment: .leading, spacing: 3) {
                                    Text(steps[i].title, bundle: .module).font(.bodyMed).foregroundStyle(Brand.ink)
                                    Text(steps[i].detail, bundle: .module).font(.sub).foregroundStyle(Brand.inkMuted)
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
                        Text("Your documents are encrypted and sent only to our identity partner.", bundle: .module)
                            .font(.sub).foregroundStyle(Brand.inkMuted)
                            .fixedSize(horizontal: false, vertical: true)
                    }
                    .padding(.top, 18)
                }
                .gutter()
            }

            VStack(spacing: 9) {
                MPButton(title: Text("Start the verification", bundle: .module), action: onStart)
                Button(action: onLater) {
                    Text("Later", bundle: .module).font(.bodyMed).foregroundStyle(Brand.inkMuted)
                        .frame(maxWidth: .infinity).frame(height: 44)
                }
            }
            .gutter()
        }
        .padding(.top, 24)
        .padding(.bottom, 10)
    }
}

public struct KYCDocumentPickerView: View {
    public init(onPick: @escaping (String) -> Void, onBack: @escaping () -> Void) {
        self.onPick = onPick
        self.onBack = onBack
    }

    public var onPick: (String) -> Void
    public var onBack: () -> Void

    /// `id` is what the picker hands back to the flow — a stable code, not the
    /// label the user reads.
    private let docs: [(id: String, title: LocalizedStringKey, detail: LocalizedStringKey,
                        symbol: String, instant: Bool)] = [
        ("national-id", "Cameroonian national ID", "Front and back", "person.text.rectangle", true),
        ("passport", "Passport", "The page with the photo", "book.pages", true),
        ("driving-licence", "Driving licence", "Front and back", "car", false),
        ("id-receipt", "National ID receipt", "Manual review within 48 h", "doc.text", false)
    ]

    public var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            NavBar(onBack: onBack)

            Text("Which document\nare you using?", bundle: .module)
                .font(.system(size: 28, weight: .semibold)).tight(-0.7)
                .foregroundStyle(Brand.ink).gutter()
            Text("It must still be valid and perfectly legible.", bundle: .module)
                .font(.bodyReg).foregroundStyle(Brand.inkMuted).gutter().padding(.top, 10)

            VStack(spacing: 0) {
                ForEach(docs.indices, id: \.self) { i in
                    Button { onPick(docs[i].id) } label: {
                        HStack(spacing: 13) {
                            IconTile(symbol: docs[i].symbol)
                            VStack(alignment: .leading, spacing: 3) {
                                HStack(spacing: 6) {
                                    Text(docs[i].title, bundle: .module).font(.bodyReg).foregroundStyle(Brand.ink)
                                    if docs[i].instant {
                                        StatusPill(text: Text("Instant", bundle: .module), symbol: "bolt.fill",
                                                   tint: Brand.credit, soft: Brand.creditSoft)
                                    }
                                }
                                Text(docs[i].detail, bundle: .module).font(.sub).foregroundStyle(Brand.inkMuted)
                            }
                            Spacer(minLength: 8)
                            Image(systemName: "chevron.forward").font(.system(size: 13, weight: .semibold))
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

public struct KYCCaptureView: View {
    public init(mode: Mode, onNext: @escaping () -> Void, onBack: @escaping () -> Void) {
        self.mode = mode
        self.onNext = onNext
        self.onBack = onBack
    }

    public enum Mode: Sendable { case document, selfie }
    public var mode: Mode
    public var onNext: () -> Void
    public var onBack: () -> Void
    @State private var captured = false

    public var body: some View {
        VStack(spacing: 0) {
            NavBar(title: Text(mode == .document ? "Identity document" : "Selfie", bundle: .module),
                   onBack: onBack)

            Text(mode == .document ? "Frame the front of your document"
                 : "Place your face inside the circle", bundle: .module)
                .font(.heading3).foregroundStyle(Brand.ink)
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
                ForEach(tips.indices, id: \.self) { i in
                    HStack(spacing: 8) {
                        Image(systemName: "checkmark").font(.system(size: 10, weight: .bold))
                            .foregroundStyle(Brand.inkFaint)
                        Text(tips[i], bundle: .module).font(.sub).foregroundStyle(Brand.inkMuted)
                        Spacer()
                    }
                }
            }
            .gutter()
            .padding(.top, 28)

            Spacer()

            if captured {
                MPButton(title: Text("Continue", bundle: .module), action: onNext).gutter()
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

    private var tips: [LocalizedStringKey] {
        mode == .document
        ? ["On a plain background, no glare", "All four corners visible", "Text sharp and legible"]
        : ["Take off glasses and headwear", "Look straight into the lens", "Natural light if you can"]
    }
}

/// Four viewfinder corners.
public struct Corners: Shape {
    public init(len: CGFloat = 30, radius: CGFloat = 14) {
        self.len = len
        self.radius = radius
    }

    public var len: CGFloat = 30
    public var radius: CGFloat = 14

    public func path(in r: CGRect) -> Path {
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

public struct KYCReviewView: View {
    public init(onDone: @escaping () -> Void) {
        self.onDone = onDone
    }

    public var onDone: () -> Void
    @State private var done = false
    @State private var spin = false

    public var body: some View {
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

            Text(done ? "Identity verified" : "Verification under way", bundle: .module)
                .font(.system(size: 26, weight: .semibold)).tight(-0.6)
                .foregroundStyle(Brand.ink).padding(.top, 26)
            Text(done ? "Your account is live. You can create your first virtual card."
                 : "Our partner is matching your selfie against your document. It takes under a minute.",
                 bundle: .module)
                .font(.bodyReg).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true).padding(.top, 8)

            if done {
                VStack(spacing: 0) {
                    Rule().padding(.top, 26)
                    HStack {
                        Text("Monthly cap", bundle: .module).font(.bodyReg).foregroundStyle(Brand.inkMuted)
                        Spacer()
                        Text(verbatim: Fmt.xaf(3_000_000)).font(.bodyReg)
                            .foregroundStyle(Brand.ink).monospacedDigit()
                    }
                    .padding(.vertical, Metric.rowVertical)
                    Rule()
                    HStack {
                        Text("Cards at once", bundle: .module).font(.bodyReg).foregroundStyle(Brand.inkMuted)
                        Spacer()
                        Text("Up to \(5)", bundle: .module).font(.bodyReg).foregroundStyle(Brand.ink)
                    }
                    .padding(.vertical, Metric.rowVertical)
                    Rule()
                }
                .transition(.opacity)
            }

            Spacer()
            if done { MPButton(title: Text("Go to my account", bundle: .module), action: onDone) }
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

#Preview("KYC intro — fr") {
    KYCIntroView(onStart: {}, onLater: {})
        .environment(\.locale, Locale(identifier: "fr"))
}
