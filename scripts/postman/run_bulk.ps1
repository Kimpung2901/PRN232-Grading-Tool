param(
  [string]$Root = "exams",
  [string]$NewmanPath = "newman",
  [string]$ReportDir = "reports",
  [switch]$DryRun
)

function Resolve-Newman($path) {
  $cmd = Get-Command $path -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Path }
  return $null
}

$rootPath = Join-Path (Get-Location) $Root
if (-not (Test-Path $rootPath)) {
  Write-Error "Root not found: $rootPath"
  exit 1
}

$newmanExe = Resolve-Newman $NewmanPath
if (-not $newmanExe) {
  Write-Error "Newman not found. Install with: npm i -g newman"
  exit 1
}

$examDirs = Get-ChildItem -Path $rootPath -Directory
if ($examDirs.Count -eq 0) {
  Write-Error "No exam folders under: $rootPath"
  exit 1
}

foreach ($exam in $examDirs) {
  $collection = Join-Path $exam.FullName "collection.json"
  if (-not (Test-Path $collection)) {
    Write-Host "Skip $($exam.Name): missing collection.json"
    continue
  }

  $envFile = Join-Path $exam.FullName "env.json"
  $targetsFile = Join-Path $exam.FullName "targets.csv"

  $targets = @()
  if (Test-Path $targetsFile) {
    $targets = Import-Csv $targetsFile
  } else {
    $targets = @([pscustomobject]@{ Name = "default"; BaseUrl = "http://localhost:5000" })
  }

  foreach ($t in $targets) {
    $name = if ($t.Name) { $t.Name } else { "target" }
    $baseUrl = if ($t.BaseUrl) { $t.BaseUrl } else { "http://localhost:5000" }

    $outDir = Join-Path $ReportDir $exam.Name
    New-Item -ItemType Directory -Force $outDir | Out-Null

    $jsonReport = Join-Path $outDir ("{0}-{1}.json" -f $name, (Get-Date -Format "yyyyMMdd-HHmmss"))

    $args = @(
      "run", $collection,
      "--env-var", ("baseUrl={0}" -f $baseUrl),
      "--reporters", "cli,json",
      "--reporter-json-export", $jsonReport
    )

    if (Test-Path $envFile) {
      $args += @("-e", $envFile)
    }

    Write-Host "Running $($exam.Name) / $name => $baseUrl"
    if (-not $DryRun) {
      & $newmanExe @args
    }
  }
}
