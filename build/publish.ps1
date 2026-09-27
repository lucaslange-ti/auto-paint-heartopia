#Requires -Version 5.1
<#
  Publica o Auto Paint Heartopia como .exe self-contained single-file (win-x64).
  Saída: ./publish/
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$out = Join-Path $root 'publish'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }

Write-Host "Publicando ($Configuration / $Runtime)..." -ForegroundColor Cyan

dotnet publish src/AutoPaint.App/AutoPaint.App.csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $out

if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou ($LASTEXITCODE)" }

Write-Host "OK -> $out" -ForegroundColor Green
Get-ChildItem $out -Filter *.exe | Select-Object Name, @{n='MB';e={[math]::Round($_.Length/1MB,1)}}
