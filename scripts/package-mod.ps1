# package-mod.ps1
# 将 mod/qq-music-radio/ 打包为 qq-music-radio.scs（zip 格式、正斜杠路径）
# 用法: powershell -ExecutionPolicy Bypass -File scripts\package-mod.ps1

$ErrorActionPreference = "Stop"

$srcDir = Join-Path $PSScriptRoot "..\mod\qq-music-radio"
$dst = Join-Path $PSScriptRoot "..\mod\qq-music-radio.scs"

if (Test-Path $dst) { Remove-Item $dst -Force }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($dst, "Create")
try {
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $srcDir "manifest.sii"), "manifest.sii") | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $srcDir "def\live_streams.sii"), "def/live_streams.sii") | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $srcDir "description.txt"), "description.txt") | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $srcDir "icon.jpg"), "icon.jpg") | Out-Null
}
finally {
    $zip.Dispose()
}

Write-Host "已打包: $dst"
