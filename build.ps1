param([string]$DestinationPath)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$packageRoot = if ($DestinationPath) { [IO.Path]::GetFullPath($DestinationPath) } else { Join-Path $projectRoot 'dist\Fuji-S3-S5-RAW-Converter' }
$destination = Join-Path $packageRoot 'data'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
# Shared vendor files are stored once in the repository; the native engines
# require them beside their respective configuration files at runtime.
foreach ($engineName in @('RAFCNV', 'RAFCNVX')) {
    $engineDestination = Join-Path $destination "engine\$engineName"
    New-Item -ItemType Directory -Force -Path $engineDestination | Out-Null
    Copy-Item -Path "$projectRoot\resources\engine\common\*" -Destination $engineDestination -Force
    Copy-Item -Path "$projectRoot\resources\engine\$engineName\*" -Destination $engineDestination -Force
}
Copy-Item -LiteralPath "$projectRoot\resources\engine\browser" -Destination "$destination\engine" -Recurse -Force
Copy-Item -LiteralPath "$projectRoot\resources\使用说明.txt" -Destination $packageRoot -Force
Copy-Item -LiteralPath "$projectRoot\resources\README.en.txt", "$projectRoot\resources\NOTICE.txt", "$projectRoot\LICENSE" -Destination $destination -Force
& $compiler /nologo /platform:x86 /optimize+ /r:System.Web.Extensions.dll /r:System.Xaml.dll "/r:$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\WPF\PresentationCore.dll" "/r:$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\WPF\WindowsBase.dll" "/out:$destination\s5raw.exe" "$projectRoot\src\Common.cs" "$projectRoot\src\RfcEx.cs" "$projectRoot\src\NativeWorker.cs" "$projectRoot\src\Native16.cs" "$projectRoot\src\FilmSimulation.cs" "$projectRoot\src\Cli.cs" "$projectRoot\src\AdaptiveParallelism.cs" "$projectRoot\src\JpegExport.cs" "$projectRoot\src\TiffOrientation.cs"
if ($LASTEXITCODE -ne 0) { throw 'CLI build failed' }
& $compiler /nologo /platform:anycpu /optimize+ /target:winexe "/win32manifest:$projectRoot\src\app.manifest" /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Xaml.dll "/r:$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\WPF\PresentationCore.dll" "/r:$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\WPF\WindowsBase.dll" "/out:$destination\Fuji-S3-S5-RAW-Converter.exe" "$projectRoot\src\Common.cs" "$projectRoot\src\RfcEx.cs" "$projectRoot\src\Gui.cs" "$projectRoot\src\FilmHelp.cs" "$projectRoot\src\UiLanguage.cs" "$projectRoot\src\Preferences.cs" "$projectRoot\src\FilmSimulation.cs" "$projectRoot\src\Viewer.cs" "$projectRoot\src\Compare.cs" "$projectRoot\src\PhotoFiles.cs" "$projectRoot\src\FullPreview.cs" "$projectRoot\src\Canvas.cs" "$projectRoot\src\PhotoInfo.cs"
if ($LASTEXITCODE -ne 0) { throw 'GUI build failed' }
Copy-Item -LiteralPath "$projectRoot\src\Fuji-S3-S5-RAW-Converter.exe.config" -Destination $destination -Force
& $compiler /nologo /platform:anycpu /optimize+ /target:winexe "/win32manifest:$projectRoot\src\app.manifest" /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll "/out:$packageRoot\Fuji-S3-S5-RAW-Converter.exe" "$projectRoot\src\Common.cs" "$projectRoot\src\RfcEx.cs" "$projectRoot\src\Launcher.cs"
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed' }
$browserDestination = Join-Path $destination 'engine\browser'
New-Item -ItemType Directory -Force -Path $browserDestination | Out-Null
& $compiler /nologo /platform:x86 /optimize+ "/out:$browserDestination\s5raw-browser.exe" "$projectRoot\src\NativeBrowser.cs"
if ($LASTEXITCODE -ne 0) { throw 'Browser worker build failed' }
Copy-Item -LiteralPath "$projectRoot\resources\engine\RFCEX" -Destination "$destination\engine" -Recurse -Force
& $compiler /nologo /platform:x64 /optimize+ /r:System.Web.Extensions.dll /r:System.Xaml.dll "/r:$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\WPF\PresentationCore.dll" "/r:$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\WPF\WindowsBase.dll" "/out:$destination\s5raw-rfcex.exe" "$projectRoot\src\Common.cs" "$projectRoot\src\RfcEx.cs" "$projectRoot\src\RfcExWorker.cs" "$projectRoot\src\JpegExport.cs" "$projectRoot\src\TiffOrientation.cs"
if ($LASTEXITCODE -ne 0) { throw 'RFC EX worker build failed' }
Write-Output "Built: $packageRoot"

