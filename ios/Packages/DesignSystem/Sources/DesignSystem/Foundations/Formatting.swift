import Foundation

/// Locale-aware money and date formatting via `FormatStyle`; never hand-built strings.
public enum Fmt {
    public static func group(_ n: Int, locale: Locale = .current) -> String {
        Decimal(abs(n)).formatted(.number.precision(.fractionLength(0)).locale(locale))
    }

    /// Formats whole francs (XAF has no minor unit).
    public static func xaf(_ amount: Int, symbol: Bool = true, locale: Locale = .current) -> String {
        guard symbol else { return group(amount, locale: locale) }
        return Decimal(abs(amount))
            .formatted(.currency(code: "XAF").precision(.fractionLength(0)).locale(locale))
    }

    /// Formats US dollars from cents.
    public static func usd(_ cents: Int, symbol: Bool = true, locale: Locale = .current) -> String {
        let major = Decimal(abs(cents)) / 100
        guard symbol else {
            return major.formatted(.number.precision(.fractionLength(2)).locale(locale))
        }
        return major.formatted(.currency(code: "USD").precision(.fractionLength(2)).locale(locale))
    }

    /// Whole part and cents, so cents can be set as a superscript.
    public static func parts(usdCents: Int, locale: Locale = .current) -> (String, String) {
        let cents = Decimal(abs(usdCents) % 100)
            .formatted(.number.precision(.integerLength(2)).grouping(.never).locale(locale))
        return (group(abs(usdCents) / 100, locale: locale), cents)
    }

    /// "Today", "Yesterday", or the formatted day, relative to `calendar`.
    public static func relativeDay(_ date: Date, calendar: Calendar = .current) -> String {
        if calendar.isDateInToday(date) {
            return String(localized: "Today", bundle: .module, comment: "Day header for today's transactions")
        }
        if calendar.isDateInYesterday(date) {
            return String(localized: "Yesterday", bundle: .module, comment: "Day header for yesterday's transactions")
        }
        let sameYear = calendar.isDate(date, equalTo: .now, toGranularity: .year)
        return sameYear
            ? date.formatted(.dateTime.weekday(.wide).day().month(.wide))
            : date.formatted(.dateTime.day().month(.wide).year())
    }

    public static func time(_ date: Date) -> String {
        date.formatted(.dateTime.hour().minute())
    }

    public static func fullDate(_ date: Date) -> String {
        date.formatted(.dateTime.day().month(.wide).year().hour().minute())
    }
}
