param([ValidateSet('restore','build','test','run')][string]$Action = 'run')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetExecutable = $null
if ($dotnetCommand) {
    $installedSdks = & $dotnetCommand.Source --list-sdks
    if ($installedSdks -match '^8\.0\.') { $dotnetExecutable = $dotnetCommand.Source }
}
if (-not $dotnetExecutable) {
    $localSdk = Join-Path $env:USERPROFILE '.cache\swd392-dotnet8\dotnet.exe'
    if (Test-Path -LiteralPath $localSdk) { $dotnetExecutable = $localSdk }
}
if (-not $dotnetExecutable) { throw 'Cài .NET SDK 8.0.x trước khi chạy project. Xem README.md.' }
Push-Location $projectRoot
try {
    if ($Action -eq 'run') {
        & $dotnetExecutable run --project src/BookSales.Web --launch-profile http
    } else {
        & $dotnetExecutable $Action BookSales.sln
    }
    if ($LASTEXITCODE -ne 0) { throw "dotnet $Action failed (exit $LASTEXITCODE)." }
} finally { Pop-Location }
