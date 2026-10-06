param([switch]$Ui)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $repo
$previous = $env:AUTOMATION_RUN_UI
try {
    dotnet restore WebAutomation.sln --locked-mode --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    if ($Ui) { $env:AUTOMATION_RUN_UI = '1'; $filter = 'TestCategory=UI' }
    else { $filter = 'TestCategory!=UI' }
    dotnet test WebAutomation.sln --no-restore --filter $filter --logger 'trx' --results-directory artifacts/results
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
} finally { $env:AUTOMATION_RUN_UI = $previous; Pop-Location }
