using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TaskbarDriveMonitor.Core;
using TaskbarDriveMonitor.Native;

namespace TaskbarDriveMonitor.UI
{
    internal class SettingsForm : Form
    {
        private AppSettings settings;
        private bool isDarkMode;
        private float scale;

        // UI Controls
        private Panel pnlDrives;
        private ComboBox cmbScreen;
        private ComboBox cmbAlignment;
        private TextBox txtOffsetX;
        private TextBox txtOffsetY;
        private ComboBox cmbRefresh;
        private ComboBox cmbTheme;
        private Button btnSave;
        private Button btnCancel;

        private class DriveSettingRow
        {
            public CheckBox CheckBox { get; set; } = null!;
            public string Letter { get; set; } = string.Empty;
        }
        private List<DriveSettingRow> driveRows = new List<DriveSettingRow>();

        public SettingsForm(AppSettings settings, bool isDarkMode)
        {
            this.settings = settings;
            this.isDarkMode = isDarkMode;

            // DPI scale
            this.scale = DpiHelper.GetScale(this.Handle);

            // Window properties
            this.Text = "Settings - Taskbar Drive Monitor";
            this.Size = new Size((int)(380 * scale), (int)(550 * scale));
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;

            Color bgColor = isDarkMode ? Color.FromArgb(32, 32, 32) : Color.FromArgb(240, 240, 240);
            Color textColor = isDarkMode ? Color.FromArgb(235, 235, 235) : Color.FromArgb(40, 40, 40);
            Color controlBg = isDarkMode ? Color.FromArgb(45, 45, 45) : Color.White;
            Color btnBg = isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(225, 225, 225);

            this.BackColor = bgColor;
            this.ForeColor = textColor;

            // 1. Title Label for Drives
            Label lblDrives = new Label
            {
                Text = "Select drives to display:",
                Location = new Point((int)(20 * scale), (int)(15 * scale)),
                Size = new Size((int)(340 * scale), (int)(20 * scale)),
                Font = new Font("Segoe UI", 9f * scale, FontStyle.Bold)
            };
            this.Controls.Add(lblDrives);

            // 2. Drives List Panel
            pnlDrives = new Panel
            {
                Location = new Point((int)(20 * scale), (int)(38 * scale)),
                Size = new Size((int)(325 * scale), (int)(120 * scale)),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = controlBg,
                AutoScroll = true
            };
            this.Controls.Add(pnlDrives);

            PopulateDrives(textColor);

            // 3. Screen selection Label & Combo
            Label lblScreen = new Label
            {
                Text = "Display monitor:",
                Location = new Point((int)(20 * scale), (int)(170 * scale)),
                Size = new Size((int)(325 * scale), (int)(20 * scale)),
                Font = new Font("Segoe UI", 9f * scale, FontStyle.Bold)
            };
            this.Controls.Add(lblScreen);

            cmbScreen = new ComboBox
            {
                Location = new Point((int)(20 * scale), (int)(190 * scale)),
                Size = new Size((int)(325 * scale), (int)(24 * scale)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = controlBg,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f * scale)
            };
            this.Controls.Add(cmbScreen);

            var screens = Screen.AllScreens;
            int selectedScrIdx = 0;
            for (int i = 0; i < screens.Length; i++)
            {
                string suffix = screens[i].Primary ? " (Primary)" : "";
                cmbScreen.Items.Add($"Screen {i + 1}{suffix} - {screens[i].Bounds.Width}x{screens[i].Bounds.Height}");
                if (!string.IsNullOrEmpty(settings.ScreenDeviceName) && screens[i].DeviceName == settings.ScreenDeviceName)
                {
                    selectedScrIdx = i;
                }
            }
            if (selectedScrIdx == 0 && settings.ScreenIndex >= 0 && settings.ScreenIndex < screens.Length)
            {
                selectedScrIdx = settings.ScreenIndex;
            }
            cmbScreen.SelectedIndex = selectedScrIdx;

            // 4. Alignment Label & Combo
            Label lblAlignment = new Label
            {
                Text = "Taskbar alignment:",
                Location = new Point((int)(20 * scale), (int)(230 * scale)),
                Size = new Size((int)(325 * scale), (int)(20 * scale)),
                Font = new Font("Segoe UI", 9f * scale, FontStyle.Bold)
            };
            this.Controls.Add(lblAlignment);

            cmbAlignment = new ComboBox
            {
                Location = new Point((int)(20 * scale), (int)(250 * scale)),
                Size = new Size((int)(325 * scale), (int)(24 * scale)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = controlBg,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f * scale)
            };
            cmbAlignment.Items.Add("Next to clock (Right)");
            cmbAlignment.Items.Add("Next to Start button (Left)");
            cmbAlignment.Items.Add("Center of taskbar (Center)");
            cmbAlignment.Items.Add("Manual coordinates (Custom)");
            this.Controls.Add(cmbAlignment);

            if (settings.Alignment == "Left") cmbAlignment.SelectedIndex = 1;
            else if (settings.Alignment == "Center") cmbAlignment.SelectedIndex = 2;
            else if (settings.Alignment == "Custom") cmbAlignment.SelectedIndex = 3;
            else cmbAlignment.SelectedIndex = 0; // Default Right

            // 5. Offsets Label & Fields
            Label lblOffsets = new Label
            {
                Text = "Pixel offsets (X / Y):",
                Location = new Point((int)(20 * scale), (int)(290 * scale)),
                Size = new Size((int)(325 * scale), (int)(20 * scale)),
                Font = new Font("Segoe UI", 9f * scale, FontStyle.Bold)
            };
            this.Controls.Add(lblOffsets);

            Label lblX = new Label
            {
                Text = "Offset X:",
                Location = new Point((int)(20 * scale), (int)(313 * scale)),
                Size = new Size((int)(70 * scale), (int)(20 * scale)),
                Font = new Font("Segoe UI", 8.5f * scale)
            };
            this.Controls.Add(lblX);

            txtOffsetX = new TextBox
            {
                Location = new Point((int)(90 * scale), (int)(310 * scale)),
                Size = new Size((int)(70 * scale), (int)(20 * scale)),
                Text = settings.OffsetX.ToString(),
                BackColor = controlBg,
                ForeColor = textColor,
                BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D,
                Font = new Font("Segoe UI", 8.5f * scale)
            };
            this.Controls.Add(txtOffsetX);

            Label lblY = new Label
            {
                Text = "Offset Y:",
                Location = new Point((int)(180 * scale), (int)(313 * scale)),
                Size = new Size((int)(70 * scale), (int)(20 * scale)),
                Font = new Font("Segoe UI", 8.5f * scale)
            };
            this.Controls.Add(lblY);

            txtOffsetY = new TextBox
            {
                Location = new Point((int)(250 * scale), (int)(310 * scale)),
                Size = new Size((int)(75 * scale), (int)(20 * scale)),
                Text = settings.OffsetY.ToString(),
                BackColor = controlBg,
                ForeColor = textColor,
                BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D,
                Font = new Font("Segoe UI", 8.5f * scale)
            };
            this.Controls.Add(txtOffsetY);

            // 6. Refresh Interval Label & Combo
            Label lblRefresh = new Label
            {
                Text = "Disk space refresh interval:",
                Location = new Point((int)(20 * scale), (int)(350 * scale)),
                Size = new Size((int)(325 * scale), (int)(20 * scale)),
                Font = new Font("Segoe UI", 9f * scale, FontStyle.Bold)
            };
            this.Controls.Add(lblRefresh);

            cmbRefresh = new ComboBox
            {
                Location = new Point((int)(20 * scale), (int)(370 * scale)),
                Size = new Size((int)(325 * scale), (int)(24 * scale)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = controlBg,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f * scale)
            };
            cmbRefresh.Items.Add("10 seconds");
            cmbRefresh.Items.Add("30 seconds");
            cmbRefresh.Items.Add("1 minute (default)");
            cmbRefresh.Items.Add("5 minutes");
            cmbRefresh.Items.Add("10 minutes");
            this.Controls.Add(cmbRefresh);

            if (settings.RefreshIntervalSeconds == 10) cmbRefresh.SelectedIndex = 0;
            else if (settings.RefreshIntervalSeconds == 30) cmbRefresh.SelectedIndex = 1;
            else if (settings.RefreshIntervalSeconds == 300) cmbRefresh.SelectedIndex = 3;
            else if (settings.RefreshIntervalSeconds == 600) cmbRefresh.SelectedIndex = 4;
            else cmbRefresh.SelectedIndex = 2; // 60s

            // 7. Theme Label & Combo
            Label lblTheme = new Label
            {
                Text = "Theme:",
                Location = new Point((int)(20 * scale), (int)(410 * scale)),
                Size = new Size((int)(325 * scale), (int)(20 * scale)),
                Font = new Font("Segoe UI", 9f * scale, FontStyle.Bold)
            };
            this.Controls.Add(lblTheme);

            cmbTheme = new ComboBox
            {
                Location = new Point((int)(20 * scale), (int)(430 * scale)),
                Size = new Size((int)(325 * scale), (int)(24 * scale)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = controlBg,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f * scale)
            };
            cmbTheme.Items.Add("Follow Windows (Auto)");
            cmbTheme.Items.Add("Dark");
            cmbTheme.Items.Add("Light");
            this.Controls.Add(cmbTheme);

            if (settings.Theme == "Dark") cmbTheme.SelectedIndex = 1;
            else if (settings.Theme == "Light") cmbTheme.SelectedIndex = 2;
            else cmbTheme.SelectedIndex = 0;

            // 8. Save button
            btnSave = new Button
            {
                Text = "Save",
                Location = new Point((int)(155 * scale), (int)(475 * scale)),
                Size = new Size((int)(90 * scale), (int)(30 * scale)),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f * scale, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            // 9. Cancel button
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point((int)(255 * scale), (int)(475 * scale)),
                Size = new Size((int)(90 * scale), (int)(30 * scale)),
                BackColor = btnBg,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f * scale)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancel);

            // Set up tooltips
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(cmbAlignment, "Select where the widget aligns on the taskbar. In Custom mode, use absolute coordinates.");
            toolTip.SetToolTip(txtOffsetX, "Horizontal offset in pixels. In Custom mode, this sets the absolute X coordinate.");
            toolTip.SetToolTip(txtOffsetY, "Vertical offset in pixels. In Custom mode, this sets the absolute Y coordinate.");
        }

        private void PopulateDrives(Color textColor)
        {
            var selectedList = new List<string>(settings.SelectedDrives.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            
            try
            {
                var drives = DriveInfo.GetDrives();
                int y = (int)(5 * scale);
                int rowHeight = (int)(28 * scale);

                foreach (var drive in drives)
                {
                    string letter = drive.Name.Replace(":\\", "").ToUpper();
                    string friendlyName = $"{letter}: [";
                    if (drive.IsReady)
                    {
                        friendlyName += string.IsNullOrEmpty(drive.VolumeLabel) ? "Local Disk" : drive.VolumeLabel;
                        friendlyName += $", {drive.DriveFormat}]";
                    }
                    else
                    {
                        friendlyName += "Not Ready]";
                    }

                    CheckBox cb = new CheckBox
                    {
                        Text = friendlyName,
                        Location = new Point((int)(10 * scale), y),
                        Size = new Size((int)(280 * scale), (int)(24 * scale)),
                        Checked = selectedList.Contains(letter),
                        ForeColor = textColor,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 9f * scale)
                    };

                    pnlDrives.Controls.Add(cb);
                    driveRows.Add(new DriveSettingRow { CheckBox = cb, Letter = letter });
                    y += rowHeight;
                }
            }
            catch { }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // 1. Gather selected drives
            var selected = new List<string>();
            foreach (var row in driveRows)
            {
                if (row.CheckBox.Checked)
                {
                    selected.Add(row.Letter);
                }
            }

            if (selected.Count == 0)
            {
                MessageBox.Show("Please select at least one drive to display!", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            settings.SelectedDrives = string.Join(",", selected);

            // 2. Alignment
            settings.Alignment = cmbAlignment.SelectedIndex switch
            {
                1 => "Left",
                2 => "Center",
                3 => "Custom",
                _ => "Right"
            };

            // 3. Offsets
            int.TryParse(txtOffsetX.Text, out int ox);
            int.TryParse(txtOffsetY.Text, out int oy);
            settings.OffsetX = ox;
            settings.OffsetY = oy;

            // 4. Refresh interval
            settings.RefreshIntervalSeconds = cmbRefresh.SelectedIndex switch
            {
                0 => 10,
                1 => 30,
                3 => 300,
                4 => 600,
                _ => 60
            };

            // 5. Theme
            settings.Theme = cmbTheme.SelectedIndex switch
            {
                1 => "Dark",
                2 => "Light",
                _ => "Auto"
            };

            // 6. Screen selection
            if (cmbScreen.SelectedIndex >= 0 && cmbScreen.SelectedIndex < Screen.AllScreens.Length)
            {
                settings.ScreenIndex = cmbScreen.SelectedIndex;
                settings.ScreenDeviceName = Screen.AllScreens[cmbScreen.SelectedIndex].DeviceName;
            }

            // Save settings to file
            settings.Save();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
