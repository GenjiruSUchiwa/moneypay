#!/usr/bin/env bash
# Adds an EF Core migration and formats what the generator wrote.
# EF emits block-scoped namespaces, which the build rejects under EnforceCodeStyleInBuild;
# formatting the generated files is the fix that adds no suppression.
set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "usage: $0 <MigrationName>   e.g. $0 AddWalletHolds" >&2
    exit 1
fi

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

dotnet tool restore
dotnet tool run dotnet-ef migrations add "$1" \
    --project server/src/MoniPay.Data \
    --startup-project server/src/MoniPay.Api
dotnet format server/src/MoniPay.Data/MoniPay.Data.csproj

echo "Review the SQL before you commit:"
echo "  dotnet tool run dotnet-ef migrations script --idempotent --project server/src/MoniPay.Data --startup-project server/src/MoniPay.Api"
