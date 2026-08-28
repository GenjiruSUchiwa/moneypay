import Charts
import Foundation

public struct MonthSpend: Identifiable {
    public init(label: String, xaf: Int, isCurrent: Bool) {
        self.label = label
        self.xaf = xaf
        self.isCurrent = isCurrent
    }

    public let id = UUID()
    public var label: String
    public var xaf: Int
    public var isCurrent: Bool
}
