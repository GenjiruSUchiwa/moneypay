import SwiftUI

// MARK: - Couleur adaptative

extension Color {
    static func adaptive(light: UInt32, dark: UInt32) -> Color {
        Color(uiColor: UIColor { $0.userInterfaceStyle == .dark ? UIColor(rgb: dark) : UIColor(rgb: light) })
    }
    init(rgb: UInt32) { self = Color(uiColor: UIColor(rgb: rgb)) }
}

extension UIColor {
    convenience init(rgb: UInt32) {
        self.init(red: Double((rgb >> 16) & 0xFF) / 255,
                  green: Double((rgb >> 8) & 0xFF) / 255,
                  blue: Double(rgb & 0xFF) / 255, alpha: 1)
    }
}

// MARK: - Palette « Registre »
//
// Monochrome d'abord. Le neutre n'est pas un gris pur : il est légèrement
// biaisé vert-bleu, choisi et non hérité. La couleur n'intervient qu'à trois
// endroits — l'habillage des cartes, la sémantique monétaire, et la teinte
// d'identité. Aucun dégradé dans le châssis de l'interface.

enum Brand {
    /// Fond de page. Papier froid en clair, encre profonde en sombre.
    static let bg       = Color.adaptive(light: 0xF4F6F6, dark: 0x0C0F10)
    /// Surface élevée — réservée aux éléments réellement détachés du fond.
    static let surface  = Color.adaptive(light: 0xFFFFFF, dark: 0x161B1C)
    /// Creux : contenu secondaire, champs de saisie, pavé numérique.
    static let well     = Color.adaptive(light: 0xEAEDED, dark: 0x121718)

    /// Filets. Toujours en opacité, jamais un gris opaque.
    static let hairline = Color.adaptive(light: 0x0E1413, dark: 0xF1F4F3).opacity(0.09)
    static let rule     = Color.adaptive(light: 0x0E1413, dark: 0xF1F4F3).opacity(0.16)

    /// Encre.
    static let ink      = Color.adaptive(light: 0x0E1413, dark: 0xF1F4F3)
    static let inkMuted = Color.adaptive(light: 0x5E6D69, dark: 0x8C9B97)
    static let inkFaint = Color.adaptive(light: 0x93A19D, dark: 0x5A6764)
    /// Encre inversée : texte sur un aplat encre.
    static let onInk    = Color.adaptive(light: 0xFFFFFF, dark: 0x0C0F10)
    /// Aplat encre : action principale. Le noir lit « sérieux » là où une
    /// couleur saturée lit « démo ».
    static let inkFill  = Color.adaptive(light: 0x101615, dark: 0xF1F4F3)

    /// Teinte d'identité — le monogramme, l'onglet actif, les liens. Jamais
    /// en grand aplat, jamais confondue avec le vert « crédit ».
    static let mark     = Color.adaptive(light: 0x0B4F6C, dark: 0x6FB6D6)
    static let markSoft = Color.adaptive(light: 0xE2EDF2, dark: 0x11262F)

    /// Sémantique monétaire. Réservée : jamais utilisée comme couleur de série.
    static let credit   = Color.adaptive(light: 0x10704A, dark: 0x4FBF8B)
    static let debit    = Color.adaptive(light: 0xA8331F, dark: 0xE58A78)
    static let pending  = Color.adaptive(light: 0x8A5A06, dark: 0xE0A950)
    static let creditSoft  = Color.adaptive(light: 0xE1F0E8, dark: 0x11241B)
    static let debitSoft   = Color.adaptive(light: 0xF7E5E1, dark: 0x2A1512)
    static let pendingSoft = Color.adaptive(light: 0xF7EDDA, dark: 0x261C0C)
}

// MARK: - Métriques

enum Metric {
    /// Marge horizontale unique de toute l'app.
    static let gutter: CGFloat = 20
    /// Respiration entre deux sections. Large : c'est elle qui remplace les cartes.
    static let section: CGFloat = 34
    /// Rayons concentriques : un rayon intérieur vaut l'extérieur moins le padding.
    static let card: CGFloat = 18
    static func inner(_ outer: CGFloat, padding: CGFloat) -> CGFloat { max(4, outer - padding) }
    static let control: CGFloat = 13
    static let rowVertical: CGFloat = 14
}

// MARK: - Typographie
//
// SF Pro exploité au-delà du réglage par défaut : axe de largeur sur les
// libellés, chiffres tabulaires partout où des nombres s'alignent, semibold
// comme poids maximal des titres (jamais bold), interlignage laissé au système.

extension Font {
    static func display(_ size: CGFloat) -> Font { .system(size: size, weight: .semibold) }

    static let title1   = Font.system(size: 28, weight: .semibold)
    static let title2   = Font.system(size: 20, weight: .semibold)
    static let title3   = Font.system(size: 17, weight: .semibold)
    static let body     = Font.system(size: 16, weight: .regular)
    static let bodyMed  = Font.system(size: 16, weight: .medium)
    static let sub      = Font.system(size: 14, weight: .regular)
    static let subMed   = Font.system(size: 14, weight: .medium)
    static let micro    = Font.system(size: 12, weight: .regular)
    static let microMed = Font.system(size: 12, weight: .medium)

    /// Eyebrow : capitales autorisées uniquement en monospace, avec chasse
    /// élargie. C'est la règle typographique, et ça sonne « relevé bancaire ».
    static let eyebrow  = Font.system(size: 11, weight: .medium, design: .monospaced)
    /// Données brutes : PAN, références, codes.
    static let dataMono = Font.system(size: 15, weight: .regular, design: .monospaced)
}

extension View {
    /// Titres > 20 pt : chasse resserrée.
    func tight(_ amount: CGFloat = -0.4) -> some View { tracking(amount) }
}

// MARK: - Formatage

enum Fmt {
    static func group(_ n: Int) -> String {
        let f = NumberFormatter()
        f.numberStyle = .decimal
        f.groupingSeparator = "\u{202F}"
        f.maximumFractionDigits = 0
        return f.string(from: NSNumber(value: abs(n))) ?? "\(abs(n))"
    }

    static func xaf(_ amount: Int, symbol: Bool = true) -> String {
        symbol ? "\(group(amount)) FCFA" : group(amount)
    }

    static func usd(_ cents: Int, symbol: Bool = true) -> String {
        let whole = group(abs(cents) / 100)
        let frac = String(format: "%02d", abs(cents) % 100)
        return symbol ? "$\(whole).\(frac)" : "\(whole).\(frac)"
    }

    /// Partie entière / centimes, pour poser les centimes en exposant.
    static func parts(usdCents: Int) -> (String, String) {
        (group(abs(usdCents) / 100), String(format: "%02d", abs(usdCents) % 100))
    }

    static func relativeDay(_ date: Date) -> String {
        let cal = Calendar.current
        if cal.isDateInToday(date) { return "Aujourd'hui" }
        if cal.isDateInYesterday(date) { return "Hier" }
        let f = DateFormatter()
        f.locale = Locale(identifier: "fr_FR")
        f.dateFormat = cal.isDate(date, equalTo: .now, toGranularity: .year) ? "EEEE d MMMM" : "d MMMM yyyy"
        return f.string(from: date).capitalized(with: Locale(identifier: "fr_FR"))
    }

    static func time(_ date: Date) -> String {
        let f = DateFormatter(); f.locale = Locale(identifier: "fr_FR"); f.dateFormat = "HH:mm"
        return f.string(from: date)
    }

    static func fullDate(_ date: Date) -> String {
        let f = DateFormatter(); f.locale = Locale(identifier: "fr_FR")
        f.dateFormat = "d MMMM yyyy 'à' HH:mm"
        return f.string(from: date)
    }
}

// MARK: - Retour haptique

enum Haptic {
    static func tap() { UIImpactFeedbackGenerator(style: .light).impactOccurred() }
    static func soft() { UIImpactFeedbackGenerator(style: .soft).impactOccurred() }
    static func success() { UINotificationFeedbackGenerator().notificationOccurred(.success) }
    static func warning() { UINotificationFeedbackGenerator().notificationOccurred(.warning) }
}
