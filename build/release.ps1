#Requires -Version 5.1
<#
  Gera (e opcionalmente publica) um release do Auto Paint Heartopia COM
  auto-update, usando Velopack.

  Diferente de build/publish.ps1 (que faz o .exe portátil single-file):
  aqui o `dotnet publish` gera uma PASTA self-contained SEM single-file
  (o Velopack precisa dos arquivos soltos para calcular updates delta) e o
  `vpk` a empacota em:

    Releases/
      AutoPaintHeartopia-win-Setup.exe        <- o que o usuário baixa/instala
      AutoPaintHeartopia-<versão>-full.nupkg   <- pacote completo
      AutoPaintHeartopia-<versão>-delta.nupkg  <- diferença p/ a versão anterior
      releases.win.json / RELEASES-win         <- índice lido pelo app

  O instalador (--framework webview2) baixa e instala o WebView2 Runtime
  automaticamente se faltar na máquina do usuário.

  Uso:
    ./build/release.ps1 -Version 0.2.0
    ./build/release.ps1 -Version 0.2.0 -Publish -Token ghp_xxx
    $env:GITHUB_TOKEN='ghp_xxx'; ./build/release.ps1 -Version 0.2.0 -Publish

  Pré-requisitos: .NET SDK 8, Node/npm, e a CLI vpk
  (o script instala com `dotnet tool install -g vpk` se faltar).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Version,
    [string]$RepoUrl = 'https://github.com/lucaslange-ti/auto-paint-heartopia',
    [string]$Channel = 'win',
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [switch]$Publish,
    [string]$Token = $env:GITHUB_TOKEN
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$releases = Join-Path $root 'Releases'
$stage    = Join-Path $root 'artifacts\publish'

# --- 0. CLI vpk ------------------------------------------------------------
$toolsDir = Join-Path $env:USERPROFILE '.dotnet\tools'
if ($env:PATH -notlike "*$toolsDir*") { $env:PATH += ";$toolsDir" }
if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    Write-Host 'Instalando a CLI vpk...' -ForegroundColor Cyan
    dotnet tool install -g vpk
    if ($LASTEXITCODE -ne 0) { throw 'dotnet tool install -g vpk falhou' }
}

# --- 1. UI Svelte -> App/wwwroot ----------------------------------------
Write-Host 'Buildando a UI (ui/)...' -ForegroundColor Cyan
Push-Location (Join-Path $root 'ui')
try {
    npm ci
    if ($LASTEXITCODE -ne 0) { throw 'npm ci falhou' }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw 'npm run build falhou' }
} finally { Pop-Location }

# --- 2. dotnet publish (pasta, self-contained, SEM single-file) ---------
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
Write-Host "dotnet publish ($Configuration / $Runtime)..." -ForegroundColor Cyan
dotnet publish src/AutoPaint.App/AutoPaint.App.csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained `
    -p:PublishSingleFile=false `
    -p:Version=$Version `
    -o $stage
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falhou ($LASTEXITCODE)" }

# --- 3. baixa releases anteriores (para gerar o delta) -----------------
New-Item -ItemType Directory -Force $releases | Out-Null
Write-Host 'Baixando releases anteriores (delta)...' -ForegroundColor Cyan
$dl = @('download','github','--repoUrl',$RepoUrl,'--outputDir',$releases,'--channel',$Channel)
if ($Token) { $dl += @('--token',$Token) }
vpk @dl   # no 1º release não há nada para baixar; ignorar erro

# --- 4. empacota ------------------------------------------------------
Write-Host "vpk pack $Version..." -ForegroundColor Cyan
vpk pack `
    --packId AutoPaintHeartopia `
    --packVersion $Version `
    --packTitle 'Auto Paint Heartopia' `
    --packAuthors 'Auto Paint Heartopia' `
    --packDir $stage `
    --mainExe AutoPaintHeartopia.exe `
    --icon (Join-Path $root 'src\AutoPaint.App\appicon.ico') `
    --outputDir $releases `
    --channel $Channel `
    --framework webview2
if ($LASTEXITCODE -ne 0) { throw "vpk pack falhou ($LASTEXITCODE)" }

# --- 5. publica no GitHub Releases ----------------------------------
if ($Publish) {
    if (-not $Token) { throw 'Faltou -Token (ou $env:GITHUB_TOKEN) para publicar.' }
    Write-Host 'vpk upload github...' -ForegroundColor Cyan
    vpk upload github `
        --repoUrl $RepoUrl `
        --token $Token `
        --outputDir $releases `
        --channel $Channel `
        --publish `
        --releaseName "v$Version" `
        --tag "v$Version"
    if ($LASTEXITCODE -ne 0) { throw "vpk upload falhou ($LASTEXITCODE)" }
    Write-Host "Release v$Version publicado em $RepoUrl/releases" -ForegroundColor Green
} else {
    Write-Host "OK (sem publicar). Arquivos em: $releases" -ForegroundColor Green
    Write-Host 'Para subir ao GitHub: repita com  -Publish -Token <PAT>' -ForegroundColor Yellow
}

Get-ChildItem $releases -File | Select-Object Name, @{n='MB';e={[math]::Round($_.Length/1MB,1)}}
