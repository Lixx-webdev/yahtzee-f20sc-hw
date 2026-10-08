# Rebuilds docs\Yahtzee_Code_Guide.pdf from the current source code.
#   powershell -File docs\guide-src\build_guide.ps1
# Requires Python 3 with Pillow (pip install pillow) and Microsoft Edge (installed with Windows 11).
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$pdf = Join-Path (Split-Path $here -Parent) 'Yahtzee_Code_Guide.pdf'
$html = Join-Path $here 'build\Yahtzee_Code_Guide.html'

# 1. Generate the HTML. This fails if any explanation no longer matches the source code.
python (Join-Path $here 'build.py')
if ($LASTEXITCODE -ne 0) { throw 'build.py failed: update the explanations in ann_*.py to match the changed code.' }

# 2. Print it to PDF with headless Edge, using a throw-away profile so your own Edge is not touched.
$edge = @("${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
          "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $edge) { throw 'Microsoft Edge was not found.' }
$profileDir = Join-Path $env:TEMP 'yahtzee-guide-edge-profile'
$url = 'file:///' + ($html -replace '\\', '/')
Start-Process $edge -Wait -ArgumentList '--headless=new', '--disable-gpu', '--no-first-run', "--user-data-dir=$profileDir",
    '--no-pdf-header-footer', "--print-to-pdf=$pdf", $url
Write-Output "PDF written: $pdf"
