import SwiftUI

// MARK: - Châssis

/// Filet. Pleine largeur pour séparer deux sections, en retrait pour séparer
/// deux lignes d'une même liste.
struct Rule: View {
    var inset: CGFloat = 0
    var strong = false
    var body: some View {
        Rectangle()
            .fill(strong ? Brand.rule : Brand.hairline)
            .frame(height: 1)
            .padding(.leading, inset)
    }
}

/// Titre de section. Le chevron est l'affordance : pas de bouton, pas de
/// libellé « Tout voir » en couleur.
struct SectionHead: View {
    var title: String
    var trailing: String? = nil
    var tappable = false
    var action: () -> Void = {}

    var body: some View {
        Button(action: { if tappable { Haptic.tap(); action() } }) {
            HStack(alignment: .firstTextBaseline, spacing: 6) {
                Text(title).font(.title2).tight().foregroundStyle(Brand.ink)
                if tappable {
                    Image(systemName: "chevron.right")
                        .font(.system(size: 13, weight: .semibold))
                        .foregroundStyle(Brand.inkFaint)
                        .baselineOffset(-1)
                }
                Spacer(minLength: 8)
                if let trailing {
                    Text(trailing).font(.sub).foregroundStyle(Brand.inkMuted)
                }
            }
            .contentShape(.rect)
        }
        .buttonStyle(.plain)
        .disabled(!tappable)
    }
}

/// Eyebrow : capitales en monospace avec chasse élargie — la seule forme de
/// capitale autorisée par les règles typo, et elle sonne « relevé ».
struct Eyebrow: View {
    var text: String
    var body: some View {
        Text(text.uppercased())
            .font(.eyebrow)
            .tracking(1.1)
            .foregroundStyle(Brand.inkFaint)
    }
}

// MARK: - Montants

/// Nombre avec centimes en exposant et chiffres tabulaires. C'est ce détail
/// qui distingue un montant composé d'un montant simplement affiché.
struct MoneyText: View {
    var whole: String
    var frac: String? = nil
    var prefix: String? = nil
    var suffix: String? = nil
    var size: CGFloat = 34
    var weight: Font.Weight = .semibold
    var color: Color = Brand.ink

    static func usd(_ cents: Int, size: CGFloat = 34, weight: Font.Weight = .semibold,
                    color: Color = Brand.ink, signed: Bool = false) -> MoneyText {
        let p = Fmt.parts(usdCents: cents)
        let sign = signed && cents > 0 ? "+" : (cents < 0 ? "−" : "")
        return MoneyText(whole: p.0, frac: p.1, prefix: "\(sign)$",
                         size: size, weight: weight, color: color)
    }

    /// `unit: false` dans les colonnes de listes : la devise y est implicite,
    /// la répéter à chaque ligne est du bruit.
    static func xaf(_ amount: Int, size: CGFloat = 34, weight: Font.Weight = .semibold,
                    color: Color = Brand.ink, signed: Bool = false,
                    unit: Bool = true) -> MoneyText {
        let sign = signed && amount > 0 ? "+" : (amount < 0 ? "−" : "")
        return MoneyText(whole: sign + Fmt.group(amount), suffix: unit ? "FCFA" : nil,
                         size: size, weight: weight, color: color)
    }

    var body: some View {
        HStack(alignment: .firstTextBaseline, spacing: 0) {
            if let prefix {
                Text(prefix).font(.system(size: size * 0.72, weight: weight))
            }
            Text(whole)
                .font(.system(size: size, weight: weight))
                .tracking(size > 24 ? -0.8 : 0)
            if let frac {
                Text(frac)
                    .font(.system(size: size * 0.52, weight: weight))
                    .baselineOffset(size * 0.34)
            }
            if let suffix {
                Text(suffix)
                    .font(.system(size: max(11, size * 0.42), weight: .medium))
                    .foregroundStyle(Brand.inkMuted)
                    .padding(.leading, max(3, size * 0.09))
            }
        }
        .monospacedDigit()
        .foregroundStyle(color)
        .contentTransition(.numericText())
    }
}

// MARK: - Boutons

enum Tone { case ink, quiet, outline, danger }

struct MPButton: View {
    var title: String
    var icon: String? = nil
    var tone: Tone = .ink
    var loading = false
    var enabled = true
    var action: () -> Void

    var body: some View {
        Button { Haptic.tap(); action() } label: {
            HStack(spacing: 8) {
                if loading { ProgressView().tint(fg).scaleEffect(0.8) }
                else if let icon { Image(systemName: icon).font(.system(size: 15, weight: .semibold)) }
                Text(title).font(.system(size: 16, weight: .medium))
            }
            .frame(maxWidth: .infinity)
            .frame(height: 52)
            .foregroundStyle(fg)
            .background(bg, in: .rect(cornerRadius: Metric.control))
            .overlay {
                if tone == .outline {
                    RoundedRectangle(cornerRadius: Metric.control).stroke(Brand.rule, lineWidth: 1)
                }
            }
            .opacity(enabled ? 1 : 0.35)
        }
        .buttonStyle(Press())
        .disabled(!enabled || loading)
    }

    private var bg: Color {
        switch tone {
        case .ink: Brand.inkFill
        case .quiet: Brand.well
        case .outline: .clear
        case .danger: Brand.debitSoft
        }
    }
    private var fg: Color {
        switch tone {
        case .ink: Brand.onInk
        case .quiet, .outline: Brand.ink
        case .danger: Brand.debit
        }
    }
}

struct Press: ButtonStyle {
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .opacity(configuration.isPressed ? 0.62 : 1)
            .animation(.easeOut(duration: 0.12), value: configuration.isPressed)
    }
}

/// Action rapide : pastille discrète, libellé dessous. Pas de disque coloré.
struct QuickAction: View {
    var icon: String
    var label: String
    var emphasis = false
    var action: () -> Void = {}

    var body: some View {
        Button { Haptic.tap(); action() } label: {
            VStack(spacing: 7) {
                Image(systemName: icon)
                    .font(.system(size: 17, weight: .medium))
                    .foregroundStyle(emphasis ? Brand.onInk : Brand.ink)
                    .frame(width: 46, height: 46)
                    .background(emphasis ? Brand.inkFill : Brand.well, in: .circle)
                Text(label)
                    .font(.micro)
                    .foregroundStyle(Brand.inkMuted)
                    .lineLimit(1).minimumScaleFactor(0.8)
            }
            .frame(maxWidth: .infinity)
        }
        .buttonStyle(Press())
    }
}

// MARK: - Lignes

/// Pastille d'icône. Carré à coins concentriques, teinte tenue à faible
/// saturation : l'icône identifie, elle ne décore pas.
struct IconTile: View {
    var symbol: String
    var tint: Color = Brand.ink
    var size: CGFloat = 38
    var filled = false

    var body: some View {
        ZStack {
            RoundedRectangle(cornerRadius: size * 0.29, style: .continuous)
                .fill(filled ? tint : Brand.well)
            Image(systemName: symbol)
                .font(.system(size: size * 0.42, weight: .medium))
                .foregroundStyle(filled ? Brand.onInk : tint)
        }
        .frame(width: size, height: size)
    }
}

/// Ligne de liste posée à même le fond. Aucune carte, aucun rayon.
struct Row<Trailing: View>: View {
    var icon: String? = nil
    var iconTint: Color = Brand.ink
    var glyph: String? = nil
    var title: String
    var subtitle: String? = nil
    var chevron = false
    var destructive = false
    @ViewBuilder var trailing: Trailing

    var body: some View {
        HStack(spacing: 13) {
            if let glyph {
                Text(glyph).font(.system(size: 20))
                    .frame(width: 38, height: 38)
                    .background(Brand.well, in: .rect(cornerRadius: 11, style: .continuous))
            } else if let icon {
                IconTile(symbol: icon, tint: destructive ? Brand.debit : iconTint)
            }
            VStack(alignment: .leading, spacing: 2) {
                Text(title).font(.body)
                    .foregroundStyle(destructive ? Brand.debit : Brand.ink)
                if let subtitle {
                    Text(subtitle).font(.sub).foregroundStyle(Brand.inkMuted).lineLimit(1)
                }
            }
            Spacer(minLength: 10)
            trailing
            if chevron {
                Image(systemName: "chevron.right")
                    .font(.system(size: 13, weight: .semibold))
                    .foregroundStyle(Brand.inkFaint)
            }
        }
        .padding(.vertical, Metric.rowVertical)
        .contentShape(.rect)
    }
}

extension Row where Trailing == EmptyView {
    init(icon: String? = nil, iconTint: Color = Brand.ink, glyph: String? = nil,
         title: String, subtitle: String? = nil, chevron: Bool = false,
         destructive: Bool = false) {
        self.init(icon: icon, iconTint: iconTint, glyph: glyph, title: title,
                  subtitle: subtitle, chevron: chevron, destructive: destructive) { EmptyView() }
    }
}

/// Valeur de fin de ligne, en encre atténuée.
struct RowValue: View {
    var text: String
    var tint: Color = Brand.inkMuted
    var body: some View { Text(text).font(.body).foregroundStyle(tint) }
}

/// Empile des lignes séparées par un filet en retrait. Pas de conteneur.
struct RuledStack<Data: RandomAccessCollection, Content: View>: View where Data.Element: Identifiable {
    var data: Data
    var inset: CGFloat = 51
    @ViewBuilder var row: (Data.Element) -> Content

    var body: some View {
        VStack(spacing: 0) {
            ForEach(Array(data.enumerated()), id: \.element.id) { i, item in
                row(item)
                if i < data.count - 1 { Rule(inset: inset) }
            }
        }
    }
}

// MARK: - Statuts

/// Pastille d'état. Toujours icône + libellé : jamais la couleur seule.
struct StatusPill: View {
    var text: String
    var symbol: String
    var tint: Color
    var soft: Color

    var body: some View {
        HStack(spacing: 4) {
            Image(systemName: symbol).font(.system(size: 9, weight: .bold))
            Text(text).font(.system(size: 11, weight: .medium))
        }
        .padding(.horizontal, 7).padding(.vertical, 3)
        .foregroundStyle(tint)
        .background(soft, in: .rect(cornerRadius: 5, style: .continuous))
    }
}

struct Chip: View {
    var text: String
    var selected = false
    var action: () -> Void = {}

    var body: some View {
        Button { Haptic.tap(); action() } label: {
            Text(text)
                .font(.system(size: 14, weight: .medium))
                .padding(.horizontal, 14).padding(.vertical, 8)
                .foregroundStyle(selected ? Brand.onInk : Brand.ink)
                .background(selected ? Brand.inkFill : Brand.well,
                            in: .rect(cornerRadius: 9, style: .continuous))
        }
        .buttonStyle(Press())
    }
}

/// Segments : soulignement, pas de pilule flottante.
struct Segments: View {
    var items: [String]
    @Binding var selection: Int
    @Namespace private var ns

    var body: some View {
        HStack(spacing: 22) {
            ForEach(items.indices, id: \.self) { i in
                Button {
                    Haptic.tap()
                    withAnimation(.spring(response: 0.3, dampingFraction: 0.85)) { selection = i }
                } label: {
                    VStack(spacing: 8) {
                        Text(items[i])
                            .font(.system(size: 15, weight: selection == i ? .medium : .regular))
                            .foregroundStyle(selection == i ? Brand.ink : Brand.inkMuted)
                        ZStack {
                            Capsule().fill(.clear).frame(height: 2)
                            if selection == i {
                                Capsule().fill(Brand.inkFill).frame(height: 2)
                                    .matchedGeometryEffect(id: "seg", in: ns)
                            }
                        }
                    }
                    .fixedSize()
                }
                .buttonStyle(.plain)
            }
            Spacer(minLength: 0)
        }
    }
}

// MARK: - Champs

struct Field: View {
    var placeholder: String
    @Binding var text: String
    var icon: String? = nil
    var keyboard: UIKeyboardType = .default
    var focused: Bool = false

    var body: some View {
        HStack(spacing: 11) {
            if let icon {
                Image(systemName: icon).font(.system(size: 15))
                    .foregroundStyle(Brand.inkFaint).frame(width: 18)
            }
            TextField(placeholder, text: $text)
                .font(.body).foregroundStyle(Brand.ink)
                .keyboardType(keyboard)
                .textInputAutocapitalization(keyboard == .emailAddress ? .never : .sentences)
                .autocorrectionDisabled()
        }
        .padding(.horizontal, 14).frame(height: 50)
        .background(Brand.well, in: .rect(cornerRadius: Metric.control, style: .continuous))
        .overlay {
            RoundedRectangle(cornerRadius: Metric.control)
                .stroke(focused ? Brand.ink : .clear, lineWidth: 1.2)
        }
    }
}

// MARK: - Toast

struct Toast: Equatable {
    var text: String
    var icon: String = "checkmark"
}

extension View {
    func toast(_ toast: Binding<Toast?>) -> some View {
        overlay(alignment: .bottom) {
            if let t = toast.wrappedValue {
                HStack(spacing: 8) {
                    Image(systemName: t.icon).font(.system(size: 12, weight: .bold))
                    Text(t.text).font(.system(size: 14, weight: .medium))
                }
                .foregroundStyle(Brand.onInk)
                .padding(.horizontal, 16).padding(.vertical, 11)
                .background(Brand.inkFill, in: .capsule)
                .padding(.bottom, 34)
                .transition(.move(edge: .bottom).combined(with: .opacity))
                .task {
                    try? await Task.sleep(for: .seconds(2))
                    withAnimation(.easeOut(duration: 0.2)) { toast.wrappedValue = nil }
                }
            }
        }
        .animation(.spring(response: 0.32, dampingFraction: 0.86), value: toast.wrappedValue)
    }

    func page() -> some View { background(Brand.bg.ignoresSafeArea()) }
    func gutter() -> some View { padding(.horizontal, Metric.gutter) }
}

// MARK: - État vide

struct EmptyNote: View {
    var title: String
    var message: String
    var actionTitle: String? = nil
    var action: () -> Void = {}

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(title).font(.bodyMed).foregroundStyle(Brand.ink)
            Text(message).font(.sub).foregroundStyle(Brand.inkMuted)
                .fixedSize(horizontal: false, vertical: true)
            if let actionTitle {
                Button(action: action) {
                    HStack(spacing: 4) {
                        Text(actionTitle).font(.subMed)
                        Image(systemName: "arrow.right").font(.system(size: 11, weight: .bold))
                    }
                    .foregroundStyle(Brand.mark)
                }
                .padding(.top, 4)
            }
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(.vertical, 18)
    }
}

/// Barre de navigation légère, sans chrome.
struct NavBar: View {
    var title: String = ""
    var onBack: (() -> Void)? = nil
    var onClose: (() -> Void)? = nil

    var body: some View {
        HStack {
            if let onBack {
                Button { Haptic.tap(); onBack() } label: {
                    Image(systemName: "chevron.left")
                        .font(.system(size: 17, weight: .semibold))
                        .foregroundStyle(Brand.ink)
                        .frame(width: 40, height: 40)
                        .contentShape(.rect)
                }
                .padding(.leading, -10)
            }
            Spacer()
            Text(title).font(.title3).foregroundStyle(Brand.ink)
            Spacer()
            if let onClose {
                Button { Haptic.tap(); onClose() } label: {
                    Image(systemName: "xmark")
                        .font(.system(size: 15, weight: .semibold))
                        .foregroundStyle(Brand.inkMuted)
                        .frame(width: 40, height: 40)
                        .contentShape(.rect)
                }
                .padding(.trailing, -10)
            } else if onBack != nil {
                Color.clear.frame(width: 40, height: 40)
            }
        }
        .gutter()
        .frame(height: 48)
    }
}
