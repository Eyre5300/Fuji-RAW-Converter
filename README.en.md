# Fuji S3/S5 Pro RAW Converter

**A Windows GUI and command-line tool for older Fujifilm RAF files, using the original Fujifilm conversion engines.**

[简体中文](README.md) · [Download](https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/latest) · [Changelog](CHANGELOG.md)

Fuji S3/S5 Pro RAW Converter grew out of a FinePix S5 Pro photo selection and batch conversion workflow. It wraps Hyper-Utility3's browser, LE converter and native 16-bit converter in a compact application. Other applications can invoke its CLI as a subprocess.

Previously named S5Raw. The CLI remains `s5raw.exe`; existing preferences and preview caches are retained.

## Quick start

Download `Fuji-S3-S5-RAW-Converter-1.6.2-Windows.zip` from Releases, extract the whole archive and open `Fuji-S3-S5-RAW-Converter.exe`. The root contains the launcher, a user guide and a `data` folder. The CLI is `data/s5raw.exe`.

Add RAF files or folders, check the photos, select the output format and film simulation, then choose **Convert selected**. Double-click a photo to browse. **Compare** matches results automatically by folder and filename, with TIFF/JPEG selection and linked pan/zoom.

The top language selector switches between Simplified Chinese and English. Language, last input folder, output folder and the source-folder output choice are remembered. The default batch concurrency is the logical CPU count.

## Output and preview

| Format | Pipeline | Color space |
| --- | --- | --- |
| Standard 8-bit TIFF | Original LE converter | LE camera setting |
| Film-simulation 8-bit TIFF | Original advanced converter | sRGB / Adobe RGB |
| 16-bit TIFF | Native 16-bit conversion | sRGB / Adobe RGB |
| JPEG | Full-resolution 16-bit render, then 8-bit JPEG encoding | sRGB; quality 1–100, default 92 |

S5 Pro modes are Standard, F1, F1a, F1b, F1c and F2. Available choices depend on the selected files and color space. They use Fujifilm profiles, apply to both JPEG and TIFF, and do not alter the RAW browsing preview. Export is not guaranteed to match the in-camera JPEG exactly.

The GUI uses Fujifilm's `PIR030RAF` browser plugin, not an intermediate TIFF conversion. For the tested S5 Pro file this displays the embedded 1440×960 camera JPEG, whereas full RAW export is 4256×2848. The comparison is therefore not a full-resolution RAW-versus-output pixel comparison. Background loading, adjacent-file prefetch and memory/disk caching improve navigation. The display buffer is 8-bit; tagged results are mapped to sRGB, without monitor soft proofing.

Portrait TIFF pixels and thumbnail orientation are normalized for viewers that ignore orientation metadata. RAW files are admitted by matching their processor code to an available Fujifilm module, rather than by a hard-coded S5 Pro model name. RAF header/module matches were checked for S2 Pro, S3 Pro, S5 Pro, F700, E550, E900, S5000, S5500, S5200/S5600, S6000fd/S6500fd, S9500 and S9600. One S3 Pro file previously passed native 16-bit TIFF conversion; current testing focuses on S5 Pro. Header/module availability is not exhaustive camera validation.

## Command line

Run from the extracted application folder:

```powershell
.\data\s5raw.exe convert -i "D:\Photos\photo.RAF" -o "D:\Output\photo.tif" --bits 16
.\data\s5raw.exe convert -i "D:\Photos\photo.RAF" -o "D:\Output\photo.jpg" --film f1b
.\data\s5raw.exe convert -i "D:\Photos" --output-dir "D:\Output" --recursive --format jpeg --existing rename --json
.\data\s5raw.exe info -i "D:\Photos\photo.RAF" --json
.\data\s5raw.exe doctor --json
```

`--film`: `standard|f1|f1a|f1b|f1c|f2`. `--jobs N` overrides logical-CPU concurrency. `--color-space adobe-rgb` is supported for 16-bit TIFF and film-simulation TIFF. JPEG uses sRGB. CLI defaults are standard 8-bit TIFF and error on an existing output; GUI starts with 16-bit TIFF and automatic renaming. Without an output directory, files are written beside the source.

With `--json`, stdout emits UTF-8 JSON Lines: `batch_started`, `file_started`, `progress`, `completed`, `file_error`, `skipped`, `summary`. Treat `completed` as the saved-file signal; it includes `output`, `format`, `bits`, `colorSpace` and `film`. `--job-list` accepts an array of objects with `input` and optional `relative`. Cancel with Ctrl+C or `--cancel-file`.

Exit codes: 0 success/skipped, 2 arguments, 3 input, 4 engine, 5 conversion, 130 cancelled. CLI option names and JSON keys are language-independent; CLI help is currently Chinese.

## Fuji S3/S5 Pro RAW Converter and SuperCCD

[SuperCCD](https://github.com/anibarro/superccd) focuses on merging S3/S5 S/R responses into an editable 6MP CFA DNG and also offers rendered preview exports. Fuji S3/S5 Pro RAW Converter uses Fujifilm's proprietary engines to produce rendered RGB TIFF/JPEG. It does not create a CFA DNG or expose S/R merge tuning. Choose the workflow that fits the intended output. The README organization was inspired by SuperCCD; Fuji S3/S5 Pro RAW Converter is not a fork.

## Build

Windows and .NET Framework 4.8 are required; Windows 11 has been tested. Fujifilm's 32-bit components run in subprocesses. A completely clean Windows installation has not been tested.

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -DestinationPath .\dist\Fuji-S3-S5-RAW-Converter
```

The script uses the Framework C# compiler, without NuGet. Source builds do not download or produce proprietary engines. Extract the release into the destination first, then build over it, or copy its complete `data/engine` directory to the build output while retaining the freshly compiled `s5raw-browser.exe`.

## Version 1.6.2

`FG05FC09.dll` (S5 Pro) and `FG05FC0B.dll` are updated from 6.2.0.26 to **6.2.0.27**, from HS-V3 1.1b, in both engines. Film resources and browser components are unchanged. The language selector is vertically centered with the toolbar buttons.

One copied S5 Pro sample passed standard 8-bit TIFF, F1b native 16-bit TIFF and full JPEG export. F1b decoded pixels matched the old engine. This does not demonstrate a quality improvement or universal camera compatibility.

The Release also archives `V3UP11B.EXE`, the HS-V3 Windows 1.1b updater found during research. It is **not a full installer**, and running it is unnecessary for the bundled Fuji S3/S5 Pro RAW Converter release. See [source and checksums](OFFICIAL_UPDATE.md) and [third-party notices](THIRD_PARTY_NOTICES.md). Fuji S3/S5 Pro RAW Converter is an independent community tool, not an official Fujifilm product.
