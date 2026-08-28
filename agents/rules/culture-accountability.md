---
title: Hold Each Other Accountable for Quality
impact: MEDIUM
impactDescription: Collective ownership is what keeps a money app trustworthy
tags: culture, accountability, quality, teamwork
---

## Hold Each Other Accountable for Quality

**Impact: MEDIUM**

MoniPay holds people's money. A rounding shortcut, a swallowed error, or an untested authorisation path is
not a style disagreement — it is a user in Douala whose balance is wrong and who has no branch to walk into.
Cutting a corner feels faster in the moment and becomes everyone's problem within the month.

**This is not about being difficult.** It is collective ownership of the codebase and of the product's
reputation. When you see a teammate — human or agent — about to merge something that will hurt, say so.

**Make it normal to challenge weak decisions, respectfully and with an offer of help:**

```
"Let's hardcode the FX rate for now."
→ "What would it take to read it from the feed in this PR? I can help."

"A Double for the balance is fine for the demo."
→ "The ledger stops reconciling. Let's move to Decimal / minor units now — it's 20 lines."

"I'll add the tests in a follow-up PR."
→ "Let's at least cover the Wallet authorization cases here. The follow-up never lands."

"I'll copy this formatter into my view."
→ "That's the third copy. Shall we move it into the DesignSystem package?"
```

**Key behaviours:**
- Push back when you see a shortcut, and say *why* it costs — precision, security, accessibility.
- Offer to help when you ask for more work. "Requesting changes" plus "I can pair on this" is a different
  message from "Requesting changes".
- Accept challenges gracefully. If someone blocks your PR on a `Decimal`, they are protecting you too.
- Critique the code, never the person. "This force-unwraps" not "you always force-unwrap".
- Escalate on facts. "This crashes when `monthlyLimitUSDCents` is nil" beats "this feels fragile".

**Own the failure, not the blame.** When something breaks in production, the questions are: what was the
gap in the checks, and which rule or test closes it? Add that test in the fix PR. A postmortem that ends in
a name and not in a check has changed nothing.

**Accountability applies to agents too.** An agent that reports "done" without running `swiftlint` and
`swift test` / `xcodebuild` has made an unverified claim, and the reviewer should treat it exactly as they would a human
saying "it works on my machine". See `culture-leverage-ai.md`.

Reference: [Cal.diy Engineering Blog](https://cal.com/blog/engineering-in-2026-and-beyond)
