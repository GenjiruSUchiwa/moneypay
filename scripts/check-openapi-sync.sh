#!/usr/bin/env bash
# The generated Swift client consumes a committed copy of the contract. This fails when that
# copy has drifted from docs/api/openapi.json, which is the single source of truth.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
contract="$repo_root/docs/api/openapi.json"
client_copy="ios/Packages/ApiClient/Sources/ApiClient/openapi.json"

if [ ! -f "$contract" ]; then
    echo "docs/api/openapi.json is missing. Run ./scripts/update-openapi.sh." >&2
    exit 1
fi

if ! diff -q "$contract" "$repo_root/$client_copy" >/dev/null 2>&1; then
    cat >&2 <<MESSAGE
The OpenAPI copy at $client_copy is out of sync with docs/api/openapi.json.

Re-copy the committed contract:

  cp docs/api/openapi.json $client_copy

MESSAGE
    exit 1
fi

echo "OpenAPI contract copies are in sync."
