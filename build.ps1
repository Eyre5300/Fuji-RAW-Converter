param([string]$DestinationPath)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$packageRoot = if ($DestinationPath) { [IO.Path]::GetFullPath($DestinationPath) } else { Join-Path $projectRoot 'dist\Fuji-S3-S5-RAW-Converter' }
$destination = Join-Path $packageRoot 'data'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
& $compiler /nologo /platform:x86 /optimize+ /r:System.Web.Extensions.dll /r:System.Xaml.dll "/r:$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\WPF\PresentationCore.dll" "/r:$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\WPF\WindowsBase.dll" "/out:$destination\s5raw.exe" "$projectRoot\src\Common.cs" "$projectRoot\src\NativeWorker.cs" "$projectRoot\src\Native16.cs" "$projectRoot\src\FilmSimulation.cs" "$projectRoot\src\Cli.cs" "$projectRoot\src\JpegExport.cs" "$projectRoot\src\TiffOrientation.cs"
if ($LASTEXITCODE -ne 0) { throw 'CLI build failed' }
& $compiler /nologo /platform:anycpu /optimize+ /target:winexe "/win32manifest:$projectRoot\src\app.manifest" /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Xaml.dll "/r:$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\WPF\PresentationCore.dll" "/r:$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\WPF\WindowsBase.dll" "/out:$destination\Fuji-S3-S5-RAW-Converter.exe" "$projectRoot\src\Common.cs" "$projectRoot\src\Gui.cs" "$projectRoot\src\UiLanguage.cs" "$projectRoot\src\Preferences.cs" "$projectRoot\src\FilmSimulation.cs" "$projectRoot\src\Viewer.cs" "$projectRoot\src\Compare.cs" "$projectRoot\src\PhotoFiles.cs" "$projectRoot\src\FullPreview.cs" "$projectRoot\src\Canvas.cs" "$projectRoot\src\PhotoInfo.cs"
if ($LASTEXITCODE -ne 0) { throw 'GUI build failed' }
Copy-Item -LiteralPath "$projectRoot\src\Fuji-S3-S5-RAW-Converter.exe.config" -Destination $destination -Force
& $compiler /nologo /platform:anycpu /optimize+ /target:winexe "/win32manifest:$projectRoot\src\app.manifest" /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll "/out:$packageRoot\Fuji-S3-S5-RAW-Converter.exe" "$projectRoot\src\Common.cs" "$projectRoot\src\Launcher.cs"
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed' }
$browserDestination = Join-Path $destination 'engine\browser'
New-Item -ItemType Directory -Force -Path $browserDestination | Out-Null
& $compiler /nologo /platform:x86 /optimize+ "/out:$browserDestination\s5raw-browser.exe" "$projectRoot\src\NativeBrowser.cs"
if ($LASTEXITCODE -ne 0) { throw 'Browser worker build failed' }
Write-Output "Built: $packageRoot"

