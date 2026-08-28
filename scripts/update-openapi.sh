#!/usr/bin/env bash
# Regenerates the committed API contract and the iOS client copy.
# The document is a build artefact of MoniPay.Api: never hand-edit either copy.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

# The iOS ApiClient package generates its Swift types from this copy.
client_copy="ios/Packages/ApiClient/Sources/ApiClient/openapi.json"

mkdir -p docs/api "$(dirname "$client_copy")"

# The generator boots the host to walk the endpoints, so it needs the configuration the host
# refuses to start without. These values are throwaway: they never reach a database.
HUSKY=0 \
ConnectionStrings__MoniPay="Host=localhost;Database=monipay;Username=monipay;Password=monipay" \
MoniPay__ApplyMigrationsOnStartup=false \
dotnet build server/src/MoniPay.Api/MoniPay.Api.csproj -c Release -p:GenerateOpenApiDocs=true

cp server/src/MoniPay.Api/obj/openapi/openapi.json docs/api/openapi.json
cp docs/api/openapi.json "$client_copy"

echo "Wrote docs/api/openapi.json and $client_copy."
