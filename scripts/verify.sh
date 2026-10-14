#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd -- "$repository_root"

for executable in dotnet node; do
    if ! command -v "$executable" >/dev/null 2>&1; then
        printf "Required command '%s' is unavailable. Install it before verification.\n" "$executable" >&2
        exit 1
    fi
done

run_step() {
    local label="$1"
    shift
    printf 'Verification: %s\n' "$label"
    "$@"
}

run_step '.NET SDK version' dotnet --version
run_step 'Node.js version' node --version
rm -rf artifacts
run_step 'Frontend behavior tests' node --test tests/Frontend/public-listing-pagination.test.cjs
run_step '.NET restore' dotnet restore Diyarak.Platform.All.sln
run_step 'Formatting verification' dotnet format Diyarak.Platform.All.sln --verify-no-changes --no-restore
run_step 'Release build' dotnet build Diyarak.Platform.All.sln -c Release --no-restore
run_step 'Release .NET tests' dotnet test Diyarak.Platform.All.sln -c Release --no-build
printf 'All Bash verification steps passed.\n'
