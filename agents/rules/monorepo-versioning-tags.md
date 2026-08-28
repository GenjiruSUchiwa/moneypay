---
title: Each Component Versions From Its Own Tag Prefix
impact: HIGH
impactDescription: A shared version number makes every app release rebuild the API and lie about what changed
tags: monorepo, versioning, tags, minver, releases, semver
---

## Each Component Versions From Its Own Tag Prefix

**Impact: HIGH**

The API and the iOS app ship on their own schedules, so they carry their own version numbers.
A single repository-wide version would force one to bump whenever the other released, and the
number would stop meaning anything.

| Component | Tag prefix | Version source | Changelog |
|---|---|---|---|
| API | `monipay.api-v*` | MinVer, via `MinVerTagPrefix` | `server/CHANGELOG.md` |
| iOS app | `monipay.ios-v*` | the `monipay-ios-version` workflow | `ios/CHANGELOG.md` |

The component name comes **first** in the tag because MinVer matches tags by prefix. `v1.2.0`
would match both components; `monipay.api-v1.2.0` matches exactly one.

**Incorrect (one prefix for the whole repository):**

```xml
<!-- server/Directory.Build.props -->
<PropertyGroup>
  <MinVerTagPrefix>v</MinVerTagPrefix>   <!-- ❌ an iOS tag now bumps the API build -->
</PropertyGroup>
```

```bash
git tag v1.4.0          # ❌ whose 1.4.0? The API rebuilds; the app changelog claims the change.
```

**Correct (the prefix names the component it versions):**

```xml
<!-- server/Directory.Build.props — inherited by every project under server/ -->
<PropertyGroup Label="Versioning">
  <!-- MinVer matches tags by prefix, which is why the component name comes first.
       An iOS tag never bumps this build. -->
  <MinVerTagPrefix>monipay.api-v</MinVerTagPrefix>
  <MinVerDefaultPreReleaseIdentifiers>preview.0</MinVerDefaultPreReleaseIdentifiers>
</PropertyGroup>
```

```bash
git tag monipay.api-v0.3.0     # ✅ bumps the API only
git tag monipay.ios-v1.8.2     # ✅ bumps the app only
```

**Never hand-write a version.** No `<Version>` in a `.csproj`, no `MARKETING_VERSION` typed into
`project.yml` for a release. The tag is the input; the build derives everything else:

```bash
# Between tags MinVer produces a height-suffixed prerelease, never a stale release number:
#   monipay.api-v0.3.0 + 4 commits  ->  0.3.1-preview.0.4
dotnet build server/MoniPay.slnx -c Release   # the assembly carries that version
```

**Tag from the release workflow, not from a laptop.** `monipay-api-release` and
`monipay-ios-version` compute the next version, refresh the changelog, commit, tag and push in
one ordered run. A hand-pushed tag skips the changelog and produces a release with no notes.

Both workflows bump only on a release-worthy commit — `feat`, `fix`, or a breaking marker —
because `git-cliff --bumped-version` patch-bumps on *any* commit type, which would ship a release
for a `docs:` change:

```bash
last="$(git describe --tags --abbrev=0 --match 'monipay.api-v*')"
if ! git log "$last"..HEAD --format='%s%n%b' -- . ':(exclude)ios' \
    | grep -qE '^(feat|fix)(\([^)]*\))?!?:|^[a-z]+(\([^)]*\))?!:|^BREAKING[ -]CHANGE:'; then
  echo "release=false" >> "$GITHUB_OUTPUT"   # nothing user-visible landed
  exit 0
fi
```

The path filters matter as much as the commit types: `':(exclude)ios'` on the API side, `-- ios/`
on the app side. Without them an app-only sprint would cut an API release whose changelog is
empty.

**A shallow clone breaks all of this.** MinVer and git-cliff both walk tag history, so every
workflow that builds or releases checks out with full history:

```yaml
- uses: actions/checkout@v4
  with:
    # MinVer derives the version from tag history, which a shallow clone does not have.
    fetch-depth: 0
```

Without it, MinVer sees no tag and stamps `0.0.0-preview.0.N` onto a production artefact.

Reference: [MinVer](https://github.com/adamralph/minver#tag-prefixes)
