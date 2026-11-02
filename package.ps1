param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$packageRoot = Join-Path $OutputDirectory 'Fuji-RAW-Converter'
& "$PSScriptRoot\build.ps1" -DestinationPath $packageRoot
$version = [regex]::Match((Get-Content "$PSScriptRoot\src\Common.cs" -Raw), 'const string Version = "([^"]+)"').Groups[1].Value
$archive = Join-Path $OutputDirectory "Fuji-RAW-Converter-$version-Windows.zip"
Compress-Archive -Path "$packageRoot\*" -DestinationPath $archive -Force
Write-Output "Packaged: $archive"
