$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-VerificationStep {
    param(
        [string]$Name,
        [string]$Executable,
        [string[]]$Arguments
    )

    Write-Host "Verification: $Name"
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE. Verification stopped."
    }
}

foreach ($executable in @("dotnet", "node")) {
    if (-not (Get-Command $executable -ErrorAction SilentlyContinue)) {
        throw "Required command '$executable' is unavailable. Install it before verification."
    }
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    Invoke-VerificationStep -Name "Selected .NET SDK" -Executable "dotnet" -Arguments @("--version")
    Invoke-VerificationStep -Name "Node.js" -Executable "node" -Arguments @("--version")
    Remove-Item artifacts -Recurse -Force -ErrorAction SilentlyContinue
    Invoke-VerificationStep -Name "Frontend behavior tests" -Executable "node" -Arguments @("--test", "tests/Frontend/public-listing-pagination.test.cjs")
    Invoke-VerificationStep -Name "Restore" -Executable "dotnet" -Arguments @("restore", "Diyarak.Platform.All.sln")
    Invoke-VerificationStep -Name "Formatting" -Executable "dotnet" -Arguments @("format", "Diyarak.Platform.All.sln", "--verify-no-changes", "--no-restore")
    Invoke-VerificationStep -Name "Release build" -Executable "dotnet" -Arguments @("build", "Diyarak.Platform.All.sln", "-c", "Release", "--no-restore")
    Invoke-VerificationStep -Name "Release .NET tests" -Executable "dotnet" -Arguments @("test", "Diyarak.Platform.All.sln", "-c", "Release", "--no-build")
    Write-Host "All Windows verification steps passed."
}
finally {
    Pop-Location
}
