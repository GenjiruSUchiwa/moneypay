<!--
Title: <type>(<scope>): <description>   e.g. feat(api): collect a MoMo top-up through Campay
Types:  feat fix docs style refactor perf test build ci chore revert
Scopes: ios api ds docs ci agents poc
A squash merge makes this title the commit on main and the line in CHANGELOG.md,
so write it as a user-facing changelog entry. The pr-title workflow enforces it.
-->

## What

<!-- One or two sentences on what changed, in the words of CONTEXT.md. -->

## Why

<!-- The problem or the decision behind it. Link the issue: Closes #123
     If an ADR covers this, link it instead of re-arguing the decision here. -->

## How

<!-- The approach, and anything a reviewer would otherwise have to reverse-engineer:
     a trade-off taken, an alternative rejected, a migration that must run first. -->

## Verified

<!-- Tick what you ran. Delete the block that does not apply to this PR. -->

**iOS**

- [ ] `xcodebuild -scheme MoniPay -destination 'platform=iOS Simulator,name=iPhone 17 Pro' build` — no new warnings
- [ ] `xcodebuild -scheme MoniPay -destination 'platform=iOS Simulator,name=iPhone 17 Pro' test`
- [ ] `swift test --package-path ios/Packages/<Name>` for each package touched
- [ ] `swiftlint`
- [ ] `xcodegen generate` re-run (a package or an app-target file was added)

**Backend**

- [ ] `dotnet build server/MoniPay.slnx -c Release` — clean, warnings are errors
- [ ] `dotnet format server/MoniPay.slnx --verify-no-changes`
- [ ] `dotnet test server/MoniPay.slnx`

**Contract**

- [ ] `./scripts/update-openapi.sh` re-run and `docs/api/openapi.json` committed — **required if an endpoint or a contract changed**
- [ ] Not applicable: this PR changes no endpoint or contract

## Screenshots

<!-- iOS only. Before / after, light and dark. A flow change deserves a short screen recording.
     Delete this section for a backend-only PR. -->

## Size

- [ ] Under 500 lines changed and under 10 code files
- [ ] Larger, and here is why it cannot be split:

<!-- Docs and asset catalogs do not count; the .xcodeproj is git-ignored and never appears.
     If it can be split, split it — see the PR Size Guidelines in CLAUDE.md. -->

## Checklist

- [ ] No `Double`/`Float` (Swift) or `double`/`float` (C#) used for money
- [ ] No force unwrap, force try or force cast added (Swift); no `null!`, `.Result` or `.Wait()` (C#)
- [ ] No secret, connection string, PAN, CVV, phone number or OTP committed or logged
- [ ] French UI copy reviewed; no English leaking into the interface, no French in code
- [ ] No new sideways dependency between feature packages or between modules
- [ ] Opened as a draft
