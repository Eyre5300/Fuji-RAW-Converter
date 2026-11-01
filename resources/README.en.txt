Fuji S3/S5 Pro RAW Converter 1.6.2

Open Fuji-S3-S5-RAW-Converter.exe in the parent folder. Choose English under Language.
Add photos or a folder, check the photos to convert, select the format and film simulation, then click Convert selected.
Language, the last input folder, output folder and Source folder option are remembered for the current Windows user.
The language can be changed without discarding photos, export options or open image comparisons.
Double-click a photo to browse. Compare matches RAW files to TIFF/JPEG results by folder and filename.

JPEG, 8-bit TIFF and 16-bit TIFF support Fujifilm film simulations. Available modes depend on the checked photos and color space.
S5 Pro: Standard, F1, F1a, F1b, F1c, F2. 16-bit TIFF remains 16 bits per channel.
RAW viewing follows the original Fujifilm browser plugin; export renders the full RAW. The RAW preview does not change with the export film setting.

Command-line examples (from the parent folder):
data\s5raw.exe convert -i photo.RAF -o photo.jpg --film f1b
data\s5raw.exe convert -i photo.RAF -o photo.tif --bits 16 --film f2 --color-space adobe-rgb
data\s5raw.exe convert -i photos --output-dir converted --recursive --format jpeg --film f1b --existing rename --json

--jobs defaults to the logical CPU count. --film accepts standard, f1, f1a, f1b, f1c, f2.
Use --json for UTF-8 JSON Lines events. Wait for completed, which includes the actual output path, bit depth, color space and selected film.
The command-line option names and JSON keys are language-independent. CLI help is in Chinese.
Settings are stored under HKEY_CURRENT_USER\Software\S5Raw.

Version 1.6.2 updates FG05FC09.dll and FG05FC0B.dll to 6.2.0.27 from HS-V3 1.1b in both conversion engines. Film resources and RAW browser components are unchanged. The top language selector is vertically centered with the buttons.
