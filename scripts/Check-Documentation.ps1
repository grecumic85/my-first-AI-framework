param([string]$Repository = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$docsRoot = Join-Path $Repository 'docs'
$broken = [Collections.Generic.List[string]]::new()
$checkedLinks = 0
foreach ($file in Get-ChildItem -LiteralPath $docsRoot -Recurse -Filter *.md) {
    $content = [IO.File]::ReadAllText($file.FullName)
    foreach ($match in [regex]::Matches($content, '\[[^\]]*\]\(([^)]+)\)')) {
        $link = $match.Groups[1].Value
        if ($link -match '^(https?://|#|mailto:)') { continue }
        $path = ($link -split '#')[0]
        $checkedLinks++
        $target = Join-Path $file.DirectoryName $path
        if (-not (Test-Path -LiteralPath $target)) { $broken.Add($file.Name + ': ' + $link) }
    }
}
$svgCount = 0
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $docsRoot 'diagrams') -Filter *.svg) {
    $xml = [xml][IO.File]::ReadAllText($file.FullName)
    if ($xml.DocumentElement.LocalName -ne 'svg') { throw "Invalid SVG: $($file.Name)" }
    $svgCount++
}
$mermaidCount = @(Get-ChildItem -LiteralPath (Join-Path $docsRoot 'diagrams') -Filter *.mmd).Count
$html = [IO.File]::ReadAllText((Join-Path $docsRoot 'handbook.html'))
$images = [regex]::Matches($html, 'src="data:image/svg\+xml;base64,').Count
$sections = [regex]::Matches($html, '<section id=').Count
$externalRuntime = $html -match '<script\b|<link[^>]+https?://|<img[^>]+src="https?://'
$result = [ordered]@{ checkedAtUtc=[DateTime]::UtcNow.ToString('o'); localLinksChecked=$checkedLinks; brokenLinks=@($broken); svgXmlValid=$svgCount; mermaidSourceFiles=$mermaidCount; handbookEmbeddedDiagrams=$images; handbookChapters=$sections; externalRuntimeDependencies=$externalRuntime; browserVisualPreview='Not performed: local file URL blocked by browser tool policy'; mermaidParserValidation='Not performed; editable sources accompanied by checked-in SVGs' }
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $docsRoot 'documentation-checks.json') -Encoding UTF8
$result | ConvertTo-Json -Depth 4
if ($broken.Count -or $svgCount -ne 12 -or $mermaidCount -ne 12 -or $sections -ne 10 -or $images -lt 12 -or $externalRuntime) { exit 1 }
