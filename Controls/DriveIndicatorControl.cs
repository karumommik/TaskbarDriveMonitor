using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using TaskbarDriveMonitor.Core;

namespace TaskbarDriveMonitor.Controls
{
    internal class DriveIndicatorControl : Control
    {
        private string driveLetter = ""; // e.g. "C"
        private string driveName = "";   // e.g. "C:\"
        private string volumeLabel = "";
        private long freeBytes = 0;
        private long totalBytes = 0;
        private double freePercent = 0.0;
        private double usedPercent = 0.0;
        private bool isReady = false;
        private string fileSystem = "";
        private DriveType driveType = DriveType.Fixed;

        private bool isHovered = false;
        private bool isDarkMode = true;
        private float scale = 1.0f;
        private ToolTip toolTip;

        public string DriveLetter => driveLetter;

        public DriveIndicatorControl(string letter)
        {
            this.driveLetter = letter.ToUpper().Replace(":", "").Replace("\\", "");
            this.driveName = this.driveLetter + @":\";
            
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.Cursor = Cursors.Hand;

            this.toolTip = new ToolTip();
            this.toolTip.InitialDelay = 500;
            this.toolTip.ReshowDelay = 100;
            this.toolTip.ShowAlways = true;

            UpdateDriveData();
        }

        public void SetTheme(bool isDark)
        {
            if (this.isDarkMode != isDark)
            {
                this.isDarkMode = isDark;
                Invalidate();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            this.scale = DpiHelper.GetScale(this.Handle);
            this.Height = (int)(38 * scale);
            this.Width = (int)(110 * scale);
        }

        public void UpdateDriveData()
        {
            try
            {
                var info = new DriveInfo(driveName);
                this.isReady = info.IsReady;
                this.driveType = info.DriveType;

                if (isReady)
                {
                    this.volumeLabel = info.VolumeLabel;
                    this.freeBytes = info.TotalFreeSpace;
                    this.totalBytes = info.TotalSize;
                    this.fileSystem = info.DriveFormat;

                    long usedBytes = totalBytes - freeBytes;
                    this.usedPercent = totalBytes > 0 ? (double)usedBytes / totalBytes * 100.0 : 0.0;
                    this.freePercent = totalBytes > 0 ? (double)freeBytes / totalBytes * 100.0 : 0.0;

                    double freeGB = freeBytes / (1024.0 * 1024.0 * 1024.0);
                    double totalGB = totalBytes / (1024.0 * 1024.0 * 1024.0);
                    string tipText = $"Drive: {volumeLabel} ({driveName})\n" +
                                     $"Type: {GetDriveTypeFriendlyName(driveType)}\n" +
                                     $"File System: {fileSystem}\n" +
                                     $"Free Space: {freePercent:F1}% ({freeGB:F1} GB / {totalGB:F1} GB)\n" +
                                     $"Click to open in Explorer";
                    
                    if (this.toolTip.GetToolTip(this) != tipText)
                    {
                        this.toolTip.SetToolTip(this, tipText);
                    }
                }
                else
                {
                    this.toolTip.SetToolTip(this, $"Drive {driveName} (Not Ready)");
                }
            }
            catch (Exception ex)
            {
                this.isReady = false;
                this.toolTip.SetToolTip(this, $"Drive {driveName} Error: {ex.Message}");
            }
            Invalidate();
        }

        private string GetDriveTypeFriendlyName(DriveType type)
        {
            return type switch
            {
                DriveType.Fixed => "Local Disk (SSD/HDD)",
                DriveType.Removable => "External Drive (USB)",
                DriveType.Network => "Network Drive",
                DriveType.CDRom => "CD-ROM",
                DriveType.Ram => "RAM Disk",
                _ => "Unknown"
            };
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = driveName,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Draw Background
            Color bg;
            if (isHovered)
            {
                bg = isDarkMode ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(20, 0, 0, 0);
            }
            else
            {
                bg = Color.Transparent;
            }

            using (var brush = new SolidBrush(bg))
            {
                g.FillRoundRectangle(brush, 0, 0, Width, Height, 4 * scale);
            }

            // 2. Setup Fonts and Colors
            Color textMainColor = isDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(30, 30, 30);
            Color textSubColor = isDarkMode ? Color.FromArgb(170, 170, 170) : Color.FromArgb(100, 100, 100);

            using (var fontMain = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fontSub = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (var brushMain = new SolidBrush(textMainColor))
            using (var brushSub = new SolidBrush(textSubColor))
            {
                if (isReady)
                {
                    // Line 1: e.g. "C: 45.2% free"
                    string line1 = $"{driveLetter}: {freePercent:F1}% free";
                    g.DrawString(line1, fontMain, brushMain, 6 * scale, 3 * scale);

                    // Line 2: e.g. "120.5 / 256 GB"
                    double freeGB = freeBytes / (1024.0 * 1024.0 * 1024.0);
                    double totalGB = totalBytes / (1024.0 * 1024.0 * 1024.0);
                    string line2 = $"{freeGB:F1} / {totalGB:F0} GB";
                    g.DrawString(line2, fontSub, brushSub, 6 * scale, 17 * scale);

                    // 3. Draw Progress Bar
                    // Background track of progress bar
                    Color barTrackColor = isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(210, 210, 210);
                    float barX = 6 * scale;
                    float barY = 31 * scale;
                    float barWidth = Width - (12 * scale);
                    float barHeight = 3 * scale;

                    using (var trackBrush = new SolidBrush(barTrackColor))
                    {
                        g.FillRoundRectangle(trackBrush, barX, barY, barWidth, barHeight, 1.5f * scale);
                    }

                    // Progress Fill (represents USED space)
                    if (usedPercent > 0)
                    {
                        Color fillColor;
                        if (usedPercent < 70)
                        {
                            fillColor = Color.FromArgb(40, 167, 69); // Green
                        }
                        else if (usedPercent < 90)
                        {
                            fillColor = Color.FromArgb(255, 193, 7); // Orange
                        }
                        else
                        {
                            fillColor = Color.FromArgb(220, 53, 69); // Red
                        }

                        float fillWidth = (float)(barWidth * (usedPercent / 100.0));
                        if (fillWidth < 2 * scale) fillWidth = 2 * scale; // Minimal fill visibility

                        using (var fillBrush = new SolidBrush(fillColor))
                        {
                            g.FillRoundRectangle(fillBrush, barX, barY, fillWidth, barHeight, 1.5f * scale);
                        }
                    }
                }
                else
                {
                    // Draw drive not ready state
                    g.DrawString($"{driveLetter}:", fontMain, brushMain, 6 * scale, 3 * scale);
                    g.DrawString("Not Ready", fontSub, brushSub, 6 * scale, 17 * scale);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                toolTip?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
