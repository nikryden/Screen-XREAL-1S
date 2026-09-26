<#
.SYNOPSIS
  Generates the XrealScreen app icon (ICO) and MSIX logo PNGs into src/XrealScreen.App/Assets.
  Design: blue rounded square, two light screens side by side, glasses outline below
  (small sizes: screens only). Re-run after changing the design.
#>
param([string]$AssetsDir = (Join-Path $PSScriptRoot '..\..\src\XrealScreen.App\Assets'))

Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

function New-RoundedPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = [Math]::Max(1, $r * 2)
    $p.AddArc($x, $y, $d, $d, 180, 90); $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90); $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure(); return $p
}

# Draws the logo mark into a square of size $s at ($ox,$oy). $background = draw the blue tile.
function Draw-Mark($g, [float]$ox, [float]$oy, [float]$s, [bool]$background) {
    $g.SmoothingMode = 'AntiAlias'
    if ($background) {
        $tile = New-RoundedPath $ox $oy $s $s ($s * 0.2)
        $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush ([System.Drawing.PointF]::new($ox, $oy)), ([System.Drawing.PointF]::new($ox + $s, $oy + $s)), ([System.Drawing.Color]::FromArgb(255, 30, 64, 175)), ([System.Drawing.Color]::FromArgb(255, 14, 116, 144))
        $g.FillPath($brush, $tile)
    }
    $small = $s -lt 40
    $screenW = $s * 0.34; $screenH = $screenW * 9 / 16 * 1.15; $gap = $s * 0.05
    $top = if ($small) { $oy + ($s - $screenH) / 2 } else { $oy + $s * 0.2 }
    $left = $ox + ($s - (2 * $screenW + $gap)) / 2
    $screenBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(245, 236, 246, 255))
    foreach ($i in 0, 1) {
        $sx = $left + $i * ($screenW + $gap)
        $g.FillPath($screenBrush, (New-RoundedPath $sx $top $screenW $screenH ([Math]::Max(1, $s * 0.025))))
    }
    if (-not $small) {
        # glasses: two lenses + bridge
        $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 125, 211, 252)), ([Math]::Max(1.5, $s * 0.045))
        $pen.LineJoin = 'Round'
        $lw = $s * 0.27; $lh = $s * 0.15; $ly = $oy + $s * 0.62; $bridge = $s * 0.08
        $l1 = $ox + ($s - (2 * $lw + $bridge)) / 2; $l2 = $l1 + $lw + $bridge
        $g.DrawPath($pen, (New-RoundedPath $l1 $ly $lw $lh ($lh * 0.45)))
        $g.DrawPath($pen, (New-RoundedPath $l2 $ly $lw $lh ($lh * 0.45)))
        $g.DrawLine($pen, $l1 + $lw, $ly + $lh * 0.35, $l2, $ly + $lh * 0.35)
    }
}

function Save-Png([int]$w, [int]$h, [string]$file, [float]$markSize, [bool]$tile = $true) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp); $g.Clear([System.Drawing.Color]::Transparent)
    Draw-Mark $g (($w - $markSize) / 2) (($h - $markSize) / 2) $markSize $tile
    $bmp.Save((Join-Path $AssetsDir $file), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}

function Get-PngBytes([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp); $g.Clear([System.Drawing.Color]::Transparent)
    Draw-Mark $g 0 0 $size $true
    $ms = New-Object System.IO.MemoryStream; $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose(); return ,$ms.ToArray()
}

# MSIX logos (sizes of the template assets)
Save-Png 88 88 'Square44x44Logo.scale-200.png' 88
Save-Png 24 24 'Square44x44Logo.targetsize-24_altform-unplated.png' 24
Save-Png 48 48 'Square44x44Logo.targetsize-48_altform-lightunplated.png' 48
Save-Png 300 300 'Square150x150Logo.scale-200.png' 220
Save-Png 620 300 'Wide310x150Logo.scale-200.png' 240
Save-Png 50 50 'StoreLogo.png' 50
Save-Png 48 48 'LockScreenLogo.scale-200.png' 48
Save-Png 1240 600 'SplashScreen.scale-200.png' 360

# Multi-size ICO with PNG-compressed entries (Windows Vista+)
$sizes = 16, 24, 32, 48, 64, 128, 256
$images = foreach ($s in $sizes) { ,(Get-PngBytes $s) }
$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $out
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $len = $images[$i].Length
    $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s }))); $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))
    $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$len); $bw.Write([uint32]$offset); $offset += $len
}
foreach ($img in $images) { $bw.Write($img) }
$bw.Flush(); [System.IO.File]::WriteAllBytes((Join-Path $AssetsDir 'AppIcon.ico'), $out.ToArray())
Write-Host "icons written to $AssetsDir"
