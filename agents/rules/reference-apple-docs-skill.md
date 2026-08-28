---
title: Ground iOS 26 APIs in the swiftui-skills docs
impact: HIGH
impactDescription: prevents hallucinated signatures for APIs too new to be in training data
tags: swiftui, ios26, documentation, skills, liquid-glass
---

## Ground iOS 26 APIs in the swiftui-skills docs

**Impact: HIGH (prevents hallucinated signatures for APIs too new to be in training data)**

MoniPay targets iOS 26. Liquid Glass, the new toolbar placements, Swift 6.2 default isolation,
SwiftData class inheritance and Swift Charts 3-D all shipped with this release, which means a
model's memory of them is unreliable: parameter labels, defaults and even type names get invented
with total confidence. The compiler catches some of it, but a plausible-looking wrong modifier
costs a build cycle every time, and a *silently accepted* wrong one — a modifier that compiles
but renders nothing — costs much more.

The `swiftui-skills` skill packages Apple's own AdditionalDocumentation, extracted from Xcode 26,
at `~/.claude/skills/swiftui-skills/docs/`. Invoke the skill **before** writing or reviewing
SwiftUI code, read the document that covers the API, and cite it. If an API is not in those docs,
say so and propose a documented alternative rather than guessing.

If `docs/` contains no `.md` files, the extraction has not run yet: run
`~/.claude/skills/swiftui-skills/setup.sh` and do not give SwiftUI guidance until it is
populated.

**Incorrect (signature invented from memory — plausible, but not the API):**

```swift
// Guessed: a `style:` label, a `cornerRadius:` parameter, an `isInteractive:` flag.
// None of these exist; the code does not compile, and the shape of the guess suggests
// the author never opened the documentation.
Text("Available balance")
    .padding()
    .glassEffect(style: .regular, cornerRadius: 16, isInteractive: true)

// Also invented: there is no `GlassContainer`, and no `blendRadius:`.
GlassContainer(blendRadius: 40) {
    balanceCard
}
```

**Correct (from `SwiftUI-Implementing-Liquid-Glass-Design.md`):**

```swift
// The modifier is `glassEffect(_:in:isEnabled:)`. The first parameter is a `Glass` value
// (`.regular`, optionally chained with `.tint(_:)` and `.interactive(_:)`); the shape goes
// through `in:` and defaults to `.capsule`.
Text("Available balance")
    .font(.title)
    .padding()
    .glassEffect(.regular.tint(Color.action).interactive(), in: .rect(cornerRadius: 16.0))

// Multiple glass views belong in a GlassEffectContainer: it is what enables blending and
// morphing, and it is faster than N independent effects. The parameter is `spacing:`.
GlassEffectContainer(spacing: 40.0) {
    HStack(spacing: 20.0) {
        balanceCard.glassEffect()
        quickActions.glassEffect()
    }
}
```

Two habits that follow from the docs and are easy to get wrong from memory: apply
`.glassEffect()` **after** the modifiers that change the view's appearance, and give each effect
a `glassEffectID(_:in:)` when it must morph into another one.

Reference: [SwiftUI `View.glassEffect(_:in:isEnabled:)`](https://developer.apple.com/documentation/SwiftUI/View/glassEffect(_:in:isEnabled:)) ·
[`GlassEffectContainer`](https://developer.apple.com/documentation/SwiftUI/GlassEffectContainer) ·
local: `~/.claude/skills/swiftui-skills/docs/SwiftUI-Implementing-Liquid-Glass-Design.md`
