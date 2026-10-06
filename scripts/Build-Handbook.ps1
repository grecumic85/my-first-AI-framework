param([string]$Repository = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$docsRoot = Join-Path $Repository 'docs'
$chapters = @('ARCHITECTURE','HIGH-LEVEL-DESIGN','LOW-LEVEL-DESIGN','DECISIONS','SOLID','DATE-PICKERS','NEW-APP-RECIPE','EXTENDING','EXTRACTION','VALIDATION')
function Inline([string]$value) {
    $value = [System.Net.WebUtility]::HtmlEncode($value)
    $value = [regex]::Replace($value, '!\[([^\]]*)\]\(([^)]+)\)', {
        param($m)
        $asset = Join-Path $docsRoot $m.Groups[2].Value
        if (-not (Test-Path -LiteralPath $asset -PathType Leaf)) { throw "Missing diagram: $asset" }
        $encoded = [Convert]::ToBase64String([IO.File]::ReadAllBytes($asset))
        '<a class="diagram-link" href="' + $m.Groups[2].Value + '"><img alt="' + $m.Groups[1].Value + '" src="data:image/svg+xml;base64,' + $encoded + '"></a>'
    })
    $value = [regex]::Replace($value, '\[([^\]]+)\]\(([^)]+)\)', {
        param($m)
        $url = $m.Groups[2].Value
        foreach ($chapter in $chapters) {
            if ($url -eq "$chapter.md") { $url = "#chapter-$chapter"; break }
        }
        '<a href="' + $url + '">' + $m.Groups[1].Value + '</a>'
    })
    $tick = [char]96
    $value = [regex]::Replace($value, "$tick([^$tick]+)$tick", '<code>$1</code>')
    $value = [regex]::Replace($value, '\*\*(.+?)\*\*', '<strong>$1</strong>')
    return $value
}
function Markdown([string]$content) {
    $result = [Collections.Generic.List[string]]::new()
    $inCode = $false; $inTable = $false; $list = ''
    $fence = '^' + ([string][char]96) * 3
    foreach ($line in ($content -split "\r?\n")) {
        if ($line -match $fence) {
            if ($inCode) { $result.Add('</code></pre>'); $inCode = $false }
            else { $result.Add('<pre><code>'); $inCode = $true }
            continue
        }
        if ($inCode) { $result.Add([System.Net.WebUtility]::HtmlEncode($line)); continue }
        if ($inTable -and $line -notmatch '^\|') { $result.Add('</tbody></table></div>'); $inTable = $false }
        if ($list -and $line -notmatch '^(- |\d+\. )') { $result.Add("</$list>"); $list = '' }
        if ($line -match '^\|') {
            if ($line -match '^\|[\s:|\-]+\|$') { continue }
            $tag = 'td'
            if (-not $inTable) { $result.Add('<div class="table"><table><tbody>'); $inTable = $true; $tag = 'th' }
            $cells = $line.Trim().Trim('|').Split('|') | ForEach-Object { "<$tag>$(Inline $_.Trim())</$tag>" }
            $result.Add('<tr>' + ($cells -join '') + '</tr>'); continue
        }
        if ($line -match '^(#{1,6}) (.+)$') {
            $level = [Math]::Min(6, $Matches[1].Length + 1)
            $result.Add("<h$level>$(Inline $Matches[2])</h$level>"); continue
        }
        if ($line -match '^(- |\d+\. )(.+)$') {
            $kind = if ($Matches[1].StartsWith('-')) { 'ul' } else { 'ol' }
            $text = $Matches[2]
            if ($list -ne $kind) { if ($list) { $result.Add("</$list>") }; $result.Add("<$kind>"); $list = $kind }
            $result.Add('<li>' + (Inline $text) + '</li>'); continue
        }
        if ($line.Trim().Length) { $result.Add('<p>' + (Inline $line) + '</p>') }
    }
    if ($inTable) { $result.Add('</tbody></table></div>') }
    if ($list) { $result.Add("</$list>") }
    if ($inCode) { throw 'Unterminated code fence in documentation' }
    return $result -join [Environment]::NewLine
}
$navigation = [Collections.Generic.List[string]]::new()
$sections = [Collections.Generic.List[string]]::new()
foreach ($chapter in $chapters) {
    $content = [IO.File]::ReadAllText((Join-Path $docsRoot "$chapter.md"))
    $title = ($content -split "\r?\n")[0].TrimStart('#',' ')
    $navigation.Add('<a href="#chapter-' + $chapter + '">' + [System.Net.WebUtility]::HtmlEncode($title) + '</a>')
    $sections.Add('<section id="chapter-' + $chapter + '">' + (Markdown $content) + '</section>')
}
$style = @'
:root{color-scheme:light;--ink:#19354e;--muted:#587086;--accent:#167b79;--line:#dce5ed}
*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;color:var(--ink);font:16px/1.65 "Segoe UI",Arial,sans-serif;background:#f3f6f9}
aside{position:fixed;inset:0 auto 0 0;width:280px;background:#16344a;color:white;overflow:auto;padding:30px 22px}
aside strong{display:block;font-size:22px;line-height:1.3;margin-bottom:12px}aside small{color:#bdd4e2}
nav{margin-top:30px}nav a{display:block;padding:9px 0;color:#d6e6ef;text-decoration:none;font-size:14px;border-bottom:1px solid #ffffff20}
nav a:hover{color:white}main{margin-left:280px;max-width:1300px;padding:45px 50px}header{margin-bottom:40px}
.eyebrow{text-transform:uppercase;letter-spacing:2px;font-size:12px;color:var(--accent);font-weight:700}
h1{font-size:42px;line-height:1.15;margin:12px 0 18px}header p{font-size:19px;color:var(--muted);max-width:780px}
section{background:white;padding:34px 40px;margin-bottom:26px;border:1px solid var(--line);border-radius:12px;scroll-margin-top:20px}
h2{font-size:29px;line-height:1.25;margin:0 0 24px}h3{font-size:22px;margin-top:34px}h4{font-size:18px}
a{color:#076e86}p{margin:14px 0}li{margin:7px 0}code{font:13px Consolas,monospace;background:#edf3f7;padding:2px 4px;border-radius:3px}
pre{overflow:auto;background:#132e43;color:#e7f3fb;padding:22px;border-radius:8px;font-size:13px;line-height:1.5}pre code{background:none;color:inherit;padding:0}
.table{overflow:auto}table{border-collapse:collapse;font-size:14px;width:100%;margin:20px 0}th,td{text-align:left;vertical-align:top;padding:12px;border:1px solid var(--line)}th{background:#eaf2f7}
img{display:block;width:100%;height:auto;border:1px solid var(--line);border-radius:7px;margin:22px 0}.diagram-link{display:block}footer{color:var(--muted);font-size:13px}
@media(max-width:1000px){aside{position:static;width:100%}nav{display:flex;gap:12px;flex-wrap:wrap}main{margin:0;padding:25px}section{padding:24px}h1{font-size:34px}}
@media print{aside{display:none}main{margin:0;padding:0;max-width:none}section{border:0;padding:20px 0;break-before:page}img,table,pre{break-inside:avoid}a{color:inherit}body{background:white}}
'@
$html = '<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Web Automation Starter - Architecture Handbook</title><style>' + $style + '</style></head><body><aside><strong>Web Automation<br>Starter</strong><small>Architecture and implementation handbook<br>Documentation edition 2</small><nav>' + ($navigation -join '') + '</nav></aside><main><header><div class="eyebrow">C# / Selenium / API / EF / Emulation</div><h1>Understand the framework.<br>Build your next test platform.</h1><p>A code-grounded guide to the design, tradeoffs, ownership and extension points, with UML diagrams and four real-world calendar adapters.</p><p>Offline document. Diagrams are embedded; click one to open its standalone SVG. Read the validation chapter for executed results and limits.</p></header>' + ($sections -join [Environment]::NewLine) + '<footer>Generated from repository Markdown and checked-in SVGs by scripts/Build-Handbook.ps1. No external JavaScript, fonts, analytics or diagram service is required.</footer></main></body></html>'
[IO.File]::WriteAllText((Join-Path $docsRoot 'handbook.html'), $html, [Text.UTF8Encoding]::new($false))
Write-Output 'Generated docs/handbook.html'
