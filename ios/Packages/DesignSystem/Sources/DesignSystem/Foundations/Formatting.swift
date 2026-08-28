import Foundation

public enum Fmt {
    public static func group(_ n: Int) -> String {
        let f = NumberFormatter()
        f.numberStyle = .decimal
        f.groupingSeparator = "\u{202F}"
        f.maximumFractionDigits = 0
        return f.string(from: NSNumber(value: abs(n))) ?? "\(abs(n))"
    }

    public static func xaf(_ amount: Int, symbol: Bool = true) -> String {
        symbol ? "\(group(amount)) FCFA" : group(amount)
    }

    public static func usd(_ cents: Int, symbol: Bool = true) -> String {
        let whole = group(abs(cents) / 100)
        let frac = String(format: "%02d", abs(cents) % 100)
        return symbol ? "$\(whole).\(frac)" : "\(whole).\(frac)"
    }

    /// Whole part and cents, so cents can be set as a superscript.
    public static func parts(usdCents: Int) -> (String, String) {
        (group(abs(usdCents) / 100), String(format: "%02d", abs(usdCents) % 100))
    }

    public static func relativeDay(_ date: Date) -> String {
        let cal = Calendar.current
        if cal.isDateInToday(date) { return "Aujourd'hui" }
        if cal.isDateInYesterday(date) { return "Hier" }
        let f = DateFormatter()
        f.locale = Locale(identifier: "fr_FR")
        f.dateFormat = cal.isDate(date, equalTo: .now, toGranularity: .year) ? "EEEE d MMMM" : "d MMMM yyyy"
        return f.string(from: date).capitalized(with: Locale(identifier: "fr_FR"))
    }

    public static func time(_ date: Date) -> String {
        let f = DateFormatter(); f.locale = Locale(identifier: "fr_FR"); f.dateFormat = "HH:mm"
        return f.string(from: date)
    }

    public static func fullDate(_ date: Date) -> String {
        let f = DateFormatter(); f.locale = Locale(identifier: "fr_FR")
        f.dateFormat = "d MMMM yyyy 'à' HH:mm"
        return f.string(from: date)
    }
}
