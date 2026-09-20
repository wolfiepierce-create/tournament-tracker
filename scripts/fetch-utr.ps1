<#
  Snapshots every upcoming UTR tennis event in the United States into one slim
  JSON file that the web app can read.

  Why this exists: UTR's search API only allows requests from UTR's own website
  (Access-Control-Allow-Origin: https://app.utrsports.net), so a browser page -
  like the phone version on GitHub Pages - is not allowed to call it. This script
  runs on GitHub's servers instead, where that rule doesn't apply, and publishes
  the result alongside the site.

  The whole country is fetched on purpose: filtering by distance happens in the
  app, on your device, so this public file says nothing about where you live.

  Runs under Windows PowerShell 5.1 and PowerShell 7 alike.
#>
param(
    [string]$Out = "data/utr-events.json",
    # Used only if UTR is unreachable: re-publish the last good snapshot rather
    # than wiping the data off the live site.
    [string]$Fallback = "https://wolfiepierce-create.github.io/tournament-tracker/data/utr-events.json"
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$api   = "https://api.utrsports.net/v2/search/events"
$today = (Get-Date).ToUniversalTime().ToString("MM/dd/yyyy")
# Geographic centre of the contiguous US; 1600 mi reaches every state incl. AK/HI edges.
$base  = "${api}?showTennisContent=true&showPickleballContent=false" +
         "&distance=1600mi&pin=39.5,-98.35" +
         "&range=eventSchedule.eventStartUtc%3E$([uri]::EscapeDataString($today))"
$headers = @{ "User-Agent" = "TournamentTracker/1.3 (personal use)"; "Accept" = "application/json" }

function Get-Utc([object]$v) {
    # UTR sends UTC times without a zone marker; make that explicit.
    if (-not $v) { return $null }
    $s = [string]$v
    if ($s -notmatch '(Z|[+-]\d\d:\d\d)$') { $s += "Z" }
    return $s
}

function Slim($src) {
    $loc = @($src.eventLocations)[0]
    if (-not $loc -or $loc.countryCode2 -ne "US") { return $null }
    $sch = $src.eventSchedule
    [ordered]@{
        id         = $src.id
        name       = $src.name
        club       = $src.club.name
        lat        = $loc.lat
        lng        = $loc.lng
        city       = $loc.display
        address    = $loc.googleFormattedName
        start      = Get-Utc $sch.eventStartUtc
        end        = Get-Utc $sch.eventEndUtc
        regOpen    = Get-Utc $sch.registrationStartUtc
        regClose   = Get-Utc $sch.registrationEndUtc
        utrRange   = $src.utrRange
        price      = $src.priceRange
        gender     = $src.gender
        teamType   = $src.teamType
        registered = $src.registeredCount
        state      = $src.eventState.value
        divisions  = @($src.eventDivisions | ForEach-Object { $_.name } | Where-Object { $_ })
    }
}

try {
    $all  = New-Object System.Collections.Generic.List[object]
    $seen = @{}
    $skip = 0; $total = 1; $pages = 0
    while ($skip -lt $total -and $pages -lt 60) {
        $r = Invoke-RestMethod -Uri "$base&top=100&skip=$skip" -Headers $headers -TimeoutSec 60
        $total = [int]$r.total
        $hits  = @($r.hits)
        if ($hits.Count -eq 0) { break }
        foreach ($h in $hits) {
            $e = Slim $h.source
            if ($e -and -not $seen.ContainsKey([string]$e.id)) { $seen[[string]$e.id] = 1; $all.Add($e) }
        }
        $skip += 100; $pages++
        Start-Sleep -Milliseconds 350      # be polite to UTR's servers
    }
    if ($all.Count -eq 0) { throw "UTR returned no US events" }

    $doc = [ordered]@{
        source      = "utr"
        generatedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        count       = $all.Count
        events      = $all
    }
    $json = $doc | ConvertTo-Json -Depth 6 -Compress
    Write-Host "Fetched $($all.Count) US events across $pages pages (UTR reports $total worldwide in range)."
}
catch {
    Write-Warning "UTR fetch failed: $($_.Exception.Message)"
    Write-Warning "Re-publishing the last good snapshot instead."
    try {
        $json = (Invoke-WebRequest -Uri $Fallback -UseBasicParsing -TimeoutSec 60).Content
    } catch {
        Write-Warning "No previous snapshot available either; publishing an empty set."
        $json = '{"source":"utr","generatedAt":null,"count":0,"events":[]}'
    }
}

$dir = Split-Path -Parent $Out
if ($dir) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
[IO.File]::WriteAllText((Join-Path (Get-Location) $Out), $json, (New-Object Text.UTF8Encoding $false))
Write-Host "Wrote $Out ($([math]::Round($json.Length/1KB)) KB)"
