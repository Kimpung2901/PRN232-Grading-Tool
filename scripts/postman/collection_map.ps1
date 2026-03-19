param(
  [Parameter(Mandatory=$true)][string]$CollectionPath,
  [string]$OutCsv = "",
  [switch]$AddIds
)

if (-not (Test-Path $CollectionPath)) {
  Write-Error "Collection not found: $CollectionPath"
  exit 1
}

$json = Get-Content $CollectionPath -Raw | ConvertFrom-Json

function Add-Ids([object[]]$items) {
  foreach ($i in $items) {
    if (-not $i.id) {
      $i | Add-Member -NotePropertyName id -NotePropertyValue ([guid]::NewGuid().ToString())
    }
    if ($i.item) { Add-Ids $i.item }
  }
}

function Flatten-Items([object[]]$items, [System.Collections.Generic.List[object]]$acc) {
  foreach ($i in $items) {
    if ($i.item) { Flatten-Items $i.item $acc }
    else { $acc.Add($i) }
  }
}

if ($AddIds) {
  Add-Ids $json.item
  $json | ConvertTo-Json -Depth 30 | Set-Content -Encoding UTF8 $CollectionPath
}

$acc = New-Object System.Collections.Generic.List[object]
Flatten-Items $json.item $acc

$rows = foreach ($i in $acc) {
  [pscustomobject]@{
    Name = $i.name
    PostmanItemId = $i.id
    Score = 1
    DependencyTestCaseId = ""
  }
}

if ([string]::IsNullOrWhiteSpace($OutCsv)) {
  $OutCsv = [System.IO.Path]::ChangeExtension($CollectionPath, ".map.csv")
}

$rows | Export-Csv -NoTypeInformation -Encoding UTF8 $OutCsv
Write-Host "Wrote mapping: $OutCsv"
