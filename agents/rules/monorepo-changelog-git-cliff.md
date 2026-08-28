---
title: Changelogs Are Generated, Never Written
impact: MEDIUM
impactDescription: A hand-edited changelog is silently overwritten by the next release run
tags: monorepo, changelog, git-cliff, releases, conventional-commits
---

## Changelogs Are Generated, Never Written

**Impact: MEDIUM**

`server/CHANGELOG.md` and `ios/CHANGELOG.md` are build outputs. git-cliff regenerates each one
**in full** from the commit history at every release, so an entry typed by hand survives exactly
until the next release run deletes it.

One `cliff.toml` per component, beside the changelog it produces. The two configs differ only in
the tags they follow and the paths they cover.

**Incorrect (editing the artefact instead of the input):**

```markdown
## Unreleased

### Features
- Collect a MoMo top-up through Campay   <!-- ❌ deleted by the next `git-cliff --tag` run -->
```

```bash
# ❌ The header is pinned: cliff.toml re-emits its own copy at the top of the file.
#    Moving or rewording it makes every future release produce a spurious diff.
sed -i '' '1s/.*/# Release notes/' server/CHANGELOG.md
```

**Correct (write the commit; let the release run write the file):**

```bash
git commit -m "feat(api): collect a MoMo top-up through Campay (#142)"
# The release workflow turns that subject into the changelog line, verbatim.
```

Which is why a commit subject and a PR title are user-facing prose, not notes to yourself:
`fix(api): stop double-holding funds on a replayed authorization` reads as a changelog line;
`fix: bug` does not.

**The per-component config.** `tag_pattern` picks the release points and `exclude_paths` keeps
the other component's commits out:

```toml
# server/cliff.toml
[git]
conventional_commits = true
filter_unconventional = false
# Only monipay.api tags are release points; skip_tags must not match them
# (git-cliff matches it as a substring), so exclude the iOS tags instead.
tag_pattern = "monipay.api-v.*"
skip_tags = "monipay.ios-.*"
# Commits that only touch the app tree stay out of the API changelog.
exclude_paths = ["ios/**", "prototype/**"]
commit_parsers = [
  { message = "^feat", group = "Features" },
  { message = "^fix", group = "Bug Fixes" },
  { message = "^docs", group = "Documentation" },
  { message = "^chore", group = "Chores" },
]
```

`skip_tags` is the subtle one. git-cliff matches it as a **substring**, so
`skip_tags = "monipay."` would skip the API tags too. Name the tags to exclude, never a prefix
that also matches your own.

**The header is pinned.** `[changelog].header` in `cliff.toml` must stay byte-identical to the
top of the committed `CHANGELOG.md`, because the release workflow overwrites the whole file:

```toml
[changelog]
header = """# Changelog

All notable changes to the MoniPay API are documented here. The format follows [Conventional Commits](https://www.conventionalcommits.org) and the project adheres to [Semantic Versioning](https://semver.org). API releases are tagged `monipay.api-v*`; the iOS app versions separately under `monipay.ios-v*`.
"""
# The blank lines in the body template are load-bearing; trim would collapse them.
trim = false
```

**The release notes strip that header.** A GitHub release shows one version's entries, not the
whole file:

```bash
git-cliff --config server/cliff.toml --tag "$TAG" --output server/CHANGELOG.md
git-cliff --config server/cliff.toml --latest --strip header --output "$RUNNER_TEMP/release-notes.md"
gh release create "$TAG" --title "$TAG" --notes-file "$RUNNER_TEMP/release-notes.md"
```

**Preview before you tag.** git-cliff renders unreleased commits without touching anything:

```bash
git-cliff --config server/cliff.toml --unreleased          # what the next API release would say
git-cliff --config ios/cliff.toml --unreleased             # and the app's
```

If a line reads badly there, fix the commit message while it is still rewritable — not the
generated file afterwards.

Reference: [git-cliff configuration](https://git-cliff.org/docs/configuration)
