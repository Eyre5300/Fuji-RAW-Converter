# Source layout

- `Common.cs`: shared version, argument quoting and RAF processor/module matching.
- `Cli.cs`: commands, batch scheduling and JSON Lines events.
- `Gui.cs`, `UiLanguage.cs`, `Preferences.cs`: main interface, localization and saved locations.
- `NativeWorker.cs`, `Native16.cs`, `FilmSimulation.cs`: original converter bridges and film profiles.
- `NativeBrowser.cs`: 32-bit Fujifilm browser bridge.
- `JpegExport.cs`, `TiffOrientation.cs`: full-size JPEG export and rendered TIFF orientation.
- `Viewer.cs`, `Compare.cs`, `Canvas.cs`, `FullPreview.cs`, `PhotoFiles.cs`, `PhotoInfo.cs`: browsing, matching, shared preview loading and metadata.
- `Launcher.cs`: root GUI launcher.

Build from the repository root with `build.ps1`. Proprietary Fujifilm engines are supplied separately in the portable Release, not in this source directory.
