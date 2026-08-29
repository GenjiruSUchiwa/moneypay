import DesignSystem
import Foundation

/// One supported country for the phone step. Names come from the user's locale, never
/// from a literal, so nothing here needs translating (`quality-localization`).
struct Country: Identifiable, Hashable, Sendable {
    /// ISO 3166-1 alpha-2 region code — also the identity.
    let id: String
    let flag: FlagMark.Country
    /// Dial code with its `+`, as displayed: `"+237"`.
    let dialCode: String
    /// How many local digits a valid number has.
    let digitCount: Int

    static let cameroon = Country(id: "CM", flag: .cm, dialCode: "+237", digitCount: 9)

    /// `prototype/app.js` → `COUNTRIES` (line 765). Cameroon first: it is the default market.
    static let supported: [Country] = [
        .cameroon,
        Country(id: "CI", flag: .ci, dialCode: "+225", digitCount: 10),
        Country(id: "SN", flag: .sn, dialCode: "+221", digitCount: 9),
        Country(id: "GA", flag: .ga, dialCode: "+241", digitCount: 8),
        Country(id: "CD", flag: .cd, dialCode: "+243", digitCount: 9),
        Country(id: "BJ", flag: .bj, dialCode: "+229", digitCount: 8)
    ]

    var localizedName: String { Locale.current.localizedString(forRegionCode: id) ?? id }

    /// `prototype/app.js` → `fmtPhoneDigits` (line 774): a space before every odd index
    /// below 9, which reads `6 99 12 34 56` for a Cameroonian number.
    func grouped(_ digits: String) -> String {
        digits.enumerated()
            .map { index, character in
                index > 0 && !index.isMultiple(of: 2) && index < 9
                    ? " \(character)" : String(character)
            }
            .joined()
    }

    /// Cameroonian mobile numbers all start with 6 and the prototype's placeholder shows it.
    /// No other market has a single prefix, so their mask is all `X` — a locale-neutral
    /// character, not copy, so it stays out of the catalog.
    var placeholder: String {
        let mask = id == "CM"
            ? "6" + String(repeating: "X", count: digitCount - 1)
            : String(repeating: "X", count: digitCount)
        return grouped(mask)
    }
}
