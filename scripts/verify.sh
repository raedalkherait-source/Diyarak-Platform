#!/usr/bin/env bash
set -euo pipefail
rm -rf artifacts
dotnet restore Diyarak.Platform.All.sln
dotnet format Diyarak.Platform.All.sln --verify-no-changes --no-restore
dotnet build Diyarak.Platform.All.sln -c Release --no-restore
dotnet test Diyarak.Platform.All.sln -c Release --no-build
