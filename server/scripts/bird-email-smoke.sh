#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: BIRD_API_KEY=... FROM=... TO=... [BASE_URL=https://eu1.platform.bird.com] $0" >&2
  echo "Manual smoke run only. Never commit keys or run in CI." >&2
}

if [ -z "${BIRD_API_KEY:-}" ] || [ -z "${FROM:-}" ] || [ -z "${TO:-}" ]; then
  usage
  exit 2
fi

BASE_URL="${BASE_URL:-https://eu1.platform.bird.com}"
SUBJECT="${SUBJECT:-MoniPay smoke run}"
BODY="${BODY:-Bonjour, ceci est un test manuel MoniPay. Ne pas répondre.}"

command -v curl >/dev/null || { echo "curl is required" >&2; exit 2; }
command -v python3 >/dev/null || { echo "python3 is required" >&2; exit 2; }

payload=$(BIRD_FROM="$FROM" BIRD_TO="$TO" BIRD_SUBJECT="$SUBJECT" BIRD_BODY="$BODY" python3 -c \
  'import json, os; print(json.dumps({"from": os.environ["BIRD_FROM"], "to": [os.environ["BIRD_TO"]], "subject": os.environ["BIRD_SUBJECT"], "text": os.environ["BIRD_BODY"], "category": "transactional"}))')

response_file=$(mktemp)
trap 'rm -f "$response_file"' EXIT

status=$(curl -sS -o "$response_file" -w "%{http_code}" -X POST "$BASE_URL/v1/email/messages" \
  -H "Authorization: Bearer $BIRD_API_KEY" \
  -H "Content-Type: application/json" \
  --data "$payload")

message_id=$(python3 -c \
  'import json, sys; print(json.load(open(sys.argv[1])).get("id", ""))' "$response_file" 2>/dev/null || true)

echo "HTTP status: $status"
echo "Message id: ${message_id:-<none>}"

if [ "$status" = "202" ] && [ -n "$message_id" ]; then
  echo "Smoke result: accepted"
  exit 0
fi

echo "Smoke result: not accepted"
exit 1
