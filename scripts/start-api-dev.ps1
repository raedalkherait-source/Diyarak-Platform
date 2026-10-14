[CmdletBinding()]
param(
    [switch]$DatabaseOnly,
    [ValidateRange(1, 600)]
    [int]$DatabaseWaitSeconds = 120
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$environmentFile = Join-Path $repositoryRoot "docker/.env"
if (-not (Test-Path -LiteralPath $environmentFile -PathType Leaf)) {
    throw "docker/.env is missing. Follow docs/runbooks/development.md before starting."
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker is not available. Install/start Docker Desktop before running this script."
}

Push-Location $repositoryRoot
try {
    & docker info --format '{{.ServerVersion}}' | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker engine is unavailable. Start Docker Desktop and try again."
    }
    & docker compose version --short | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose is unavailable. Check your Docker Desktop installation."
    }

    if (-not $DatabaseOnly) {
        if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
            throw "dotnet is unavailable. Install the SDK selected by global.json."
        }
        & dotnet --version
        if ($LASTEXITCODE -ne 0) {
            throw "The SDK selected by global.json is unavailable."
        }
    }

    Write-Host "Starting PostgreSQL..."
    & docker compose --env-file $environmentFile up -d postgres
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL startup failed. Review the Docker Compose error above."
    }

    $containerIds = @(& docker compose --env-file $environmentFile ps --all --quiet postgres)
    if ($LASTEXITCODE -ne 0 -or $containerIds.Count -ne 1) {
        throw "Could not resolve one PostgreSQL container for this Compose project."
    }
    $containerId = $containerIds[0].Trim()
    $deadline = [DateTime]::UtcNow.AddSeconds($DatabaseWaitSeconds)
    Write-Host "Waiting for PostgreSQL health check..."
    while ($true) {
        $stateOutput = & docker inspect --format '{{.State.Status}} {{if .State.Health}}{{.State.Health.Status}}{{else}}missing{{end}}' $containerId
        if ($LASTEXITCODE -ne 0) {
            throw "Could not inspect PostgreSQL container state."
        }
        $state = ([string]$stateOutput).Trim()
        if ($state -eq "running healthy") {
            break
        }
        if ($state -match '^(exited|dead|removing)\b' -or $state -match '\b(unhealthy|missing)$') {
            throw "PostgreSQL is not ready ($state). Check: docker compose --env-file docker/.env logs --tail 50 postgres"
        }
        if ([DateTime]::UtcNow -ge $deadline) {
            throw "PostgreSQL did not become healthy within $DatabaseWaitSeconds seconds ($state)."
        }
        Start-Sleep -Seconds 2
    }

    Write-Host "PostgreSQL is healthy."
    if (-not $DatabaseOnly) {
        Write-Host "Starting Development API. Frontend: http://localhost:5227/app/"
        & dotnet run --project "src/Hosts/Diyarak.Api" --launch-profile http
        if ($LASTEXITCODE -ne 0) {
            throw "API exited with code $LASTEXITCODE. Review its output above."
        }
    }
}
finally {
    Pop-Location
}
