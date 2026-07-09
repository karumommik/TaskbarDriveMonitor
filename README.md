# Taskbar Drive Monitor (v1.0.2)

A lightweight, stable, and convenient Windows 11 utility that automatically places itself on the taskbar, displaying selected drives' free capacity (both as percentages and in gigabytes) along with dynamic, color-coded progress bars.

---

## 1. Key Features

1. **Taskbar Capacity Widget:** A borderless, clean layout sitting directly next to your system tray icons showing drive letters, free space percentage, and free/total space (e.g., `C: 45.2% free` and `120.5 / 256 GB`).
2. **Visual Progress Bars:** Dynamic, GDI+ custom-drawn color-coded bars indicating **used** space under each drive:
   * 🟢 **Green:** Under 70% full.
   * 🟡 **Orange:** 70% - 90% full.
   * 🔴 **Red:** Over 90% full.
3. **File Explorer Integration:** Left-clicking directly on any drive widget instantly opens that drive in File Explorer using high-performance shell execution.
4. **Customizable Drive Filtering:** Select exactly which connected drives to display (SSD, HDD, USB drives, or mapped network drives) via the settings interface.
5. **DPI Awareness:** Scales dynamically on startup to remain sharp and aligned on multi-monitor setups with different scaling levels (e.g., 100%, 125%, 150%, 200%).
6. **System Tray Integration:** Left-clicking the tray icon forces an instant disk capacity refresh; right-clicking opens a configuration panel for drives selection, offsets, startup, and exit.

---

## 2. Installation & Quick Start

### Option 1: Standalone Portable Release (Recommended)
1. Go to the [Releases](https://github.com/karumommik/TaskbarDriveMonitor/releases) page.
2. Download the latest `TaskbarDriveMonitor-win-x64.zip` (or `win-arm64` if on an ARM device).
3. Extract the `TaskbarDriveMonitor.exe` executable to a permanent folder on your computer (e.g., `C:\Program Files\TaskbarDriveMonitor` or a dedicated folder in your User directory).
4. Double-click the file to run the utility.
5. **Auto-start with Windows:** Right-click the drive icon in the system tray and select **"Run at Windows Startup"**.

### Option 2: Microsoft Store Installation
1. Search for **Taskbar Drive Monitor** in the Microsoft Store (or use the Store deep link once published).
2. Install the application natively. Windows Store will handle automatic updates.

---

## 3. Known Behaviors & Quirks

To ensure 24/7 stability and prevent being flagged by antivirus software, this utility runs as a lightweight, borderless Win32 overlay window rather than hooking deep into the Windows Explorer (`explorer.exe`) process memory. As a result, you might notice:
* **Brief Vanishing/Reappearing:** When minimizing windows, pressing `Win+D` (Show Desktop), or entering fullscreen mode, the utility may briefly disappear for a fraction of a second. It automatically repositions itself back into place within 500ms.
* **Focus Safety:** Clicking on the drive capacity widgets does not steal focus from your active windows, meaning you will not get tabbed out of games or active apps.

---

## 4. Release History & Changelog

### v1.0.2 (2026-07-09)
* **Microsoft Store MSIX Support:** Integrated package manifest and asset requirements. The build pipeline now outputs a single `.msixbundle` supporting both `x64` and `ARM64` for Microsoft Store deployment.
* **Portable Builds Preserved:** The standalone portable `.zip` releases continue to be built and published identically to previous versions.

### v1.0.1 (2026-07-09)
* Optimized File Explorer opening speed by using ShellExecute instead of spawning a new `explorer.exe` process from disk.

### v1.0.0 (2026-07-09)
* Initial release of TaskbarDriveMonitor.
* Implemented programmatic WinForms overlay widget with focus prevention (`WS_EX_NOACTIVATE`).
* Added custom GDI+ drawn `DriveIndicatorControl` with dynamic progress bars (green/orange/red).
* Added settings panel with drive selection, alignment configurations, DPI scaling, and custom offset.
* Added Startup registry registration.
* Configured automated GitHub Actions release workflow.

---

## 5. How to Build from Source (Advanced)

### Prerequisites
* .NET 10.0 SDK or higher.

### Running from Source
1. Open PowerShell in the project directory.
2. Run the application:
   ```bash
   dotnet run
   ```

### Publishing Standalone Portable Builds
To compile a single-file, self-contained executable with zero external dependencies (no .NET runtime installation required by the user):
```bash
dotnet publish TaskbarDriveMonitor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -o "./publish"
```

---

## 6. Technical Architecture

* **Language/Platform:** C# 10.0 / .NET 10.0 Windows Forms (WinForms).
* **Zero Designer Files:** Built entirely programmatically. No `.Designer.cs` code generation or layout editor. All controls and margins are constructed on the fly to maximize stability.
* **Explorer Shell Integration:** Optimizes File Explorer opening speeds using native `ShellExecute` methods instead of launching heavier `explorer.exe` disk processes.
* **Low-Overhead Drive Polling:** Disk statistics are read on a background thread using a low-frequency timer (default once every 1 minute) to maintain 0% CPU overhead during normal use.
* **High-DPI Scaling:** All layouts, borders, margins, fonts, and controls dynamically adjust by querying Windows API DPI scaling factors per monitor.

---

## 7. CI/CD Release Pipeline & Deployment

Releases are fully automated via the **GitHub Actions workflow** (`.github/workflows/release.yml`).
To publish a new release:
1. Increment the version inside `TaskbarDriveMonitor.csproj` (e.g., `<Version>1.0.2</Version>`).
2. Document the release in `README.md` under the "Release History & Changelog" section.
3. Create and push a git tag matching `v*` (e.g. `v1.0.2`):
   ```bash
   git tag v1.0.2
   git push origin v1.0.2
   ```
4. The GitHub Action runner will automatically compile `win-x64` and `win-arm64` ZIP packages, generate `.sha256` integrity files, build the Microsoft Store `.msixbundle`, write release notes, and create the official GitHub release.
