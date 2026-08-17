#!/usr/bin/env bash
set -euo pipefail
rm -rf artifacts/test-results
dotnet test Diyarak.Platform.All.sln -c Release --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory artifacts/test-results
