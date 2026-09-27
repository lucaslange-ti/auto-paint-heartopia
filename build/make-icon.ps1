#Requires -Version 5.1
<#
  Gera o ícone da aplicação a partir da arte em docs/icone.jpg:
  recorta quadrado central, arredonda os cantos (18%) e monta um .ico
  multi-resolução (16/24/32/48/64/128/256) em src/AutoPaint.App/appicon.ico.

  docs/ não é versionado — este script é ferramenta do mantenedor. O .ico
  gerado é o que vai para o Git e é embutido no .exe (<ApplicationIcon>).

  Uso:  ./build/make-icon.ps1        (roda no Windows PowerShell 5.1)
#>
[CmdletBinding()]
param(
    [string]$Source,
    [string]$OutIco,
    [string]$OutPng
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
if (-not $Source) { $Source = Join-Path $root 'docs\icone.jpg' }
if (-not $OutIco) { $OutIco = Join-Path $root 'src\AutoPaint.App\appicon.ico' }
if (-not $OutPng) { $OutPng = Join-Path $root 'artifacts\appicon-256.png' }

$Source = [System.IO.Path]::GetFullPath($Source)
$OutIco = [System.IO.Path]::GetFullPath($OutIco)
$OutPng = [System.IO.Path]::GetFullPath($OutPng)
if (-not (Test-Path $Source)) { throw "Arte não encontrada: $Source" }
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($OutPng)) | Out-Null

$src = [System.Drawing.Image]::FromFile($Source)
try {
    $M = 256
    $radius = [int]($M * 0.18)

    # recorte quadrado central
    $side = [Math]::Min($src.Width, $src.Height)
    $sx = [int](($src.Width  - $side) / 2)
    $sy = [int](($src.Height - $side) / 2)

    # imagem cheia 256x256
    $full = New-Object System.Drawing.Bitmap $M, $M, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $gf = [System.Drawing.Graphics]::FromImage($full)
    $gf.InterpolationMode = 'HighQualityBicubic'
    $gf.PixelOffsetMode   = 'HighQuality'
    $gf.DrawImage($src,
        (New-Object System.Drawing.Rectangle 0, 0, $M, $M),
        (New-Object System.Drawing.Rectangle $sx, $sy, $side, $side),
        [System.Drawing.GraphicsUnit]::Pixel)
    $gf.Dispose()

    # caminho de cantos arredondados
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc(0,       0,       $d, $d, 180, 90)
    $path.AddArc($M - $d, 0,       $d, $d, 270, 90)
    $path.AddArc($M - $d, $M - $d, $d, $d,   0, 90)
    $path.AddArc(0,       $M - $d, $d, $d,  90, 90)
    $path.CloseFigure()

    # master: pinta a imagem só dentro do caminho (bordas suavizadas)
    $master = New-Object System.Drawing.Bitmap $M, $M, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($master)
    $g.SmoothingMode = 'AntiAlias'
    $tb = New-Object System.Drawing.TextureBrush $full
    $g.FillPath($tb, $path)
    $tb.Dispose(); $g.Dispose(); $full.Dispose(); $path.Dispose()

    $master.Save($OutPng, [System.Drawing.Imaging.ImageFormat]::Png)

    # frames PNG para cada tamanho
    $sizes = 256, 128, 64, 48, 32, 24, 16
    $frames = foreach ($s in $sizes) {
        if ($s -eq $M) {
            $bmp = $master
        } else {
            $bmp = New-Object System.Drawing.Bitmap $s, $s, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $gg = [System.Drawing.Graphics]::FromImage($bmp)
            $gg.InterpolationMode = 'HighQualityBicubic'
            $gg.PixelOffsetMode   = 'HighQuality'
            $gg.SmoothingMode     = 'AntiAlias'
            $gg.DrawImage($master, (New-Object System.Drawing.Rectangle 0, 0, $s, $s))
            $gg.Dispose()
        }
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        if ($bmp -ne $master) { $bmp.Dispose() }
        [pscustomobject]@{ Size = $s; Bytes = $ms.ToArray() }
        $ms.Dispose()
    }

    # monta o .ico (ICONDIR + ICONDIRENTRY[] + PNGs)
    $out = New-Object System.IO.MemoryStream
    $bw  = New-Object System.IO.BinaryWriter $out
    $bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$frames.Count)
    $offset = 6 + 16 * $frames.Count
    foreach ($f in $frames) {
        $wh = if ($f.Size -ge 256) { 0 } else { $f.Size }
        $bw.Write([Byte]$wh); $bw.Write([Byte]$wh); $bw.Write([Byte]0); $bw.Write([Byte]0)
        $bw.Write([UInt16]1); $bw.Write([UInt16]32)
        $bw.Write([UInt32]$f.Bytes.Length); $bw.Write([UInt32]$offset)
        $offset += $f.Bytes.Length
    }
    foreach ($f in $frames) { $bw.Write($f.Bytes) }
    $bw.Flush()
    [System.IO.File]::WriteAllBytes($OutIco, $out.ToArray())
    $bw.Dispose(); $out.Dispose(); $master.Dispose()
}
finally { $src.Dispose() }

Write-Host "OK:" -ForegroundColor Green
Get-Item $OutIco | Select-Object FullName, Length
Write-Host "Preview: $OutPng"
