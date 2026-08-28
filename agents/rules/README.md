# MoniPay Engineering Rules

Modular, machine-readable engineering rules for the MoniPay monorepo (SwiftUI iOS app + .NET backend), adapted from
[Cal.diy's engineering standards](https://cal.com/blog/engineering-in-2026-and-beyond) to Swift 6 / iOS 26 and .NET 10.

## Structure

Rules are grouped by filename prefix, defined in `_sections.md`:

| Prefix | Section | Impact |
|--------|---------|--------|
| `architecture-` | Architecture | CRITICAL |
| `quality-` | Code Quality | CRITICAL |
| `data-` | Data Layer | HIGH |
| `api-` | API Design | HIGH |
| `performance-` | Performance | HIGH |
| `ci-` | CI/CD | HIGH |
| `monorepo-` | Monorepo | HIGH |
| `testing-` | Testing | MEDIUM-HIGH |
| `patterns-` | Design Patterns | MEDIUM |
| `culture-` | Team Culture | MEDIUM |
| `reference-` | Reference | LOW |

Stack-specific rules say so in their name (`-dotnet-`, `-efcore-`, `-swiftui-`, `-swift-testing`) or tags; the rest apply to both stacks.
The full list is in [../README.md](../README.md).

## Files

- `_sections.md` - Sections, ordering, impact levels
- `_template.md` - Template for a new rule
- `{section}-{rule-name}.md` - One rule per file

## Rule format

YAML frontmatter (`title`, `impact`, optional `impactDescription`, `tags`), a short explanation of why the rule matters,
an **Incorrect** and a **Correct** code example, and a reference link. Code examples are Swift 6.3 / iOS 26 or C# / .NET 10,
always in English (see `quality-english-only-code.md`).

## Adding a rule

1. Copy `_template.md` to `{section}-{name}.md`
2. Fill in the frontmatter
3. Explain the rule, show incorrect and correct code, add a reference
4. Add the rule to the index in `../README.md`

## Core principle

We build money infrastructure that must not fail: warnings are errors, money is never a `Double`, secrets never reach git,
and every change ships complete, reviewed and tested.
