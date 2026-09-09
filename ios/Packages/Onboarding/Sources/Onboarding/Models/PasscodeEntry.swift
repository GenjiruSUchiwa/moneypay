struct PasscodeEntry: Hashable, Sendable {
    static let length = 4

    enum Phase: Hashable, Sendable { case creating, confirming }
    enum Event: Hashable, Sendable { case confirmed, mismatch }

    private(set) var first = ""
    private(set) var second = ""
    private(set) var isMismatch = false

    var phase: Phase { first.count == Self.length ? .confirming : .creating }
    var entry: String { phase == .confirming ? second : first }
    var dotsFilled: Int { entry.count }

    mutating func setEntry(_ raw: String) -> Event? {
        isMismatch = false
        let digits = String(raw.filter(\.isNumber).prefix(Self.length))
        guard phase == .confirming else {
            first = digits
            return nil
        }
        second = digits
        guard second.count == Self.length else { return nil }
        guard second != first else { return .confirmed }
        isMismatch = true
        first = ""
        second = ""
        return .mismatch
    }
}
