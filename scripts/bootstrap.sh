#!/usr/bin/env bash
set -euo pipefail
command -v dotnet >/dev/null || { echo "dotnet SDK is required" >&2; exit 1; }
dotnet --version
dotnet restore Diyarak.Platform.All.sln
dotnet build Diyarak.Platform.All.sln -c Debug --no-restore
dotnet test Diyarak.Platform.All.sln -c Debug --no-build
git config core.hooksPath .githooks || true
