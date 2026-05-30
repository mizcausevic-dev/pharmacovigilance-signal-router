$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
python "$PSScriptRoot\generate_screenshots.py"
Write-Host "README assets rendered in $root\screenshots"
