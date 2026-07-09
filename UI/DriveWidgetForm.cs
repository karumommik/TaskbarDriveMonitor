using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using TaskbarDriveMonitor.Core;
using TaskbarDriveMonitor.Native;
using TaskbarDriveMonitor.Controls;

namespace TaskbarDriveMonitor.UI
{
    public class DriveWidgetForm : Form
    {
        private AppSettings settings;
        private List<DriveIndicatorControl> driveControls = new List<DriveIndicatorControl>();
        
        private System.Windows.Forms.Timer fastTimer = null!;
        private System.Windows.Forms.Timer diskRefreshTimer = null!;
        
        private NotifyIcon notifyIcon = null!;
        private ToolTip toolTip = null!;

        private int calculatedWidth = 120;
        private bool isDarkMode = true;
        private int themeCheckCounter = 0;
        private int gcCounter = 0;

        private Color themeBgColor;
        private Color themeBorderColor;

        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "TaskbarDriveMonitor";

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // WS_EX_NOACTIVATE = 0x08000000
                // WS_EX_TOPMOST = 0x00000008
                // WS_EX_TOOLWINDOW = 0x00000080
                cp.ExStyle |= 0x08000000 | 0x00000008 | 0x00000080;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation => true;

        private void Log(string msg)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log.txt");
                File.AppendAllText(logPath, $"[Widget] {msg}\r\n");
            }
            catch { }
        }

        public DriveWidgetForm()
        {
            Log("Constructor start");

            // Load settings
            settings = AppSettings.Load();
            Log("Settings loaded");

            // Set up form properties
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.Fuchsia;
            this.TransparencyKey = Color.Fuchsia;
            this.DoubleBuffered = true;
            Log("Form properties set");

            // Tooltip
            toolTip = new ToolTip();
            toolTip.InitialDelay = 500;
            toolTip.ReshowDelay = 100;
            Log("Tooltip created");

            // Initialize System Tray Icon
            Log("Initializing Tray Icon...");
            InitializeTrayIcon();
            Log("Tray Icon initialized");

            // Setup Theme Colors & Create Controls
            Log("Updating theme colors...");
            UpdateThemeColors();
            Log("Theme colors updated");

            Log("Recreating drive controls...");
            RecreateDriveControls();
            Log("Drive controls recreated");

            // Fast Timer (100ms) for positioning & theme polling
            fastTimer = new System.Windows.Forms.Timer { Interval = 100 };
            fastTimer.Tick += FastTimer_Tick;
            fastTimer.Start();
            Log("Fast timer started");

            // Disk Space Refresh Timer
            diskRefreshTimer = new System.Windows.Forms.Timer { Interval = settings.RefreshIntervalSeconds * 1000 };
            diskRefreshTimer.Tick += DiskRefreshTimer_Tick;
            diskRefreshTimer.Start();
            Log("Disk refresh timer started");

            // Trigger initial layouts
            Log("Updating layout...");
            UpdateLayout();
            Log("Layout updated");

            Log("Updating position...");
            UpdatePosition();
            Log("Position updated");

            this.FormClosing += (s, e) => {
                notifyIcon.Visible = false;
            };
            Log("Constructor end");
        }

        private void InitializeTrayIcon()
        {
            notifyIcon = new NotifyIcon();
            try
            {
                Log("Creating tray icon bitmap");
                // Create a dynamic nice harddrive-like icon
                Bitmap bmp = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.FillRectangle(Brushes.Gray, 2, 2, 12, 12);
                    g.FillEllipse(Brushes.LightGray, 4, 4, 3, 3);
                    g.FillEllipse(Brushes.LightGray, 9, 4, 3, 3);
                    using (Pen p = new Pen(Color.White, 1))
                    {
                        g.DrawRectangle(p, 2, 2, 12, 12);
                        g.DrawLine(p, 2, 10, 14, 10);
                    }
                }
                notifyIcon.Icon = Icon.FromHandle(bmp.GetHicon());
            }
            catch (Exception ex)
            {
                Log($"Tray icon bitmap error: {ex.Message}");
                notifyIcon.Icon = SystemIcons.Application;
            }

            notifyIcon.Text = "Taskbar Drive Monitor";
            notifyIcon.Visible = true;

            // Context Menu
            Log("Creating tray context menu");
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Renderer = new ModernToolStripRenderer(isDarkMode);

            ToolStripMenuItem titleItem = new ToolStripMenuItem("Taskbar Drive Monitor") { Enabled = false };
            contextMenu.Items.Add(titleItem);

            ToolStripMenuItem startupItem = new ToolStripMenuItem("Run at Windows Startup", null, (s, e) => {
                var mi = (ToolStripMenuItem)s!;
                mi.Checked = !mi.Checked;
                SetStartup(mi.Checked);
            });
            startupItem.Checked = IsStartupEnabled();
            contextMenu.Items.Add(startupItem);

            ToolStripMenuItem refreshItem = new ToolStripMenuItem("Refresh Disk Info", null, (s, e) => RefreshDriveDataNow());
            contextMenu.Items.Add(refreshItem);

            ToolStripMenuItem settingsItem = new ToolStripMenuItem("Settings", null, (s, e) => this.BeginInvoke(new Action(ShowSettings)));
            contextMenu.Items.Add(settingsItem);

            ToolStripMenuItem repositionItem = new ToolStripMenuItem("Reposition Widget", null, (s, e) => UpdatePosition());
            contextMenu.Items.Add(repositionItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, (s, e) => {
                notifyIcon.Visible = false;
                Application.Exit();
            });
            contextMenu.Items.Add(exitItem);

            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += (s, e) => RefreshDriveDataNow();
        }

        private void RecreateDriveControls()
        {
            // Clear existing controls
            foreach (var ctrl in driveControls)
            {
                this.Controls.Remove(ctrl);
                ctrl.Dispose();
            }
            driveControls.Clear();

            // Load drives list
            string[] letters = settings.SelectedDrives.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (letters.Length == 0)
            {
                letters = new string[] { "C" };
            }

            Log($"Loading controls for drives: {string.Join(", ", letters)}");
            float scale = DpiHelper.GetScale(this.Handle);
            Log($"Scale factor: {scale}");
            foreach (var letter in letters)
            {
                Log($"Creating control for drive: {letter}");
                var ctrl = new DriveIndicatorControl(letter);
                ctrl.SetTheme(isDarkMode);
                ctrl.BackColor = themeBgColor; // Explicitly set solid BackColor to resolve purple outline
                this.Controls.Add(ctrl);
                driveControls.Add(ctrl);
                Log($"Control for drive {letter} added");
            }

            Log("Updating layout in RecreateDriveControls");
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            float scale = DpiHelper.GetScale(this.Handle);
            int margin = (int)(4 * scale);
            int currentX = margin;
            int h = (int)(38 * scale);

            foreach (var ctrl in driveControls)
            {
                ctrl.Height = h;
                ctrl.Width = (int)(110 * scale);
                ctrl.Location = new Point(currentX, 0);
                currentX += ctrl.Width + margin;
            }

            calculatedWidth = currentX;
            this.Height = h;
            this.Width = calculatedWidth;

            Log($"Layout updated. calculatedWidth: {calculatedWidth}, Height: {h}");
            UpdatePosition();
        }

        private int GetLeftAlignmentOffset(Screen scr, float scale)
        {
            if (!scr.Primary)
            {
                return (int)(12 * scale);
            }

            // Default fallback for Windows 10 or left-aligned Windows 11
            int baseOffset = (int)(84 * scale);

            try
            {
                // Check if we are on Windows 11 (build >= 22000)
                var os = Environment.OSVersion;
                if (os.Platform == PlatformID.Win32NT && os.Version.Build >= 22000)
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                    {
                        if (key != null)
                        {
                            var taskbarAl = key.GetValue("TaskbarAl"); // 0 = Left, 1 = Center
                            var taskbarDa = key.GetValue("TaskbarDa"); // 0 = Hidden, 1 = Visible (Widgets)

                            int alignment = (taskbarAl is int al) ? al : 1; // Default to Center on Win11
                            int showWidgets = (taskbarDa is int da) ? da : 1; // Default to Visible on Win11

                            if (alignment == 1 && showWidgets == 1)
                            {
                                // Windows 11 Widgets button (Weather info) on the left side of the taskbar
                                // The Widgets/Weather button width is typically around 140-160px on high DPI,
                                // so we start after it with a buffer. Let's make it 180px.
                                return (int)(180 * scale);
                            }
                        }
                    }
                }
            }
            catch { }

            return baseOffset;
        }

        private void UpdatePosition()
        {
            float scale = DpiHelper.GetScale(this.Handle);

            // Keep form visible and not minimized
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            if (!this.Visible)
            {
                this.Visible = true;
            }

            // Find screen
            Screen? scr = null;
            if (!string.IsNullOrEmpty(settings.ScreenDeviceName))
            {
                foreach (var s in Screen.AllScreens)
                {
                    if (s.DeviceName == settings.ScreenDeviceName)
                    {
                        scr = s;
                        break;
                    }
                }
            }
            if (scr == null)
            {
                // Default to primary screen first for out-of-the-box experience
                foreach (var s in Screen.AllScreens)
                {
                    if (s.Primary)
                    {
                        scr = s;
                        break;
                    }
                }
            }
            if (scr == null)
            {
                int scrIdx = Math.Max(0, Math.Min(Screen.AllScreens.Length - 1, settings.ScreenIndex));
                scr = Screen.AllScreens[scrIdx];
            }

            var bounds = scr.Bounds;
            int taskbarTop = scr.WorkingArea.Bottom;
            int taskbarHeight = scr.Bounds.Bottom - scr.WorkingArea.Bottom;

            int collapsedHeight = (int)(38 * scale);
            int targetTop = taskbarTop + (taskbarHeight - collapsedHeight) / 2;

            int targetLeft = 0;
            string alignment = settings.Alignment;

            if (alignment == "Custom")
            {
                targetLeft = settings.OffsetX;
                targetTop = settings.OffsetY;
            }
            else if (alignment == "Left")
            {
                targetLeft = bounds.Left + GetLeftAlignmentOffset(scr, scale) + settings.OffsetX;
                targetTop += settings.OffsetY;
            }
            else if (alignment == "Center")
            {
                targetLeft = bounds.Left + (bounds.Width - calculatedWidth) / 2 + settings.OffsetX;
                targetTop += settings.OffsetY;
            }
            else // "Right" (Default)
            {
                IntPtr trayHwnd = Win32.FindWindow("Shell_TrayWnd", null);
                if (scr.Primary && trayHwnd != IntPtr.Zero)
                {
                    IntPtr notifyHwnd = Win32.FindWindowEx(trayHwnd, IntPtr.Zero, "TrayNotifyWnd", null);
                    Win32.RECT rectTray;
                    if (notifyHwnd != IntPtr.Zero && Win32.GetWindowRect(notifyHwnd, out rectTray))
                    {
                        // Position next to tray notification area, leaving a 48px safety margin
                        targetLeft = (int)(rectTray.Left / scale) - calculatedWidth - (int)(48 * scale) + settings.OffsetX;
                    }
                    else
                    {
                        targetLeft = bounds.Right - calculatedWidth - (int)(200 * scale) + settings.OffsetX;
                    }
                }
                else
                {
                    targetLeft = bounds.Right - calculatedWidth - (int)(16 * scale) + settings.OffsetX;
                }
                targetTop += settings.OffsetY;
            }

            // Update bounds
            if (this.Left != targetLeft || this.Top != targetTop || this.Width != calculatedWidth)
            {
                this.Bounds = new Rectangle(targetLeft, targetTop, calculatedWidth, this.Height);
            }

            // Topmost Z-order
            Win32.SetWindowPos(this.Handle, Win32.HWND_TOPMOST, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE | Win32.SWP_SHOWWINDOW);
        }

        private void RefreshDriveDataNow()
        {
            foreach (var ctrl in driveControls)
            {
                ctrl.UpdateDriveData();
            }
        }

        private void FastTimer_Tick(object? sender, EventArgs e)
        {
            // 1. Poll theme change (every 2 seconds / 20 ticks)
            themeCheckCounter++;
            if (themeCheckCounter >= 20)
            {
                themeCheckCounter = 0;
                UpdateThemeColors();
            }

            // 2. Refresh positioning
            UpdatePosition();

            // 3. GC check
            gcCounter++;
            if (gcCounter >= 600)
            {
                gcCounter = 0;
                GC.Collect();
            }
        }

        private void DiskRefreshTimer_Tick(object? sender, EventArgs e)
        {
            RefreshDriveDataNow();
        }

        private void UpdateThemeColors()
        {
            bool systemDarkMode = true;
            if (settings.Theme == "Auto")
            {
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        if (key != null)
                        {
                            var value = key.GetValue("AppsUseLightTheme");
                            if (value != null)
                            {
                                systemDarkMode = (int)value == 0;
                            }
                        }
                    }
                }
                catch { }
            }
            else
            {
                systemDarkMode = (settings.Theme == "Dark");
            }

            this.isDarkMode = systemDarkMode;

            if (isDarkMode)
            {
                themeBgColor = Color.FromArgb(28, 28, 28);
                themeBorderColor = Color.FromArgb(55, 55, 55);
            }
            else
            {
                themeBgColor = Color.FromArgb(243, 243, 243);
                themeBorderColor = Color.FromArgb(210, 210, 210);
            }

            foreach (var ctrl in driveControls)
            {
                ctrl.SetTheme(isDarkMode);
                ctrl.BackColor = themeBgColor; // Explicitly set solid BackColor to resolve purple outline
            }
            if (notifyIcon.ContextMenuStrip != null)
            {
                notifyIcon.ContextMenuStrip.Renderer = new ModernToolStripRenderer(isDarkMode);
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;

            float scale = DpiHelper.GetScale(this.Handle);
            int r = (int)(6 * scale);

            using (var brush = new SolidBrush(themeBgColor))
            {
                g.FillRoundRectangle(brush, 0, 0, Width - 1, Height - 1, r);
            }

            using (var pen = new Pen(themeBorderColor, 1f))
            {
                g.DrawRoundRectangle(pen, 0, 0, Width - 1, Height - 1, r);
            }
        }

        private void ShowSettings()
        {
            foreach (Form openForm in Application.OpenForms)
            {
                if (openForm is SettingsForm)
                {
                    openForm.BringToFront();
                    return;
                }
            }

            var form = new SettingsForm(settings, isDarkMode);
            if (form.ShowDialog() == DialogResult.OK)
            {
                // Reload settings and update drive list
                this.settings = AppSettings.Load();
                
                // Update disk timer interval
                diskRefreshTimer.Interval = Math.Max(5, settings.RefreshIntervalSeconds) * 1000;

                RecreateDriveControls();
                UpdateThemeColors();
                UpdateLayout();
                UpdatePosition();
                RefreshDriveDataNow();
            }
        }

        private bool IsStartupEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey))
                {
                    return key != null && key.GetValue(AppName) != null;
                }
            }
            catch { return false; }
        }

        private void SetStartup(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                            key.SetValue(AppName, "\"" + Application.ExecutablePath + "\"");
                        else
                            key.DeleteValue(AppName, false);
                    }
                }
            }
            catch { }
        }
    }
}
