---
title: No Inline Styling — Use DesignSystem Tokens and Components
impact: CRITICAL
impactDescription: Inline literals fork the visual language and make theming and dark mode impossible
tags: quality, designsystem, tokens, swiftui, consistency
---

## No Inline Styling — Use DesignSystem Tokens and Components

**Impact: CRITICAL**

`DesignSystem` is a real component library, not a folder of helpers. Every colour, type ramp, spacing step,
corner radius and animation curve is a **token**; every reusable visual element is a **component**. A
feature package that writes `Color(red:0.1, green:0.1, blue:0.2)`, `.font(.system(size: 17, weight: .semibold))`
or `.padding(16)` has silently forked the design language: the next theme change, dark-mode pass or Dynamic
Type fix has to hunt those literals down across every feature.

**The rule:** inside `ios/Packages/<Feature>/`, a view may compose components and apply *semantic* tokens.
It may not invent visual values, and it may not re-implement something `DesignSystem` already provides.

### Raw values live only in `DesignSystem/Sources/DesignSystem/Tokens/`

`Colors.swift`, `Typography.swift`, `Spacing.swift`, `Radii.swift`, `Motion.swift` are the *only* files
allowed to contain a hex literal, an `RGB` initialiser, a point size, or a magic number. Everything else
refers to them by semantic name — `.surface`, `.action`, `.inkQuiet`, `Spacing.md`, `Typography.title`,
`Radii.card`, `Motion.standard` — never by appearance (`.blue50`, `.grey3`).

**Incorrect (four literals and a hand-rolled card, inside a feature):**

```swift
// ios/Packages/Home/Sources/Home/Views/BalanceCard.swift
VStack(alignment: .leading, spacing: 8) {                 // ❌ magic spacing
    Text("Available balance", bundle: .module)
        .font(.system(size: 13, weight: .medium))         // ❌ invented type ramp
        .foregroundStyle(Color(white: 0.45))              // ❌ raw colour
    Text(formattedBalance)
        .font(.system(size: 34, weight: .bold, design: .rounded))
}
.padding(16)                                              // ❌ magic padding
.background(Color(red: 0.98, green: 0.98, blue: 1.0))     // ❌ light-only hex, breaks dark mode
.clipShape(RoundedRectangle(cornerRadius: 20))            // ❌ invented radius
.shadow(color: .black.opacity(0.06), radius: 12, y: 4)    // ❌ duplicates `.cardSurface()`
```

**Correct (tokens + an existing component):**

```swift
// ios/Packages/Home/Sources/Home/Views/BalanceCard.swift
import SwiftUI
import DesignSystem
import Money

VStack(alignment: .leading, spacing: Spacing.sm) {
    Eyebrow("Available balance", bundle: .module)
    MoneyText(balance, size: .display)
}
.padding(Spacing.md)
.cardSurface()                       // Foundations/Surfaces.swift: fill, radius, shadow
```

### Never re-implement a component

Before writing a pill, a row, a chip, a keypad key or a button, check
`DesignSystem/Sources/DesignSystem/Components/<Family>/` and the `Gallery` catalog. If it exists, use it
with its variant enum (`Tone`, `Size`) rather than styling a `Button` by hand.

```swift
// ❌ A reinvented status badge: no accessibility label, approximate colours.
Text(tx.status.label)
    .font(.caption).padding(.horizontal, 8).padding(.vertical, 4)
    .background(tx.status == .declined ? Color.red.opacity(0.12) : Color.green.opacity(0.12))
    .clipShape(Capsule())

// ✅ The existing component: consistent tones, a symbol (not colour alone), accessible.
StatusPill(status: tx.status)
```

### Rule of promotion

A visual pattern that appears in a **second** feature moves into `DesignSystem` *before* that second use
ships — not in a follow-up PR. Adding it means: one file under `Components/<Family>/`, a `public` type with
a labelled `public init`, variants as enums, an accessibility label and traits, a `#Preview` showing every
variant, a `///` doc comment saying when to use it, and a screen in the `Gallery` catalog.

Copy-pasting a modifier chain into a third feature is the failure this rule exists to prevent.

### Lint idea: flag raw colours outside `Tokens/`

Add to `ios/.swiftlint.yml` so the gate is mechanical rather than a reviewer's memory:

```yaml
custom_rules:
  no_raw_color_literals:
    name: "Raw colour outside Tokens"
    included: ".*/Packages/(?!DesignSystem/Sources/DesignSystem/Tokens).*\\.swift"
    regex: "Color\\((red|white|hue):|Color\\(hex:|UIColor\\(red:|#colorLiteral"
    message: "Use a semantic DesignSystem token (.surface, .action, .inkQuiet)."
    severity: error
  no_raw_font_size:
    name: "Raw font size outside Tokens"
    included: ".*/Packages/(?!DesignSystem/Sources/DesignSystem/Tokens).*\\.swift"
    regex: "\\.font\\(\\.system\\(size:"
    message: "Use Typography.* instead of a hardcoded point size."
    severity: error
```

`.padding(16)` and `cornerRadius: 20` are harder to catch with a regex without false positives — they stay a
review item (see `quality-review-checklist.md`). Assets in `Tokens/` remain exempt by construction.

Reference: [Apple — Managing model data and styles in SwiftUI](https://developer.apple.com/documentation/swiftui/view-styles)
