import Foundation

/// Locale-aware formatting for every value the UI shows.
///
/// Money is a `Decimal` run through `FormatStyle`, never a hand-built string:
/// grouping, the currency symbol and its position, and the number of fraction
/// digits (XAF has none, USD has two) are all decided by the caller's locale.
/// Dates follow `Locale.current` too — nothing here pins French.
public enum Fmt {
    public static func group(_ n: Int, locale: Locale = .current) -> String {
        Decimal(abs(n)).formatted(.number.precision(.fractionLength(0)).locale(locale))
    }

    /// FCFA. XAF has no minor unit, so `amount` is already whole francs.
    public static func xaf(_ amount: Int, symbol: Bool = true, locale: Locale = .current) -> String {
        guard symbol else { return group(amount, locale: locale) }
        return Decimal(abs(amount))
            .formatted(.currency(code: "XAF").precision(.fractionLength(0)).locale(locale))
    }

    /// US dollars, from cents.
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

    /// "Today", "Yesterday", or the day itself. The calendar is a parameter so a
    /// test can pin Africa/Douala instead of inheriting the runner's time zone.
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
