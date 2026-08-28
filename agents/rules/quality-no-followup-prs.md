---
title: Minimize Follow-up PRs for Small Refactors
impact: HIGH
impactDescription: Deferred cleanups become permanent technical debt
tags: quality, pr, refactoring, technical-debt
---

## Minimize Follow-up PRs for Small Refactors

**Impact: HIGH**

"I'll clean this up in a follow-up" is, statistically, a decision never to clean it up. The follow-up loses
to the next feature, the context evaporates, and six months later the shortcut is load-bearing. If a small
refactor can be done in this PR, do it in this PR.

**Incorrect (shortcut plus a promise):**

```swift
// TODO(follow-up): move to Decimal, this Double is good enough for the demo
var usdToXAF: Double = 610.0

// TODO(follow-up): rename, `d` is unreadable
let d = store.transactions.filter { $0.amountXAF < 0 }
let r = d.reduce(0) { $0 + $1.amountXAF }
```

The `Double` rate ships. Six weeks later the ledger no longer reconciles and the fix touches thirty files.

**Correct (fixed now, in the same PR):**

```swift
var usdToXAF: Decimal = 610

let debits = store.transactions.filter { $0.settled.minorUnits < 0 }
let totalSpentXAF = debits.reduce(0) { $0 + $1.settled.minorUnits }
```

**Do it now when the change is:**
- A rename for clarity, in files you are already touching.
- Removing a force unwrap or a `try?` you just noticed.
- Deleting dead code your change orphaned.
- Extracting a duplicated `View` modifier chain or money-formatting helper into `DesignSystem`
  (or `Money`, if it is a value and not a view).
- Adding the missing `String(localized:)` around a French literal you walked past.
- Adding the test that would have caught the bug you are fixing.

**A separate PR is legitimate when:**
- The refactor is genuinely substantial (a `Money` model migration, splitting out a new package, a
  navigation rewrite) and would bury
  the change under review.
- The current PR is an urgent production fix and must ship now — then open the follow-up issue *in the same
  session*, linked from the PR, not "later".
- The change needs review from someone who is not reviewing this PR.

**If you truly must defer**, leave a machine-findable marker with an owner and a reason, and open the issue
before the PR merges:

```swift
// TODO(aristide, #142): replace the hardcoded rate with the BEAC feed.
// Blocked: the provider contract is not signed yet.
```

A `TODO` with no owner and no issue number is a comment nobody will ever action. SwiftLint flags them.

Reference: [Cal.diy Engineering Blog](https://cal.com/blog/engineering-in-2026-and-beyond)
