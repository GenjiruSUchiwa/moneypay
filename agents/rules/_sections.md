# Sections

This file defines all sections, their ordering, impact levels, and descriptions.
The section ID (in parentheses) is the filename prefix used to group rules.

---

## 1. Architecture (architecture)

**Impact:** CRITICAL
**Description:** Package layout (iOS SwiftPM, .NET modular monolith), boundaries, layering, design-system library.

## 2. Code Quality (quality)

**Impact:** CRITICAL
**Description:** Style, safety, zero warnings, complexity, English-only code, PRs and reviews.

## 3. Data Layer (data)

**Impact:** HIGH
**Description:** Repositories, DTO boundaries, money representation, persistence, EF Core, ledger.

## 4. API Design (api)

**Impact:** HIGH
**Description:** Backend contract, minimal endpoints, OpenAPI, localization, no magic strings.

## 5. Performance (performance)

**Impact:** HIGH
**Description:** SwiftUI body cost, algorithms, dates, concurrency.

## 6. Testing (testing)

**Impact:** MEDIUM-HIGH
**Description:** Swift Testing, xunit.v3 + Testcontainers, fakes, coverage, locale.

## 7. Design Patterns (patterns)

**Impact:** MEDIUM
**Description:** Dependency injection, observable state, async flows, navigation, outbox.

## 8. Team Culture (culture)

**Impact:** MEDIUM
**Description:** Accountability and how agents are expected to work.

## 9. CI/CD (ci)

**Impact:** HIGH
**Description:** Build-first, failure triage, git workflow, .NET pipeline.

## 10. Monorepo (monorepo)

**Impact:** HIGH
**Description:** Layout, versioning tags, changelog, conventional commits, per-component CI, OpenAPI contract, package management.

## 11. Reference (reference)

**Impact:** LOW
**Description:** File locations, local dev setup, Apple docs skill.
