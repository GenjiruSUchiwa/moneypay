# iOS authentication views

Scope: the visual portion of the open [GitHub milestones](https://github.com/GenjiruSUchiwa/moneypay/milestones).
The prototype remains the visual reference: `prototype/app.js`, `signupPhone`, `signupOTP`,
`signupPasscode`, `signupBiometric`, and `signupProfile`; `prototype/styles.css` owns its tokens.
No prototype, provider, backend, API contract, session storage, or pricing changes are included.

| Milestone | Visual coverage |
| --- | --- |
| iOS onboarding: splash | Existing splash and welcome carousel retained. |
| iOS onboarding: sign-up | Existing phone, country picker, code, passcode, Face ID, and profile views reused. The passcode field resets its identity between creation and confirmation. |
| API sign-up: foundations | No separate iOS screen is specified. |
| API sign-up: sessions and routes | No separate iOS screen is specified. Their client feedback is represented by the scenarios below. |
| API sign-up: providers, runtime and iOS cutover | UI portions of #107 and #108: loading, SMS delivery, verification/refusal feedback, profile failure, retry and email conflict. |
| API sign-in | UI portion of #111: phone → code → local passcode → Face ID, plus account creation/sign-in alternatives retaining the phone number. |

## Review

Open Gallery → Authentication states, or launch the simulator app with `-screen <key>`.
The original entries (`splash`, `welcome`, `phone`, `otp`, `passcode`, `faceid`, `profilestep`)
remain available.

| Keys | What to inspect |
| --- | --- |
| `signin-phone`, `signin-code`, `signin-passcode`, `signin-faceid` | Four-step sign-in layout and new-device copy. |
| `phone-sending`, `phone-offline`, `signup-expired` | Phone submission, retry and restart. |
| `code-queued`, `code-verifying`, `code-delivery-failed` | Pending delivery, verification loading and resend recovery. |
| `code-invalid`, `code-locked`, `code-expired` | Remaining attempts, disabled verification and expired-code recovery. |
| `code-resend-wait`, `code-resend-limit` | Resend refusals still allow entry of the code already received. |
| `phone-registered`, `phone-unregistered` | Switch flows while retaining the number and selected country. |
| `profile-submitting`, `profile-failed`, `profile-email-used` | Loading, retained profile fields, retry and email correction. |

These are deliberately **UI-only previews**. Codes use the existing local verification simulation;
pending/error scenarios stay visible until navigation or their recovery action. Sample retry times
and attempt counts are display data, not security enforcement. All preview account creation uses
`PreviewAccountClient`; no SMS is sent and no session, passcode, or biometric credential is stored.
Finishing the sign-in preview returns to Welcome and never emits an authenticated outcome.
The existing sign-up integration is outside this change.

The profile form scrolls when feedback and the keyboard need more space; its primary action stays
pinned above the keyboard. Email correction focuses the email field and retains the other details.

Visual feedback composes `EmptyNote`, `PhoneField`, `OTPBoxes`, `PasscodeDots`, `Field`,
`MPButton` and `SegmentedProgress`. French strings live in the Onboarding catalog with English keys;
remaining-attempt messages use catalog plurals. The system keyboard replaces the prototype keypad,
as required by the onboarding milestone.

Apple references: `swiftui-skills/docs/Swift-Concurrency-Updates.md` backs main-actor state;
`swiftui-skills/docs/SwiftUI-New-Toolbar-Features.md` backs the system toolbar approach.

## Verification commands

```sh
cd ios/Packages/Onboarding
xcodebuild -scheme Onboarding -destination 'platform=iOS Simulator,name=iPhone 17 Pro' \
  SWIFT_SUPPRESS_WARNINGS=NO test
cd ../Gallery
xcodebuild -scheme Gallery -destination 'platform=iOS Simulator,name=iPhone 17 Pro' \
  SWIFT_SUPPRESS_WARNINGS=NO build
cd ../..
swiftlint --strict
xcodegen generate
xcodebuild -scheme MoniPay -destination 'platform=iOS Simulator,name=iPhone 17 Pro' \
  SWIFT_SUPPRESS_WARNINGS=NO build
```

`SWIFT_SUPPRESS_WARNINGS=NO` removes Xcode's dependency warning suppression, which otherwise conflicts
with the packages' warnings-as-errors setting. Warnings remain enabled and treated as errors.

Local verification on 2026-09-05: Chrome prototype comparison; light and dark simulator checks on
iPhone 17 Pro / iOS 26.5; phone → code → passcode confirmation → Face ID interaction; email correction;
Onboarding, DesignSystem and Money tests passing; Gallery and app builds; strict lint.
Regression tests cover passcode field replay, back navigation from Face ID, and the welcome progress
clock. Simulator recordings confirmed regular 4.2-second carousel cycles after disabling timer
coalescing for auto-advance.
Mobile MCP was not exposed in this session, so simulator interaction used native computer control
and screenshots used `simctl`.

For reviewable PRs, separate the shared feedback views and catalog from the preview-flow/gallery wiring;
keep the passcode confirmation fix independently reviewable. Backend milestones remain open until
their actual contracts and authentication behavior are implemented.
