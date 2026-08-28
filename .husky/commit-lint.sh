#!/usr/bin/env bash
# Rejects a commit message that does not follow Conventional Commits 1.0, or that does not
# name the issue it closes.
# Argument 1 is the file git holds the message in, as passed to the commit-msg hook.
#
# Squash merges on GitHub never reach this hook: there the PR title becomes the commit
# on main, which is why the same convention is checked on PR titles in CI.
set -euo pipefail

message_file="$1"
subject="$(head -n 1 "$message_file")"

# Messages git writes itself, and the fixup/squash markers consumed by an interactive
# rebase, are not the developer's to shape.
case "$subject" in
  Merge*|Revert*|fixup!*|squash!*) exit 0 ;;
esac

types='feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert'
# ios: the SwiftUI app.      api: the .NET server.   ds:     the DesignSystem package.
# docs: docs/ and *.md.      ci: build and workflows. agents: agents/ rules and guides.
# poc: the Node POC and the HTML prototype.
# This list must stay identical to the one in .github/workflows/pr-title.yml: the hook checks
# a local commit, the workflow checks the PR title a squash merge turns into that commit.
scopes='ios|api|ds|docs|ci|agents|poc'

if ! echo "$subject" | grep -qE "^($types)(\(($scopes)\))?(!)?: .{1,72}$"; then
  cat >&2 <<EOF
Commit message does not follow Conventional Commits.

  got:   $subject
  want:  <type>[(<scope>)][!]: <description of 1 to 72 characters> (#<issue>)

  type:  ${types//|/ }
  scope: ${scopes//|/ } (optional)

  example: feat(api): collect a MoMo top-up through Campay (#123)
EOF
  exit 1
fi

# The issue the commit closes, so history says why a change exists without a round trip
# through the PR. GitHub appends the pull request number to a squashed commit itself,
# which is why the PR title check does not ask for this one.
if ! echo "$subject" | grep -qE ' \(#[0-9]+\)$'; then
  cat >&2 <<EOF
Commit message does not name the issue it closes.

  got:   $subject
  want:  $subject (#<issue>)
EOF
  exit 1
fi

exit 0
