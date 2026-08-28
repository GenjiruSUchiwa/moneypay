---
title: Date Handling — Calendar, FormatStyle, and Africa/Douala
impact: HIGH
impactDescription: Formatter allocation and naive date math dominate ledger rendering cost
tags: performance, dates, calendar, formatstyle, timezone
---

## Date Handling — Calendar, FormatStyle, and Africa/Douala

**Impact: HIGH**

Two things go wrong with dates in this app: correctness (arithmetic on `TimeInterval` ignores calendars and
time zones) and cost (`DateFormatter` allocation is expensive and is easy to do once per row).

### 1. Never do calendar arithmetic with seconds

`86_400` is not a day. It is wrong across DST elsewhere, and it is wrong here whenever "the last 30 days"
should mean thirty *calendar* days from the start of today, not `now - 2_592_000`.

**Incorrect:**

```swift
let since = Date().addingTimeInterval(-30 * 86_400)          // ❌ not 30 calendar days
let debits = transactions.filter { $0.date >= since }
```

**Correct:**

```swift
// ios/Packages/Platform/Sources/Platform/AppCalendar.swift
public enum AppCalendar {
    /// Shared calendar pinned to Africa/Douala (UTC+1, no DST).
    /// `Calendar.current` returns a value-type copy: rebuilding it per row is expensive.
    public static let douala: Calendar = {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = TimeZone(identifier: "Africa/Douala") ?? .gmt
        calendar.locale = Locale(identifier: "fr_CM")
        return calendar
    }()
}

guard let since = AppCalendar.douala.date(byAdding: .day, value: -30,
                                          to: AppCalendar.douala.startOfDay(for: .now))
else { return [] }
let debits = transactions.filter { $0.date >= since }
```

Use `DateComponents` for anything a human would call a day, month, or billing period — a card's monthly
spend limit resets on a calendar month boundary in Douala, not 30 × 86 400 seconds after issuance.

### 2. Never store a wall-clock string; store `Date`, display with `FormatStyle`

`Date` is an absolute instant. Convert to local time only at the display edge.

**Incorrect (allocates a formatter per row, ignores the user's locale):**

```swift
struct TransactionRow: View {
    let tx: Transaction
    var body: some View {
        let formatter = DateFormatter()               // ❌ allocated on every render
        formatter.dateFormat = "dd/MM/yyyy HH:mm"     // ❌ frozen format, not localized
        return Text(formatter.string(from: tx.date))
    }
}
```

**Correct (`FormatStyle` values are cheap, `Sendable`, and locale-aware):**

```swift
// ios/Packages/DesignSystem/Sources/DesignSystem/DateFormats.swift
extension Date.FormatStyle {
    /// Time shown in the transaction feed.
    public static let txTime = Date.FormatStyle(date: .omitted, time: .shortened)
        .locale(Locale(identifier: "fr_CM"))
    /// Daily section header.
    public static let daySection = Date.FormatStyle(date: .abbreviated, time: .omitted)
        .locale(Locale(identifier: "fr_CM"))
}

Text(tx.date, format: .txTime)
Text(section.day, format: .daySection)
Text(tx.date, format: .relative(presentation: .named))   // "2 hours ago"
```

If you genuinely need `DateFormatter` (a fixed provider format such as Campay's `yyyy-MM-dd'T'HH:mm:ssZ`,
decoded in `ApiClient`), create it **once** as a `static let` with `Locale(identifier: "en_US_POSIX")` and `TimeZone(secondsFromGMT: 0)`.
Never build one inside a loop or a `body`.

### 3. Group by day, once, using the shared calendar

```swift
// O(n): one startOfDay call per transaction, shared calendar.
let byDay = Dictionary(grouping: transactions) {
    AppCalendar.douala.startOfDay(for: $0.date)
}
```

### 4. Time zone policy

- Persist and transmit instants in UTC (`Date`, ISO 8601).
- Display in `Africa/Douala` — it is UTC+1 all year, no DST, which makes it a safe display default even when
  the device is elsewhere. Use `.autoupdatingCurrent` only where the *device* time is what matters.
- Tests pin the time zone explicitly; never assert against `Date()`.

Reference: [Apple — Formatting dates with FormatStyle](https://developer.apple.com/documentation/foundation/date/formatstyle)
