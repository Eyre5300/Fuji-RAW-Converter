# Fuji S3/S5 Pro RAW Converter

A Windows GUI and CLI for Fujifilm S3 Pro / S5 Pro RAF photos, using the original Fujifilm conversion engines.

[简体中文](README.md) · [Download for Windows](https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/latest)

## Download and run

1. Open the [download page](https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/latest) and download **`Fuji-S3-S5-RAW-Converter-1.6.2-Windows.zip`**.
2. **Extract the entire ZIP**, then double-click **`Fuji-S3-S5-RAW-Converter.exe`** in the main folder.
3. Add photos or a folder, check the photos, select the output format and film simulation, and click **Convert selected**.

You do not need Source code downloads, a compiler or the separate official updater. Keep the `data` folder beside the launcher.

```text
Fuji-S3-S5-RAW-Converter/
├─ Fuji-S3-S5-RAW-Converter.exe  ← Open this
├─ 使用说明.txt
└─ data/                       ← Runtime files
```

Requires Windows and .NET Framework 4.8. Windows 11 has been tested; dependencies have not been verified on a completely clean system.

## Features

- RAF thumbnails, selection, browsing and photo metadata; convert only checked items.
- Original **8-bit / native 16-bit TIFF** and **full-resolution JPEG** export.
- Original film simulations: Standard, F1, F1a, F1b, F1c, F2. Available choices depend on the selected files and color space. Both TIFF and JPEG support them.
- Double-click to browse. Compare automatically matches by folder and filename, with selectable TIFF/JPEG results.
- Batch concurrency defaults to the logical CPU count and is adjustable.
- Output beside source files by default; rename existing outputs automatically. Remember input/output locations.
- Simplified Chinese / English selector in the top toolbar, with high-DPI support.
- Portrait TIFF pixel and thumbnail orientation normalization.

16-bit TIFF supports sRGB / Adobe RGB. JPEG uses a complete 16-bit sRGB render before encoding; default quality is 92. Film simulations change exported pixels, not the RAW browsing preview. Results are not guaranteed to match in-camera JPEGs exactly.

RAW browsing uses the original Fujifilm plugin. The tested S5 Pro file displays an embedded 1440×960 camera JPEG, while full export is 4256×2848. The left preview is not a full-resolution RAW decode. Display buffers are 8-bit and ICC-tagged results are mapped to sRGB.

## Camera compatibility

Primarily intended for **S3 Pro / S5 Pro**. Files are accepted by matching RAF processor codes to original engine modules, not a hard-coded two-camera list.

Other header/module matches checked: S2 Pro, F700, E550, E900, S5000, S5500, S5200/S5600, S6000fd/S6500fd, S9500 and S9600. A header match is not complete conversion validation. One S3 Pro file previously passed native 16-bit TIFF conversion; current checks focus on S5 Pro.

## Command line and integration

The CLI remains `data/s5raw.exe` for compatibility with S5Raw. Run from the extracted folder:

```powershell
.\data\s5raw.exe convert -i "D:\Photos\photo.RAF" -o "D:\Output\photo.tif" --bits 16
.\data\s5raw.exe convert -i "D:\Photos\photo.RAF" -o "D:\Output\photo.jpg" --film f1b
.\data\s5raw.exe convert -i "D:\Photos" --output-dir "D:\Output" --recursive --format jpeg --existing rename --json
.\data\s5raw.exe --help
```

`--film`: `standard|f1|f1a|f1b|f1c|f2`. `--jobs N`: concurrency. `--color-space adobe-rgb`: 16-bit TIFF or film-simulation TIFF.

Other applications can launch the CLI with `--json` and read UTF-8 JSON Lines. A `completed` event signals a saved output. CLI defaults to standard 8-bit TIFF and error on existing files; GUI starts with 16-bit TIFF and automatic renaming.

## Build from source

End users can skip this section. On Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -DestinationPath .\dist\Fuji-S3-S5-RAW-Converter
```

Uses the .NET Framework C# compiler without NuGet. Builds do not produce proprietary Fujifilm engines. Extract the portable release into the destination before building, or copy its complete `data/engine` into the output while retaining the newly compiled `s5raw-browser.exe`.

## Official software archive

The download page separately provides **`V3UP11B.EXE`**, the historical HS-V3 Windows **1.1b updater** found during research. It is not a full installer and may require an existing installation or original media. **You do not need it to run this tool.**

Retrieved from a [historical mirror](https://www.touslesdrivers.com/index.php?v_code=27749&v_page=23). The Release includes provenance and engine inspection notes in `HS-V3-1.1b-source-and-checksum.txt`, plus `SHA256SUMS.txt` for asset checksums.

Version 1.6.2 updates two conversion modules to **6.2.0.27** and aligns the language selector. One copied S5 Pro sample passed 8-bit TIFF, 16-bit TIFF and JPEG export; F1b pixels matched the previous engine. No image-quality improvement is inferred.

This is not an official Fujifilm product. Original engines, browser components and profiles retain their respective owners' rights; publishing this source does not relicense them.
