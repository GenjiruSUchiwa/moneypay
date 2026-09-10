#!/usr/bin/env bash
# Manual Bird SMS smoke check for issue #101. Kept out of CI on purpose.
# Usage:
#   BIRD_BASE_URL=https://eu1.platform.bird.com \
#   BIRD_API_KEY=<sandbox-key> BIRD_SENDER=<approved-sender> BIRD_TO=<sandbox-recipient> \
#   ./scripts/bird-sms-smoke.sh
# The script prints only the HTTP status and the Bird message id. It never prints
# credentials, the recipient, or the message body. Record the sanitized outcome in
# docs/adr/0004-sms-provider.md: date, sandbox result, and whether Bird reported
# acceptance (202) or confirmed handset delivery (a later delivery receipt).
set -euo pipefail

: "${BIRD_BASE_URL:?Set BIRD_BASE_URL to the regional host, e.g. https://eu1.platform.bird.com}"
: "${BIRD_API_KEY:?Set BIRD_API_KEY to a sandbox key}"
: "${BIRD_SENDER:?Set BIRD_SENDER to an approved sender for the destination}"
: "${BIRD_TO:?Set BIRD_TO to a provider-owned test number or approved sandbox recipient}"

response_file="$(mktemp)"
trap 'rm -f "$response_file"' EXIT

status="$(curl -sS --max-time 15 -o "$response_file" -w '%{http_code}' \
  -X POST "$BIRD_BASE_URL/v1/sms/messages" \
  -H "Authorization: Bearer $BIRD_API_KEY" \
  -H 'Content-Type: application/json' \
  -H "Idempotency-Key: smoke-$(date -u +%Y%m%dT%H%M%SZ)" \
  -d "$(jq -n --arg from "$BIRD_SENDER" --arg to "$BIRD_TO" \
    '{from: $from, to: $to, text: "MoniPay smoke check. Ignore this message.", category: "authentication"}')")"

message_id="$(jq -r '.id // "none"' "$response_file")"
echo "status=$status id=$message_id"
