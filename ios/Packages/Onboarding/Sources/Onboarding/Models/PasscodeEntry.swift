/// Two-phase passcode entry: type four digits, then repeat them. Pure state — no haptics,
/// no navigation, no storage. The caller reacts to the returned `Event`.
struct PasscodeEntry: Hashable, Sendable {
    static let length = 4

    enum Phase: Hashable, Sendable { case creating, confirming }
    enum Event: Hashable, Sendable { case confirmed, mismatch }

    private(set) var first = ""
    private(set) var second = ""
    private(set) var isMismatch = false

    var phase: Phase { first.count == Self.length ? .confirming : .creating }
    /// The entry being typed — what the system keyboard's text field is bound to.
    var entry: String { phase == .confirming ? second : first }
    var dotsFilled: Int { entry.count }

    /// Replaces the entry being typed with what the text field now holds (digits only, at most
    /// `length`) and reports the outcome when the fourth confirmation digit lands. A mismatch
    /// clears **both** entries, as the prototype's `passKey` does. Any edit clears a previous
    /// mismatch first.
    mutating func setEntry(_ raw: String) -> Event? {
        isMismatch = false
        let digits = String(raw.filter(\.isNumber).prefix(Self.length))
        guard phase == .confirming else {
            first = digits
            return nil
        }
        second = digits
        guard second.count == Self.length else { return nil }
        // Keep `first` on a match: the caller copies it into the draft.
        guard second != first else { return .confirmed }
        isMismatch = true
        first = ""
        second = ""
        return .mismatch
    }
}
