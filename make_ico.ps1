Add-Type -AssemblyName System.Drawing

$srcFile = "C:\Users\Karu\.gemini\antigravity-cli\brain\a0b72dad-60df-4dde-8f3d-be95087fd618\taskbar_drive_monitor_logo_1783636864210.jpg"
$destFile = "C:\Users\Karu\TaskbarDriveMonitor\Assets\app.ico"

# Create Assets folder if it doesn't exist
if (!(Test-Path -Path "C:\Users\Karu\TaskbarDriveMonitor\Assets")) {
    New-Item -ItemType Directory -Path "C:\Users\Karu\TaskbarDriveMonitor\Assets" | Out-Null
}

$orig = [System.Drawing.Image]::FromFile($srcFile)
$bmp = New-Object System.Drawing.Bitmap(256, 256)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($orig, 0, 0, 256, 256)
$g.Dispose()
$orig.Dispose()

$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $ms.ToArray()
$ms.Dispose()
$bmp.Dispose()

$fs = New-Object System.IO.FileStream($destFile, [System.IO.FileMode]::Create)
$bw = New-Object System.IO.BinaryWriter($fs)

$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]1)

$bw.Write([byte]0)
$bw.Write([byte]0)
$bw.Write([byte]0)
$bw.Write([byte]0)
$bw.Write([uint16]1)
$bw.Write([uint16]32)
$bw.Write([uint32]$pngBytes.Length)
$bw.Write([uint32]22)

$bw.Write($pngBytes)
$bw.Close()
$fs.Close()

Write-Host "Created app.ico successfully in TaskbarDriveMonitor."
