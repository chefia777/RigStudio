# PowerShell script to generate a 64x64 pixel art humanoid character PNG
# Uses System.Drawing (available on Windows/.NET Framework + .NET Core on Windows)

Add-Type -AssemblyName System.Drawing

$width = 64
$height = 64
$bmp = New-Object System.Drawing.Bitmap $width, $height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half

# Transparent background
$g.Clear([System.Drawing.Color]::Transparent)

# Pen: dark gray, 3px wide
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 60, 60, 60), 3)
$pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

# Head (circle)
$g.DrawEllipse($pen, 24, 4, 16, 16)

# Body (vertical line)
$g.DrawLine($pen, 32, 20, 32, 40)

# Arms (diagonal lines from shoulder)
$g.DrawLine($pen, 32, 28, 20, 20)
$g.DrawLine($pen, 32, 28, 44, 20)

# Legs (diagonal lines from hip)
$g.DrawLine($pen, 32, 40, 22, 56)
$g.DrawLine($pen, 32, 40, 42, 56)

$outputPath = Join-Path -Path $PSScriptRoot -ChildPath "character.png"
$bmp.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)

$g.Dispose()
$bmp.Dispose()
$pen.Dispose()

Write-Host "Created $outputPath ($($width)x$($height) pixel art humanoid)"
