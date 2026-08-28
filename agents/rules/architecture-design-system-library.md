---
title: DesignSystem Is the Component Library
impact: CRITICAL
impactDescription: One implementation per visual element; features stop drifting into private, inaccessible variants
tags: architecture, designsystem, components, tokens, accessibility, swiftui
---

## DesignSystem Is the Component Library

**Impact: CRITICAL**

Every reusable visual element in MoniPay lives in `ios/Packages/DesignSystem/`, one component per file.
Features **compose** those components; they never re-create them, never re-style them inline, and never reach
for a raw colour, font size, corner radius, or duration.

`DesignSystem` depends on SwiftUI and nothing of ours — no `Money`, no `WalletStore`
(`architecture-layering.md`). A component takes `String`, `Double`, and closures; the *feature* is what joins
a domain value to a component.

**Package structure:**

```
ios/Packages/DesignSystem/Sources/DesignSystem/
  Tokens/            Colors.swift  Typography.swift  Spacing.swift  Radii.swift  Motion.swift
  Foundations/       Theme.swift  Surfaces.swift (Liquid Glass)  Press.swift  ViewModifiers.swift
  Components/
    Buttons/         MPButton.swift  QuickAction.swift  IconTile.swift
    Typography/      MoneyText.swift  Eyebrow.swift  SectionHead.swift
    Lists/           Row.swift  RowValue.swift  RuledStack.swift  Rule.swift
    Feedback/        StatusPill.swift  Chip.swift  Toast.swift  EmptyNote.swift  SuccessMark.swift
    Inputs/          Field.swift  Segments.swift  Keypad.swift
    Navigation/      NavBar.swift
    Cards/           CardArt.swift
    DataViz/         Meter.swift  Viz.swift
  Resources/         fonts, Localizable.xcstrings
```

**Tokens are the only place a raw value appears.** Names are semantic — what the colour *means*, not what it
looks like — so a theme change is a token change:

```swift
// ios/Packages/DesignSystem/Sources/DesignSystem/Tokens/Colors.swift
public enum Brand {
    public static let surface  = Color(rgb: 0xFBFAF8)   // raw hex allowed ONLY in Tokens/
    public static let action   = Color(rgb: 0x141414)
    public static let ink      = Color(rgb: 0x141414)
    public static let inkQuiet = Color(rgb: 0x6B6B6B)
    public static let credit   = Color(rgb: 0x1B7F4C)
    public static let debit    = Color(rgb: 0xB8332E)
}
```

**Incorrect (a feature reinventing a button with raw values):**

```swift
// ios/Packages/TopUp/Sources/TopUp/Views/ConfirmStepView.swift
Button { confirm() } label: {
    Text("Confirm top-up", bundle: .module)
        .font(.system(size: 16, weight: .medium))                // ❌ raw type ramp
        .frame(maxWidth: .infinity, minHeight: 52)                // ❌ raw metric
        .foregroundStyle(.white)
        .background(Color(red: 0.08, green: 0.08, blue: 0.08))    // ❌ raw hex outside Tokens/
        .clipShape(RoundedRectangle(cornerRadius: 14))            // ❌ raw radius
}
// no haptic, no press state, no disabled state, no loading state, no accessibility traits
```

This button is 2 pt taller than every other primary button, ignores the theme, has no pressed feedback, and
must be found and fixed by hand the day the brand changes.

**Correct (compose the library component):**

```swift
// ios/Packages/TopUp/Sources/TopUp/Views/ConfirmStepView.swift
import DesignSystem

MPButton(title: String(localized: "Confirm top-up", bundle: .module),
         tone: .ink,
         loading: model.phase == .awaitingUSSD,
         enabled: model.isValid) {
    model.confirm()
}
.gutter()
```

**Component API conventions** — every file under `Components/` follows all six:

```swift
// ios/Packages/DesignSystem/Sources/DesignSystem/Components/Buttons/MPButton.swift
import SwiftUI

/// The screen's primary call to action: one per screen, pinned to the bottom safe area.
/// Use `.quiet` for a secondary action in the same stack and `.danger` for a destructive
/// confirmation. For an inline tappable list row use `Row`, not a button.
public struct MPButton: View {
    /// Variants are a closed enum, never a pile of Bools.
    public enum Tone: Sendable { case ink, quiet, outline, danger }

    public init(title: String,
                icon: String? = nil,
                tone: Tone = .ink,
                loading: Bool = false,
                enabled: Bool = true,
                action: @escaping () -> Void) { … }

    public var body: some View {
        Button { Haptic.tap(); action() } label: { … }
            .buttonStyle(Press())
            .disabled(!enabled || loading)
            .accessibilityLabel(Text(title))
            .accessibilityAddTraits(.isButton)
            .accessibilityValue(loading ? Text("Loading", bundle: .module) : Text(""))
    }
}

#Preview("MPButton — every variant") {
    VStack(spacing: 12) {
        MPButton(title: "Confirm", tone: .ink) {}
        MPButton(title: "Later", tone: .quiet) {}
        MPButton(title: "Change", tone: .outline) {}
        MPButton(title: "Delete card", tone: .danger) {}
        MPButton(title: "Create card", loading: true) {}
        MPButton(title: "Continue", enabled: false) {}
    }
    .padding()
}
```

1. `public` type with a `public init` taking **labeled** parameters and sensible defaults.
2. Variants are **enums** (`Tone`, `Size`), never boolean pairs like `isPrimary`/`isSmall`.
3. Accessibility is part of the component: label, traits, value, and a Dynamic Type–safe layout.
4. A `#Preview` showing **every variant and state**, disabled and loading included.
5. A `///` doc comment saying **when to use it** — and when to use something else.
6. No domain types, no `Money`, no business copy inside the component; strings arrive as parameters.

**Liquid Glass belongs in `Foundations/`, never in a feature.** iOS 26's `glassEffect` is a material, i.e. a
foundation, and it is easy to misuse: one `.glassEffect()` per view kills performance and prevents morphing.
Wrap it once, expose it as a modifier, and let features apply the wrapper:

```swift
// ios/Packages/DesignSystem/Sources/DesignSystem/Foundations/Surfaces.swift
import SwiftUI

public extension View {
    /// The app's floating surface: action bars, the balance header, the authorization sheet.
    /// Apply LAST — after padding, background, and anything else that changes appearance.
    func mpFloatingSurface(tinted: Bool = false, interactive: Bool = false) -> some View {
        glassEffect(
            tinted ? .regular.tint(Brand.action).interactive(interactive)
                   : .regular.interactive(interactive),
            in: .rect(cornerRadius: Radii.control)
        )
    }
}
```

```swift
// ios/Packages/DesignSystem/Sources/DesignSystem/Components/Navigation/ActionBar.swift
/// Groups the screen's floating actions. Uses one GlassEffectContainer so the effects
/// blend and morph together instead of each rendering its own glass pass.
public struct ActionBar<Content: View>: View {
    private let spacing: CGFloat
    private let content: Content

    public init(spacing: CGFloat = Spacing.large, @ViewBuilder content: () -> Content) {
        self.spacing = spacing
        self.content = content()
    }

    public var body: some View {
        GlassEffectContainer(spacing: spacing) {
            HStack(spacing: spacing) { content }
        }
    }
}
```

Feature-side rules for glass:

- **Never call `.glassEffect(...)` in a feature package.** Use `mpFloatingSurface()` or a component that
  already wraps it — otherwise radii, tints, and interactivity drift screen by screen.
- **Multiple glass views in one region go inside a single `GlassEffectContainer`** (`ActionBar` above), not
  one container per view.
- **System button styles are the default**: `.buttonStyle(.glass)` and `.buttonStyle(.glassProminent)` are
  wrapped by `MPButton` tones; do not apply them ad hoc next to an `MPButton`.
- **Morphing needs identity**: a glass view that appears or disappears carries `.glassEffectID(_:in:)` with a
  `@Namespace`, and the hierarchy change is wrapped in `withAnimation` — otherwise it pops instead of morphing.
- **Order matters**: the glass modifier goes last, after padding and background.

**Promotion rule: used twice → it belongs in DesignSystem.** A visual pattern that appears in a second
feature moves into `DesignSystem` **before the second use ships** — not "later", not "after the deadline". If
`Cards` and `Transactions` both draw a rounded status badge, `StatusPill` exists before the second PR merges.
Copying it is how a design system dies.

**`Gallery` is the catalog, not a feature.** `ios/Packages/Gallery/` depends on `DesignSystem` **only**, and
shows one screen per component family with every component and every variant. It is the visual-review surface
for design changes: add a component, add it to the catalog in the same PR.

Reference: swiftui-skills → `SwiftUI-Implementing-Liquid-Glass-Design.md` (`glassEffect`,
`GlassEffectContainer`, `glassEffectID`, `.buttonStyle(.glass)`) ·
[Applying Liquid Glass to custom views](https://developer.apple.com/documentation/SwiftUI/Applying-Liquid-Glass-to-custom-views) ·
[SwiftUI accessibility modifiers](https://developer.apple.com/documentation/swiftui/accessibility-modifiers)
