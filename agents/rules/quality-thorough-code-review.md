---
title: Address Every Nit Before Merging
impact: HIGH
impactDescription: Tolerated nits are how a codebase degrades one PR at a time
tags: quality, code-review, standards, accountability
---

## Address Every Nit Before Merging

**Impact: HIGH**

Do not approve a PR with unresolved nits to avoid being "the difficult one". That is precisely how a
codebase gets sloppy: each individual concession is small, and the sum is unreadable. Review is not a
politeness ritual; it is the last gate before code that moves other people's money reaches `main`.

**Incorrect approach:**

```
Reviewer: "`d` isn't a great name, but I get what it does."
Reviewer: "We normally guard-and-return here, but this works."
Reviewer: "This `Double` for the FX rate is probably fine for now."
Reviewer: "LGTM with minor comments 👍"
→ merged with four issues, two of which are money bugs
```

**Correct approach:**

```
Reviewer: "Please rename `d` → `debits`."
Reviewer: "Please invert this into `guard let card else { return }` per quality-no-force-unwrap."
Reviewer: "This must be `Decimal` — `Double` will drift the FCFA ledger. Blocking."
Reviewer: "Requesting changes: 3 items above, all small."
→ author fixes them in the same PR; merge is clean
```

**Separate blocking from non-blocking, and say which is which.** A reviewer who marks everything blocking
is as unhelpful as one who blocks nothing. Prefix accordingly:

- `blocking:` correctness, money precision, force unwraps, concurrency isolation, missing localization,
  security (logged PAN/CVV/keys), accessibility regressions.
- `nit:` naming, ordering, a clearer `switch`, a redundant modifier. Still expected to be fixed in this PR —
  "nit" describes the size, not the optionality.
- `question:` you do not understand something. The answer often becomes a doc comment.

**Reviewing Swift specifically, always look for:** force unwraps, `Double` money, missing `[weak self]`,
work on the main actor, hardcoded French strings (or a `String(localized:)` missing `bundle: .module`),
`@State` holding a reference type, `ForEach` without a stable identity, swallowed errors, and an `import`
that creates an illegal package edge. The checklist lives in `quality-review-checklist.md`.

**Challenge weak decisions, respectfully and concretely:**
- "Let's hardcode the rate for now" → "What would it take to read it from the feed in this PR?"
- "I'll add tests after" → "Can we add the `Wallet.authorize` cases now? I can pair on it."
- "Just copy the formatter into this view" → "This is the third copy — let's move it to `DesignSystem`."

**Receiving review is part of the job.** Push back with reasoning when you disagree, accept the change when
you do not, and never merge over unresolved blocking comments.

Reference: [Cal.diy Engineering Blog](https://cal.com/blog/engineering-in-2026-and-beyond)
