param([string[]]$Schemas = @('public'), [string]$ContextName = 'ApplicationDbContext')
$ErrorActionPreference = 'Stop'
if (-not $env:AUTOMATION_Database__ConnectionString) { throw 'Set AUTOMATION_Database__ConnectionString in this process.' }
if ($ContextName -notmatch '^[A-Z][A-Za-z0-9]*$') { throw 'Invalid context name.' }
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $repo 'src/Automation.Database/Automation.Database.csproj'
# Timestamped generation folder: never force-overwrites handwritten repository or context files.
$folder = 'Generated/Scaffold_' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
Push-Location $repo
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'EF tool restore failed.' }
    $arguments = @('ef','dbcontext','scaffold',$env:AUTOMATION_Database__ConnectionString,
        'Npgsql.EntityFrameworkCore.PostgreSQL','--project',$project,'--output-dir',"$folder/Entities",
        '--context-dir',$folder,'--context',$ContextName,'--no-onconfiguring')
    foreach ($schema in $Schemas) { $arguments += @('--schema',$schema) }
    dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Database scaffolding failed.' }
} finally { Pop-Location }
# The connection is not saved to source. EF still receives it as a process argument; use a restricted workstation.
