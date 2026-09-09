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
MoniPay__Users__PersonalDataKeyBase64="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=" \
MoniPay__Sessions__VerificationCodeKeyBase64="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=" \
MoniPay__Sessions__PersonalDataKeyBase64="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=" \
MoniPay__Sessions__SigningKeyBase64="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=" \
MoniPay__Sessions__Issuer="https://openapi.monipay.example" \
MoniPay__Sessions__Audience="monipay-openapi" \
MoniPay__Sessions__Legal__TermsVersion="terms-2026-08" \
MoniPay__Sessions__Legal__PrivacyVersion="privacy-2026-08" \
MoniPay__Notifications__DataKeyBase64="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=" \
MoniPay__Notifications__Sms__BaseUrl="https://sms-openapi.monipay.example" \
MoniPay__Notifications__Sms__ApiKey="openapi-throwaway" \
MoniPay__Notifications__Sms__SenderId="MoniPay" \
dotnet build server/src/MoniPay.Api/MoniPay.Api.csproj -c Release -p:GenerateOpenApiDocs=true

cp server/src/MoniPay.Api/obj/openapi/openapi.json docs/api/openapi.json
cp docs/api/openapi.json "$client_copy"

echo "Wrote docs/api/openapi.json and $client_copy."
