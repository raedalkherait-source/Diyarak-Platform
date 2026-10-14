[CmdletBinding()]
param(
    [ValidateRange(1, 65535)]
    [int]$Port = 5227,
    [ValidateRange(1, 120)]
    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$baseUrl = "http://localhost:$Port"

function Get-CheckedResponse {
    param(
        [string]$Path,
        [string]$Label,
        [string]$ContentTypePattern
    )

    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl$Path" -Method Get -UseBasicParsing -TimeoutSec $TimeoutSeconds -MaximumRedirection 0
    }
    catch {
        throw "$Label failed: $($_.Exception.Message)"
    }
    finally {
        $watch.Stop()
    }
    if ([int]$response.StatusCode -ne 200) {
        throw "$Label returned HTTP $($response.StatusCode), expected 200."
    }
    if ([string]$response.Headers["Content-Type"] -notmatch $ContentTypePattern) {
        throw "$Label returned an unexpected content type."
    }
    Write-Host ("{0}: HTTP {1}, {2:N2}s" -f $Label, $response.StatusCode, $watch.Elapsed.TotalSeconds)
    return $response
}

$html = Get-CheckedResponse -Path "/app/index.html" -Label "Frontend HTML" -ContentTypePattern '^text/html(?:;|$)'
if ($html.Content -notmatch 'id="listing-grid"') {
    throw "Frontend HTML does not contain the listing grid."
}
$null = Get-CheckedResponse -Path "/app/src/app.js" -Label "Frontend JavaScript" -ContentTypePattern '^(text|application)/javascript(?:;|$)'
$null = Get-CheckedResponse -Path "/app/src/styles/app.css" -Label "Frontend CSS" -ContentTypePattern '^text/css(?:;|$)'

$collection = Get-CheckedResponse -Path "/api/market/public/listings?page=1&pageSize=1" -Label "Public listing collection" -ContentTypePattern '^application/json(?:;|$)'
try {
    $page = $collection.Content | ConvertFrom-Json
}
catch {
    throw "Public listing collection returned invalid JSON."
}
if ($null -eq $page -or $page -is [System.Array]) {
    throw "Public listing collection must be a JSON object."
}
foreach ($field in @("items", "page", "pageSize", "hasMore")) {
    if ($page.PSObject.Properties.Name -notcontains $field) {
        throw "Public listing collection is missing '$field'."
    }
}
if ($page.items -isnot [System.Array] -or $page.items.Count -gt 1 -or
    ($page.page -isnot [int] -and $page.page -isnot [long]) -or
    ($page.pageSize -isnot [int] -and $page.pageSize -isnot [long]) -or
    $page.page -ne 1 -or $page.pageSize -ne 1 -or $page.hasMore -isnot [bool]) {
    throw "Public listing collection returned invalid pagination metadata."
}

if ($page.items.Count -eq 0) {
    Write-Host "Collection is valid and empty; detail check skipped."
}
else {
    $item = $page.items[0]
    if ($null -eq $item -or $item.PSObject.Properties.Name -notcontains "listingId") {
        throw "Public listing item is missing its identifier."
    }
    $listingId = [Guid]::Empty
    if (-not [Guid]::TryParse([string]$item.listingId, [ref]$listingId) -or $listingId -eq [Guid]::Empty) {
        throw "Public listing item has an invalid identifier."
    }
    $detailResponse = Get-CheckedResponse -Path "/api/market/public/listings/$listingId" -Label "Public listing detail" -ContentTypePattern '^application/json(?:;|$)'
    try {
        $detail = $detailResponse.Content | ConvertFrom-Json
    }
    catch {
        throw "Public listing detail returned invalid JSON."
    }
    if ($null -eq $detail -or $detail -is [System.Array] -or
        $detail.PSObject.Properties.Name -notcontains "listingId") {
        throw "Public listing detail is missing its identifier."
    }
    $detailId = [Guid]::Empty
    if (-not [Guid]::TryParse([string]$detail.listingId, [ref]$detailId) -or $detailId -ne $listingId) {
        throw "Public listing detail identifier does not match the collection item."
    }
}

Write-Host "Development API readiness checks passed."
