using System;
using System.Windows.Forms;
using TaskbarDriveMonitor.Native;
using TaskbarDriveMonitor.UI;

namespace TaskbarDriveMonitor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            string dir = TaskbarDriveMonitor.Native.Win32.IsPackaged() 
                ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskbarDriveMonitor") 
                : AppDomain.CurrentDomain.BaseDirectory;
            if (TaskbarDriveMonitor.Native.Win32.IsPackaged()) System.IO.Directory.CreateDirectory(dir);
            
            string logPath = System.IO.Path.Combine(dir, "debug_log.txt");
            string crashPath = System.IO.Path.Combine(dir, "crash.log");
            
            try 
            {
                System.IO.File.WriteAllText(logPath, "Starting Main...\r\n");
                Win32.SetProcessDPIAware(); 
                System.IO.File.AppendAllText(logPath, "SetProcessDPIAware succeeded.\r\n");
            } 
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.AppendAllText(logPath, "DPI warning: " + ex.Message + "\r\n");
                }
                catch { }
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                System.IO.File.AppendAllText(logPath, "Creating DriveWidgetForm...\r\n");
                var form = new DriveWidgetForm();
                System.IO.File.AppendAllText(logPath, "DriveWidgetForm created. Running Application.Run...\r\n");
                
                Application.Run(form);
                System.IO.File.AppendAllText(logPath, "Application.Run completed normally.\r\n");
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.WriteAllText(crashPath, ex.ToString());
                    System.IO.File.AppendAllText(logPath, "CRASH: " + ex.Message + "\r\n");
                }
                catch { }
            }
        }
    }
}
