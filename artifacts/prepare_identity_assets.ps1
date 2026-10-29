# Deterministic format/size adaptation of the cleaned logo: preserve its colors,
# isolate it from navy and center it in the adaptive foreground safe area.
Add-Type -AssemblyName System.Drawing
$taskRoot = Split-Path $PSScriptRoot
$sourcePath = Join-Path $PSScriptRoot 'identity/icon-source.png'
$source = [System.Drawing.Bitmap]::new($sourcePath)
$symbol = [System.Drawing.Bitmap]::new($source.Width, $source.Height)
$left = $source.Width; $top = $source.Height; $right = 0; $bottom = 0
for ($y = 0; $y -lt $source.Height; $y++) {
    for ($x = 0; $x -lt $source.Width; $x++) {
        $pixel = $source.GetPixel($x, $y)
        if ($pixel.B -gt 100 -and $pixel.G -gt 70) {
            $symbol.SetPixel($x, $y, $pixel)
            $left = [Math]::Min($left, $x); $right = [Math]::Max($right, $x)
            $top = [Math]::Min($top, $y); $bottom = [Math]::Max($bottom, $y)
        }
    }
}
$width = $right - $left + 1; $height = $bottom - $top + 1
$scale = 310.0 / [Math]::Max($width, $height)
$canvas = [System.Drawing.Bitmap]::new(512, 512)
$graphics = [System.Drawing.Graphics]::FromImage($canvas)
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$destination = [System.Drawing.RectangleF]::new((512-$width*$scale)/2, (512-$height*$scale)/2, $width*$scale, $height*$scale)
$graphics.DrawImage($symbol, $destination, [System.Drawing.RectangleF]::new($left,$top,$width,$height), [System.Drawing.GraphicsUnit]::Pixel)
$graphics.Dispose()
$canvas.Save((Join-Path $taskRoot 'Soundlet/Resources/AppIcon/soundlet_symbol.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Save((Join-Path $taskRoot 'Soundlet/Resources/Splash/soundlet_symbol.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$source.Dispose(); $symbol.Dispose(); $canvas.Dispose()
Write-Output "Symbol bounds: $left,$top,$width,$height; output 512x512 with 310px symbol."
