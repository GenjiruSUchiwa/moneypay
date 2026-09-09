import DesignSystem
import Foundation

struct Country: Identifiable, Hashable, Sendable {
    let id: String
    let flag: FlagMark.Country
    let dialCode: String
    let digitCount: Int

    static let cameroon = Country(id: "CM", flag: .cm, dialCode: "+237", digitCount: 9)

    static let supported: [Country] = [
        .cameroon,
        Country(id: "CI", flag: .ci, dialCode: "+225", digitCount: 10),
        Country(id: "SN", flag: .sn, dialCode: "+221", digitCount: 9),
        Country(id: "GA", flag: .ga, dialCode: "+241", digitCount: 8),
        Country(id: "CD", flag: .cd, dialCode: "+243", digitCount: 9),
        Country(id: "BJ", flag: .bj, dialCode: "+229", digitCount: 8)
    ]

    var localizedName: String { Locale.current.localizedString(forRegionCode: id) ?? id }

    func grouped(_ digits: String) -> String {
        digits.enumerated()
            .map { index, character in
                index > 0 && !index.isMultiple(of: 2) && index < 9
                    ? " \(character)" : String(character)
            }
            .joined()
    }

    var placeholder: String {
        let mask = id == "CM"
            ? "6" + String(repeating: "X", count: digitCount - 1)
            : String(repeating: "X", count: digitCount)
        return grouped(mask)
    }
}
