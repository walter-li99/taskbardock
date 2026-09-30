param(
    [string]$Font = "Segoe Fluent Icons",
    [string]$Out = "",
    [int[]]$Codes = @()
)

Add-Type -AssemblyName System.Drawing

if ([string]::IsNullOrEmpty($Out)) { $Out = Join-Path $env:TEMP "glyph_sheet.png" }

if ($Codes.Count -eq 0) {
    $Codes = @(
        0xE80D, 0xE80E, 0xE80F, 0xE810, 0xE811,
        0xE823, 0xE917, 0xE722, 0xE8B8, 0xEB51,
        0xEB52, 0xE734, 0xE735, 0xE8C3, 0xE752,
        0xE753, 0xE754, 0xE944, 0xE945, 0xE946,
        0xE7AD, 0xE7B8, 0xE81E, 0xE81F, 0xE820,
        0xE80A, 0xE8A9, 0xECA5, 0xE7C6, 0xE83D,
        0xE83E, 0xEEA1, 0xEA80, 0xE82D, 0xEA81,
        0xE8B9, 0xE7BF, 0xED58, 0xE8EC, 0xE8ED,
        0xE7C9, 0xE121, 0xE114, 0xEB5F, 0xE8F1
    )
}

$cols = 9
$cell = 92
$rows = [int][math]::Ceiling($Codes.Count / [double]$cols)
$w = $cols * $cell
$h = $rows * ($cell + 20)

$bmp = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::White)
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

$gf = New-Object System.Drawing.Font($Font, 46, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$lf = New-Object System.Drawing.Font("Consolas", 11)
$br = [System.Drawing.Brushes]::Black
$sfmt = New-Object System.Drawing.StringFormat
$sfmt.Alignment = [System.Drawing.StringAlignment]::Center
$sfmt.LineAlignment = [System.Drawing.StringAlignment]::Center

for ($i = 0; $i -lt $Codes.Count; $i++) {
    $col = $i % $cols
    $row = [int][math]::Floor($i / $cols)
    $xf = [float]($col * $cell)
    $yf = [float]($row * ($cell + 20))
    $cf = [float]$cell
    $r1 = New-Object System.Drawing.RectangleF($xf, $yf, $cf, $cf)
    $g.DrawString([string][char]$Codes[$i], $gf, $br, $r1, $sfmt)
    $label = "U+" + $Codes[$i].ToString("X4")
    $lx = $xf + [float]16
    $ly = $yf + $cf + [float]2
    $g.DrawString($label, $lf, $br, $lx, $ly)
}

$g.Dispose()
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output "saved $Out"
