#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
publish_dir="$repo_root/artifacts/win-x64"

dotnet workload restore "$repo_root/src/cloud-connector-windows-gui/cloud-connector-windows-gui.csproj"
dotnet test "$repo_root/tests/cloud-connector-windows-gui.Core.Tests/cloud-connector-windows-gui.Core.Tests.csproj"
dotnet publish "$repo_root/src/cloud-connector-windows-gui/cloud-connector-windows-gui.csproj" \
  -f net10.0-windows10.0.19041.0 \
  -c Release \
  -r win10-x64 \
  --self-contained true \
  -p:WindowsPackageType=None \
  -o "$publish_dir"

echo "Published cloud-connector-windows-gui to $publish_dir"
