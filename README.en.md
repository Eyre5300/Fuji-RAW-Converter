# Fuji S3/S5 Pro RAW Converter

Windows GUI and CLI for Fujifilm RAF, with the original S3/S5 engines and RAW FILE CONVERTER EX (SILKYPIX).

[简体中文](README.md) · [Published Windows releases](https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/latest)

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
- Uncompressed 40MP 16-bit RGB TIFF is approximately 239 MB. All cameras follow the configured parallel task count, with no fixed RFC EX two-worker cap. The UI shows a current-task-count notice only while memory pressure reduces parallelism; the notice disappears after recovery. A 90% RAM and system-commit budget, after subtracting other applications, is divided by observed per-task peak memory to choose a stable parallel task count, bounded by the configured count. One successful task calibrates the estimate. Task completion and brief memory dips do not ramp concurrency back up within the batch; sustained budget shortfalls reduce it while existing tasks finish. The configured value is never rewritten.
- Version 1.7.5 is built and packaged; runtime testing was not performed at the user’s request.

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

Memory-estimation details: cold estimates are 1 GiB per RFC EX task and 256 MiB per legacy task. Observed batch peaks then carry 10% headroom. A lower target must persist for 3 seconds (1 second at >=95% memory pressure). The 90% budget is a target, not an OS-enforced hard limit; workload phases and other applications can still change usage.
