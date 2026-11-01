param([string]$Source = 'C:\Program Files\ISL\SILKYPIX Developer Studio 8.0 RFCEX')
$ErrorActionPreference = 'Stop'
$expected = 'afdac23b0107aed6e5454af7c810a9d4363ac4e3e99df1b14f9d5825cdf28ad8'
$dll = Join-Path $Source 'Library\SILKYPIX64.dll'
if ((Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) {
    throw 'Unsupported RFC EX DLL build. Private ABI offsets require the pinned 8.1.20.0 binary.'
}
$destination = Join-Path $PSScriptRoot 'resources\engine\RFCEX'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
foreach ($name in @('Library','ColorModeData','ICCProfiles','DefaultPresets','DefaultDevelopPresets','SPD50.ini','MakerTaste.ini','fsenable.inf','SILKYPIXStr.dat')) {
    $item = Get-Item -LiteralPath (Join-Path $Source $name)
    $files = if ($item.PSIsContainer) { Get-ChildItem -LiteralPath $item.FullName -Recurse -File } else { $item }
    foreach ($file in $files) {
        $target = Join-Path $destination $file.FullName.Substring($Source.TrimEnd('\').Length+1)
        if ((Test-Path -LiteralPath $target) -and (Get-FileHash -LiteralPath $file.FullName).Hash -eq (Get-FileHash -LiteralPath $target).Hash) { continue }
        New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }
}
# Names present in this exact engine; admission is not per-camera validation.
$ascii = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($dll))
$names = [regex]::Matches($ascii,'[\x20-\x7e]{3,}') | ForEach-Object { $_.Value.Trim() } | Where-Object {
    $_ -cmatch '^(X-(?:[A-Z]|Pro)\d+[A-Z]?(?: II)?|X(?:10|20|30|70|100[FSTV]?|100VI)|XF(?:1|10)|GFX[ A-Za-z0-9]+|FinePix[ A-Za-z0-9/]+)$'
} | Sort-Object -Unique
[IO.File]::WriteAllLines((Join-Path $destination 'models.txt'),[string[]]$names,(New-Object Text.UTF8Encoding $false))
$manifest = Get-ChildItem -LiteralPath $destination -Recurse -File | Where-Object Name -ne 'manifest.json' | ForEach-Object {
    [pscustomobject]@{file=$_.FullName.Substring($destination.Length+1).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
}
$manifest | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $destination 'manifest.json') -Encoding UTF8
Write-Output "Extracted RFC EX 8.1.20.0 to $destination ($($names.Count) camera names)."
