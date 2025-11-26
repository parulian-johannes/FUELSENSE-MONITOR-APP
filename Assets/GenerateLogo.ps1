# Generate Logo for FuelSense Monitor App
Add-Type -AssemblyName System.Drawing

# Create 256x256 bitmap
$bitmap = New-Object System.Drawing.Bitmap(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

# Background circle - Dark blue
$bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 30, 41, 59))
$graphics.FillEllipse($bgBrush, 8, 8, 240, 240)

# Border - Blue
$borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 59, 130, 246), 6)
$graphics.DrawEllipse($borderPen, 8, 8, 240, 240)

# Letter E - Blue
$font = New-Object System.Drawing.Font("Arial", 140, [System.Drawing.FontStyle]::Bold)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 59, 130, 246))
$stringFormat = New-Object System.Drawing.StringFormat
$stringFormat.Alignment = [System.Drawing.StringAlignment]::Center
$stringFormat.LineAlignment = [System.Drawing.StringAlignment]::Center
$graphics.DrawString("E", $font, $textBrush, [System.Drawing.RectangleF]::new(0, 0, 256, 256), $stringFormat)

# Gear icon (top right) - Green
$gearBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 16, 185, 129))
$graphics.FillEllipse($gearBrush, 175, 30, 35, 35)

$gearPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 13, 148, 136), 2)
$graphics.DrawEllipse($gearPen, 175, 30, 35, 35)

# Gear teeth (simplified)
for ($i = 0; $i -lt 8; $i++) {
    $angle = $i * 45
    $rad = [Math]::PI * $angle / 180
    $x1 = 192.5 + 17.5 * [Math]::Cos($rad)
    $y1 = 47.5 + 17.5 * [Math]::Sin($rad)
    $x2 = 192.5 + 21 * [Math]::Cos($rad)
    $y2 = 47.5 + 21 * [Math]::Sin($rad)
    $toothPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 13, 148, 136), 3)
    $graphics.DrawLine($toothPen, $x1, $y1, $x2, $y2)
}

# Gear center - Dark
$gearCenter = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 30, 41, 59))
$graphics.FillEllipse($gearCenter, 183, 38, 19, 19)

# Gear hole - Gray
$gearHole = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 55, 65, 81))
$graphics.FillEllipse($gearHole, 188, 43, 9, 9)

# Accent line - Blue
$linePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(150, 59, 130, 246), 4)
$graphics.DrawLine($linePen, 50, 210, 160, 210)

# Save PNG
$bitmap.Save("$PSScriptRoot\logo.png", [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "✅ logo.png created (256x256)" -ForegroundColor Green

# Create 128x128 version
$bitmap128 = New-Object System.Drawing.Bitmap(128, 128)
$graphics128 = [System.Drawing.Graphics]::FromImage($bitmap128)
$graphics128.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics128.DrawImage($bitmap, 0, 0, 128, 128)
$bitmap128.Save("$PSScriptRoot\logo_128.png", [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "✅ logo_128.png created (128x128)" -ForegroundColor Green

# Create 48x48 for icon
$bitmap48 = New-Object System.Drawing.Bitmap(48, 48)
$graphics48 = [System.Drawing.Graphics]::FromImage($bitmap48)
$graphics48.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics48.DrawImage($bitmap, 0, 0, 48, 48)
$bitmap48.Save("$PSScriptRoot\logo_48.png", [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "✅ logo_48.png created (48x48)" -ForegroundColor Green

# Create ICO file with multiple sizes
$icon = [System.Drawing.Icon]::FromHandle($bitmap128.GetHicon())
$fs = New-Object System.IO.FileStream("$PSScriptRoot\logo.ico", [System.IO.FileMode]::Create)
$icon.Save($fs)
$fs.Close()
Write-Host "✅ logo.ico created" -ForegroundColor Green

# Cleanup
$graphics.Dispose()
$graphics128.Dispose()
$graphics48.Dispose()
$bitmap.Dispose()
$bitmap128.Dispose()
$bitmap48.Dispose()

Write-Host "`n🎨 Logo generation complete!" -ForegroundColor Cyan
