---
title: XCUITest Sparingly, #Preview Constantly
impact: MEDIUM
impactDescription: UI tests are the slowest, flakiest layer — spend them on money flows only
tags: testing, xcuitest, previews, accessibility, ui
---

## XCUITest Sparingly, #Preview Constantly

**Impact: MEDIUM**

A SwiftUI screen has two things worth checking: *does it look right* and *does the flow complete*.
The first is answered by `#Preview` in seconds; the second by XCUITest in minutes. Use each for what
it is good at, and never write an XCUITest to assert a colour, a font, or a layout.

**XCUITest is reserved for the flows where a failure costs money or locks a user out:**
1. Sign-up → OTP → profile (`Packages/Onboarding` → `SignUpFlow.swift`)
2. Top-up MoMo end to end (`Packages/TopUp` → `TopUpFlow.swift`)
3. Card creation and freeze (`Packages/Cards` → `CreateCardFlow.swift`, `CardControlsView.swift`)
4. KYC submission (`Packages/KYC` → `KYCFlow.swift`)

Everything else — Settings sub-screens, Gallery, Insights — is covered by previews and by unit tests
on the package's `@Observable` model.

A UI test drives the assembled app, so it can only live at the app level: a `MoniPayUITests` target
declared in `ios/project.yml` over `ios/UITests/`. It never goes in a package's `Tests/`, and never
in `ios/Tests/` (app *unit* tests for the composition root) — `swift test --package-path …` must
stay simulator-free and fast.

**Rules:**
- **Every element a UI test touches carries an `accessibilityIdentifier`.** Never query by visible
  label: copy changes, and a localized string is not a test contract.
- Identifiers are stable, English, dotted: `topup.amount.field`, `topup.confirm.button`.
  Keep them in one `enum A11y` so production and test target share the literal.
- The app under test launches with a deterministic state via a launch argument
  (`-uiTestSeed <fixture>`), never against the POC backend.
- UI tests use XCTest (`XCUIApplication` has no Swift Testing equivalent yet); unit tests never do.
- `#Preview` is mandatory on every new `View`, with at least the empty, loaded, and error states.
  A preview lives in the package that owns the view and renders with `SampleData` from
  `WalletStore`, never with a live client.

**Incorrect (queries user-visible copy, waits with sleep, asserts styling):**

```swift
func testTopUp() {
    let app = XCUIApplication()
    app.launch()
    app.buttons["Top up"].tap()   // breaks the moment the copy or the display language changes
    sleep(3)                         // arbitrary wait = flakiness
    app.textFields.firstMatch.typeText("25")
    XCTAssertTrue(app.staticTexts["25 FCFA"].exists)
}
```

**Correct (identifiers, explicit waits, seeded state, one meaningful assertion):**

```swift
// ios/Packages/DesignSystem/Sources/DesignSystem/A11y.swift
// Public: shared by the feature packages and by the UI-test target.
public enum A11y {
    public enum TopUp {
        public static let amountField = "topup.amount.field"
        public static let confirmButton = "topup.confirm.button"
        public static let successBanner = "topup.success.banner"
    }
}

// In the view — copy goes through String(localized:) with an English key;
// French ships as a translation in Localizable.xcstrings.
TextField(String(localized: "Amount"), text: $amount)
    .accessibilityIdentifier(A11y.TopUp.amountField)
Button(String(localized: "Confirm")) { await model.confirm() }
    .accessibilityIdentifier(A11y.TopUp.confirmButton)
```

```swift
// ios/UITests/TopUpFlowUITests.swift  (target MoniPayUITests)
import DesignSystem
import XCTest

final class TopUpFlowUITests: XCTestCase {
    func testTopUpShowsUpdatedBalance() {
        let app = XCUIApplication()
        app.launchArguments = ["-uiTestSeed", "balance-zero"]   // deterministic state, no network
        app.launch()

        let field = app.textFields[A11y.TopUp.amountField]
        XCTAssertTrue(field.waitForExistence(timeout: 5))
        field.tap()
        field.typeText("25")
        app.buttons[A11y.TopUp.confirmButton].tap()

        // Explicit wait on the expected state, never sleep().
        XCTAssertTrue(app.otherElements[A11y.TopUp.successBanner].waitForExistence(timeout: 10))
    }
}
```

**Previews carry the visual burden.** Give each state a name so the canvas is a checklist, and pin
the locale the product actually ships in:

```swift
// ios/Packages/TopUp/Sources/TopUp/TopUpFlow.swift
#Preview("Top-up — empty") {
    TopUpFlow(model: .preview(state: .idle))
        .environment(\.locale, Locale(identifier: "fr_CM"))   // ship locale, not source language
}

#Preview("Top-up — insufficient balance") {
    TopUpFlow(model: .preview(state: .failed(.insufficientBalance(neededXAF: 15_700))))
        .environment(\.locale, Locale(identifier: "fr_CM"))
}

#Preview("Top-up — dark + Dynamic Type XXL") {
    TopUpFlow(model: .preview(state: .succeeded(balanceXAF: 428_500)))
        .preferredColorScheme(.dark)
        .environment(\.dynamicTypeSize, .accessibility3)
}
```

A preview that needs a network call or a live `Store` is a design smell: the view is not taking its
state as input. Fix the view, do not delete the preview. Because each feature is its own package,
`swift build --package-path ios/Packages/TopUp` already proves the previews compile — no simulator
needed.

Reference: [Previews in Xcode](https://developer.apple.com/documentation/swiftui/preview(_:body:)) ·
[User interface tests](https://developer.apple.com/documentation/xctest/user-interface-tests)
