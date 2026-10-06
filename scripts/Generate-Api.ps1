param([string]$Manifest = (Join-Path $PSScriptRoot '../contracts/services.json'))
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
dotnet run --project (Join-Path $repo 'tools/ApiGenerator/ApiGenerator.csproj') -- ([IO.Path]::GetFullPath($Manifest))
if ($LASTEXITCODE -ne 0) { throw 'API generation failed.' }
