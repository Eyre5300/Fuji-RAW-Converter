Fuji S3/S5 Pro RAW Converter

Batch-convert supported Fujifilm RAF photos to JPEG or TIFF, choose film simulations by camera model, and browse or compare the results.

Download the complete Windows portable package, version 1.7.7: https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/tag/v1.7.7 · 简体中文: https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter

Get started

Requires 64-bit Windows and .NET Framework 4.8. Extract the entire ZIP and run Fuji-S3-S5-RAW-Converter.exe from its root folder. Keep the data folder alongside it. No separate official converter installation or camera connection is required.

1. Add RAF photos or folders and check the photos to convert.
2. Choose the destination, JPEG or TIFF, and the applicable color space.
3. Select a film for each camera model. Each model can have a different setting; Help explains its available modes.
4. Select Convert selected. Browse the exported files or use Compare to compare them with the originals.

Output defaults to the source folder. Existing names receive a numeric suffix by default; skip, error and overwrite options are also available. Original RAF files are not modified. The interface supports English and Simplified Chinese.

Supported cameras and films

Camera coverage follows the support list of the official conversion engines included with version 1.7.7. The film columns list the choices and color-space conditions currently offered by this application.

| Camera | Available films | Conditions |
|---|---|---|
| FinePix S5 Pro | Standard, F1, F1a, F1b, F1c, F2 | Film modes support sRGB and Adobe RGB |
| FinePix S3 Pro | Standard, F1, F2 | F1/F2 require sRGB; Adobe RGB offers Standard only |
| X-T50 | Default / As shot, plus the 20 films below | sRGB and Adobe RGB; JPEG uses sRGB |

The 20 X-T50 film choices:

- Color: PROVIA / Standard, Velvia / Vivid, ASTIA / Soft, Classic Chrome, PRO Neg. Hi, PRO Neg. Std, Classic Neg., ETERNA, ETERNA Bleach Bypass, Nostalgic Neg., REALA ACE.
- Black and white: ACROS, ACROS + Ye, ACROS + R, ACROS + G, Monochrome, Monochrome + Ye, Monochrome + R, Monochrome + G.
- Sepia monochrome: Sepia.

Every camera below offers default conversion only, with no additional film choices:

| Series | Supported cameras | Available films |
|---|---|---|
| X-T | X-T1, X-T2, X-T3, X-T4, X-T5, X-T10, X-T20, X-T30, X-T30 II, X-T100, X-T200 | Default conversion only |
| X-H / X-Pro | X-H1, X-H2, X-H2S, X-Pro1, X-Pro2, X-Pro3 | Default conversion only |
| X-E / X-M / X-S | X-E1, X-E2, X-E2S, X-E3, X-E4, X-E5, X-M1, X-M5, X-S1, X-S10, X-S20 | Default conversion only |
| X-A | X-A1, X-A2, X-A3, X-A5, X-A7, X-A10, X-A20 | Default conversion only |
| X100 | FinePix X100, X100S, X100T, X100F, X100V, X100VI | Default conversion only |
| X compact | X10, X20, X30, X70, XF1, XF10 | Default conversion only |
| GFX | GFX 50R, GFX 50S, GFX50S II, GFX 100, GFX100S, GFX100 II, GFX100S II, GFX100RF | Default conversion only |
| FinePix E / F | E550, E900, F700, F710, F505EXR, F550EXR, F600EXR, F605EXR, F770EXR, F775EXR, F800EXR, F805EXR, F900EXR | Default conversion only |
| FinePix HS | HS10, HS11, HS20EXR, HS22EXR, HS30EXR, HS33EXR, HS35EXR, HS50EXR, HS55EXR | Default conversion only |
| FinePix S / SL | S1, S2 Pro, S20 Pro, S100FS, S200EXR, S5000, S5100, S5200, S5500, S5600, S6000fd, S6500fd, S7000, S9000, S9500, S9600, SL1000, SL1010 | Default conversion only |

Camera support is limited to this version’s list. RAW files from other brands are not supported. The film menu follows the detected file and selected color space; applying another camera model's films is not supported.

Output and color

| Output | Bit depth and color | Intended use |
|---|---|---|
| JPEG | 8-bit sRGB; adjustable quality, default 92 | Sharing, viewing and finished images |
| 8-bit TIFF | 8-bit/channel RGB; sRGB or Adobe RGB where available | Editing with a smaller file size |
| 16-bit TIFF | Native 16-bit/channel RGB; sRGB or Adobe RGB | Editing that benefits from greater tonal precision |

Some early FinePix cameras use the photo's camera color space for Standard + 8-bit TIFF and do not allow a separate color-space choice. Unavailable color controls are disabled. Output uses the full image dimensions supported for that camera.

ACROS and Monochrome produce black-and-white images; Sepia produces brown monochrome. Choose an available color film for color output. Default / As shot may preserve a monochrome capture style. A 16-bit RGB TIFF stores six bytes per pixel plus metadata, so it is normally much larger than the RAF.

Browsing and batches

Double-click a photo to browse it; Compare can match and switch between exported TIFF/JPEG files. The main RAW thumbnails use embedded previews and do not update with film selections; inspect the exported files for the film effect.

The configured parallel count is the maximum. Actual tasks adapt to measured memory use around a 90% memory budget, without rewriting the setting or trying to fill RAM at all costs. Only a memory-driven reduction shows the memory-pressure notice and current task count. Initial measurement and estimation run in the background.

Official-converter edit sidecars are not imported. Software output is not guaranteed to match an in-camera JPEG exactly.

Download and license

Release 1.7.7 and SHA-256 checksums: https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/tag/v1.7.7. Download the complete Windows ZIP; source code and compilation are not required.

This is not an official Fujifilm product. Third-party components retain their original licenses; see data/NOTICE.txt inside the package. Build, command-line and implementation details are in the developer documentation: https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter.
