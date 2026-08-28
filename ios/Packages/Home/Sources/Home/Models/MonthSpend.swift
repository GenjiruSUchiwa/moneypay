import Charts
import Foundation

/// One month of spending on the Analyse screen.
///
/// The month is a `Date`, not a typed-out month name: the label comes from a
/// `FormatStyle`, so it follows the reader's locale.
public struct MonthSpend: Identifiable {
    public init(month: Date, xaf: Int, isCurrent: Bool) {
        self.month = month
        self.xaf = xaf
        self.isCurrent = isCurrent
    }

    public let id = UUID()
    public var month: Date
    public var xaf: Int
    public var isCurrent: Bool

    public var label: String { month.formatted(.dateTime.month(.abbreviated)) }

    /// Demo series: the last `amounts.count` months up to today.
    public static func demoSeries(_ amounts: [Int], calendar: Calendar = .current) -> [MonthSpend] {
        amounts.enumerated().map { offset, xaf in
            let back = offset - (amounts.count - 1)
            let month = calendar.date(byAdding: .month, value: back, to: .now) ?? .now
            return MonthSpend(month: month, xaf: xaf, isCurrent: offset == amounts.count - 1)
        }
    }
}
