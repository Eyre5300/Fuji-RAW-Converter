# Fuji S3/S5 Pro RAW Converter

Windows GUI and CLI for Fujifilm RAF, with the original S3/S5 engines and RAW FILE CONVERTER EX (SILKYPIX).

[简体中文](README.md) · [Download Windows 1.7.7](https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/tag/v1.7.7)

## Run

Extract the whole ZIP and open `Fuji-S3-S5-RAW-Converter.exe`. Keep the `data` folder alongside it. Requires 64-bit Windows and .NET Framework 4.8. No official GUI or USB-connected camera is needed.

Add and check RAF photos, choose an output format and location, then choose a film separately for each camera model. X-T50 can use PROVIA while S5 Pro uses F1b in the same batch. Each selector has a **Help** button explaining all modes available for that model and locating the current choice without changing it. Click **Convert selected** to export.

Captions, selectors and Help buttons are vertically centered. Unchecking a camera disables its selector but retains its choice. More than three rows scroll. The toolbar switches Chinese/English. Compare matches and displays exported TIFF/JPEG files.

## Capabilities

- X-T50: full-resolution JPEG, 8-bit and native 16-bit TIFF; as-shot plus 20 film choices.
- S3/S5: original engines and supported F1/F2 modes remain available.
- ACROS and Monochrome produce black-and-white output; Sepia produces a brown monochrome. Choose PROVIA explicitly for color. As-shot may retain a monochrome camera setting.
- TIFF supports sRGB / Adobe RGB, except legacy standard 8-bit TIFF which follows camera color. RFC EX checks output bit depth and ICC. JPEG is encoded from full 16-bit sRGB development, default quality 92.
- RAW browsing uses embedded previews and does not redraw when export films change. Official editor sidecars are not imported.
- Uncompressed 40MP 16-bit RGB TIFF is approximately 239 MB. All cameras follow the configured parallel task count, with no fixed RFC EX two-worker cap. The UI shows a current-task-count notice only while memory pressure reduces parallelism; the notice disappears after recovery. Each batch first completes one conversion alone and measures its actual memory use. The measured task and available RAM/system commit determine the initial parallel count against a 90% budget; no fixed per-task memory assumption is used. Live measurements then allow concurrency to increase when usage is low or decrease when high, always bounded by the configured count. Profiling and estimation run in the background without extra UI messages; a memory-pressure notice appears only when the budget reduces concurrency.
- Version 1.7.7 was tested and approved by the user; the existing portable archive is published without rebuilding or additional runtime tests.

Other listed Fujifilm models can attempt default conversion but are not individually validated. Modern film overrides currently support X-T50 only. Other brands are not supported. Tested on Windows 11, not on a completely clean Windows installation.

## CLI and build

```powershell
.\data\s5raw.exe convert -i photo.RAF -o photo.tif --bits 16 --film provia
.\data\s5raw.exe convert -i s5pro.RAF -o photo.jpg --film f1b
.\data\s5raw.exe convert --job-list jobs.json --output-dir output --format jpeg --json
.\data\s5raw.exe info -i photo.RAF --json
.\data\s5raw.exe doctor --json
```

Job-list entries contain `input` and `relative`, optionally overriding `film` and `colorSpace` per file. Omitted values use global options. JSON Lines `completed` events confirm saved outputs. See `--help` for all options.

Run `powershell -ExecutionPolicy Bypass -File .\package.ps1` on Windows. No NuGet or old release is required. Five executables are built using the .NET Framework compiler. The extracted dependencies in `resources/engine` are copied automatically. `build.ps1 -DestinationPath` and `package.ps1 -OutputDirectory` support separate output directories. Resources total about 558 MiB; the ZIP is about 513 MiB.

The isolated RFC EX x64 worker uses a private ABI pinned to SILKYPIX64.dll 8.1.20.0 by SHA-256. Do not substitute another DLL version. `extract-rfcex.ps1` extracts a matching installation and creates a checksum manifest. Legacy workers are x86. WinForms/WPF and native Windows engines prevent native Linux/macOS operation; Wine is untested. GitHub Actions builds Windows ZIPs.

Project code is MIT licensed; third-party binaries retain their original terms. See [LICENSE](LICENSE) and [NOTICE](resources/NOTICE.txt). This is not an official Fujifilm product.

Help summaries use the [X-T50 manual](https://fujifilm-dsc.com/en/manual/x-t50/menu_shooting/image_quality_setting/#film_simulation) and [S5 Pro manual, p.111](https://dl.fujifilm-x.com/support/manual/s/finepix_s5pro_manual_01.pdf). Software results may differ from in-camera JPEGs.

Memory scheduling: the first successful task supplies measured peaks for the initial estimate; a 5-second smoothed live per-task measurement then replaces that estimate. Usage below 88% for 5 seconds permits an increase if tasks remain queued, limited to 25% of the current count per adjustment (at least one). Usage above 92% for 3 seconds reduces concurrency; at 95% the hold is shortened to 1 second. Ordinary adjustments are at least 5 seconds apart, and reductions wait for active tasks to drain. The 90% budget is a target, not an OS-enforced hard limit. The configured value is never changed, and no additional CPU-based cap is applied.

Version 1.7.7 adjusts only layout, alignment and spacing in the main window and film-help dialog. Conversion behavior, parameter defaults, camera-specific film choices and scheduling remain as in 1.7.6. Tested and approved by the user; published without rebuilding or additional runtime tests.
