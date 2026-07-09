using System;
using System.Collections.Generic;

namespace TaskbarDriveMonitor.Core
{
    internal class AppSettings
    {
        public string SelectedDrives = "C"; // Comma-separated drive letters, e.g. "C,D,E"
        public int ScreenIndex = 0;
        public string ScreenDeviceName = "";
        public string Alignment = "Left"; // Default to Left out-of-the-box!
        public int OffsetX = 0; // Horizontal offset
        public int OffsetY = 0; // Vertical offset
        public int RefreshIntervalSeconds = 60;
        public string Theme = "Auto"; // "Auto", "Dark", "Light"
        public bool ShowProgressBar = true;

        private static string GetFilePath()
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.txt");
        }

        public static AppSettings Load()
        {
            var s = new AppSettings();
            string path = GetFilePath();
            if (System.IO.File.Exists(path))
            {
                try
                {
                    var lines = System.IO.File.ReadAllLines(path);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(new char[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string val = parts[1].Trim();
                            if (key == "SelectedDrives") s.SelectedDrives = val;
                            else if (key == "ScreenIndex") int.TryParse(val, out s.ScreenIndex);
                            else if (key == "ScreenDeviceName") s.ScreenDeviceName = val;
                            else if (key == "Alignment") s.Alignment = val;
                            else if (key == "OffsetX") int.TryParse(val, out s.OffsetX);
                            else if (key == "OffsetY") int.TryParse(val, out s.OffsetY);
                            else if (key == "RefreshIntervalSeconds") int.TryParse(val, out s.RefreshIntervalSeconds);
                            else if (key == "Theme") s.Theme = val;
                            else if (key == "ShowProgressBar") bool.TryParse(val, out s.ShowProgressBar);
                        }
                    }
                }
                catch { }
            }
            return s;
        }

        public void Save()
        {
            try
            {
                var lines = new List<string>
                {
                    "SelectedDrives=" + SelectedDrives,
                    "ScreenIndex=" + ScreenIndex,
                    "ScreenDeviceName=" + ScreenDeviceName,
                    "Alignment=" + Alignment,
                    "OffsetX=" + OffsetX,
                    "OffsetY=" + OffsetY,
                    "RefreshIntervalSeconds=" + RefreshIntervalSeconds,
                    "Theme=" + Theme,
                    "ShowProgressBar=" + (ShowProgressBar ? "True" : "False")
                };
                System.IO.File.WriteAllLines(GetFilePath(), lines.ToArray());
            }
            catch { }
        }
    }
}
