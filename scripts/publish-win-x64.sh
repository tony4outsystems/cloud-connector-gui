#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
connector_root="$(cd "$repo_root/../cloud-connector" && pwd)"
publish_dir="$repo_root/artifacts/win-x64"

dotnet test "$repo_root/tests/cloud-connector-windows-gui.Core.Tests/cloud-connector-windows-gui.Core.Tests.csproj"
dotnet publish "$repo_root/src/cloud-connector-windows-gui/cloud-connector-windows-gui.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o "$publish_dir"

(
  cd "$connector_root"
  GOOS=windows GOARCH=amd64 CGO_ENABLED=0 go build -o "$publish_dir/outsystemscc.exe" .
)

echo "Published cloud-connector-windows-gui to $publish_dir"
