# TaskbarDriveMonitor (v1.0.0)

A lightweight, clean disk capacity widget running on your Windows Taskbar. It sits next to your system tray (or in custom positions) and displays selected drives' free space (percentage and GB) along with dynamic, color-coded progress bars.

---

## Features

* **Taskbar Overlay**: Fully borderless, non-focus-stealing widget (`WS_EX_NOACTIVATE`) that sits on top of the taskbar.
* **Disk Space Display**: Shows drive letters, free space percentage, and free/total space in GB (e.g., `C: 45.2% free` and `120.5 / 256 GB`).
* **Visual Progress Bars**: A thin colored bar below each drive indicator represents **used** disk space:
  * 🟢 **Green**: Under 70% full.
  * 🟡 **Orange**: 70% - 90% full.
  * 🔴 **Red**: Over 90% full.
* **File Explorer Shortcuts**: Click on any drive widget to immediately open that drive in File Explorer (`explorer.exe <DriveLetter>:\`).
* **Settings Panel**:
  * Select which connected drives to display (HDD, SSD, USB, or Network mapped drives).
  * Configure custom screen, taskbar alignment, and pixel offsets (X & Y).
  * Change disk space refresh interval (10s, 30s, 1m, 5m, 10m).
  * Choose theme mode (Auto, Dark, Light).
* **Tray Icon**: Left-click to force-refresh disk space, right-click for settings, startup, repositioning, and exit.
* **Run at Windows Startup**: Registers in the registry to run automatically when Windows starts.

---

## Technical Architecture

* **Language/Platform**: C# 10.0 / .NET 10.0 Windows Forms (WinForms).
* **Zero Designer Files**: Built entirely programmatically. No `.Designer.cs` code generation or layout editor.
* **High-DPI Aware**: All elements, fonts, borders, progress bars, and margins are scaled dynamically using Windows API `GetDpiForWindow`.
* **Zero CPU overhead**: Disk statistics are read on a background timer at a configurable low frequency (default once every 1 minute). Window repositioning and theme updates are throttled.

---

## How to Build & Run Locally

### Prerequisites
* .NET 10 SDK or higher.

### Running from source
1. Open PowerShell or command prompt in the project folder.
2. Run the application:
   ```bash
   dotnet run
   ```

### Publishing Self-Contained Builds
To compile a single-file, self-contained executable with zero external dependencies:
```bash
dotnet publish TaskbarDriveMonitor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -o "./publish"
```

---

## CI/CD Release Pipeline & Deployment

Releases are fully automated via the **GitHub Actions workflow** (`.github/workflows/release.yml`).
To publish a new release:
1. Document the release in `README.md` under the "Release History & Changelog" section.
2. Increment the `<Version>` tag in `TaskbarDriveMonitor.csproj` (e.g., `<Version>1.0.0</Version>`).
3. Create and push a git tag matching `v*` (e.g. `v1.0.0`):
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
4. The GitHub Action runner will automatically compile `win-x64` and `win-arm64` ZIP packages, generate `.sha256` integrity files, compile release notes, and create the official GitHub release.

---

## Release History & Changelog

### v1.0.0 (2026-07-09)
* Initial release of TaskbarDriveMonitor.
* Implemented programmatic WinForms overlay widget with focus prevention (`WS_EX_NOACTIVATE`).
* Added custom GDI+ drawn `DriveIndicatorControl` with dynamic progress bars (green/orange/red).
* Added settings panel with drive selection, alignment configurations, DPI scaling, and custom offset.
* Added Startup registry registration.
* Configured automated GitHub Actions release workflow.
