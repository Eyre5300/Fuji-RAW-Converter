Fuji S3/S5 Pro RAW Converter 1.7.7

Extract the entire ZIP and run the launcher in the parent folder. Keep data beside it.
Requires 64-bit Windows and .NET Framework 4.8. No official GUI or USB camera connection is needed.

Add/check photos and choose JPEG or 8/16-bit TIFF. Each imported camera model has a separate film selector.
Click Help beside a selector to read every mode available for that camera, with the current choice marked.
Help does not change your selections. ACROS/Monochrome produce B&W; choose PROVIA for color.
X-T50 provides as-shot plus 20 choices. S3/S5 retain their original F1/F2 modes.
Unchecking a camera disables its selector but retains its choice. Different cameras can use different films in one batch.

RAW previews are embedded camera JPEGs and do not change with export film settings.
Use Compare to inspect exported pixels. Official editor sidecars are not imported.
TIFF supports sRGB/Adobe RGB, except legacy standard 8-bit TIFF which follows camera color.
An uncompressed 40MP 16-bit RGB TIFF is about 239 MB. JPEG uses full 16-bit sRGB development before encoding.
All cameras follow the configured parallel task count. A current-task-count notice appears only during memory throttling and disappears after recovery. Each batch first completes one conversion alone and measures its actual memory use. The measured task and available RAM/system commit determine the initial parallel count against a 90% budget; no fixed per-task memory assumption is used. Live measurements then allow concurrency to increase when usage is low or decrease when high, always bounded by the configured count. Profiling and estimation run in the background without extra UI messages; a memory-pressure notice appears only when the budget reduces concurrency.
Version 1.7.7: compiled and packaged, not runtime-tested at the user’s request.
Other listed Fujifilm cameras can attempt default conversion but are not individually validated.
Modern film overrides are currently limited to X-T50. Other camera brands are not supported.

Use data/s5raw.exe --help for CLI options and info -i photo.RAF --json for available film keys.
Job-list entries can independently specify film and colorSpace; omitted fields use global options.
The original RAW files are read-only. Preferences are stored under HKEY_CURRENT_USER/Software/S5Raw.

Release: https://github.com/Eyre5300/Fuji-S3-S5-RAW-Converter/releases/latest

Version 1.7.7 adjusts only layout, alignment and spacing in the main window and film-help dialog. Conversion behavior, parameter defaults, camera-specific film choices and scheduling remain as in 1.7.6. Built and packaged without UI or conversion runtime tests, as requested.
