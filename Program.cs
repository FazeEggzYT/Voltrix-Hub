using System;
using System.Threading;
using System.Windows.Forms;

namespace Voltrix
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            foreach (string a in args)
            {
                if (a == "--uninstall")
                {
                    Application.EnableVisualStyles();
                    Installer.RunUninstallFromArgs();
                    return;
                }
            }
            bool created;
            using (Mutex mutex = new Mutex(true, "VOLTRIX-Hub-single-instance", out created))
            {
                if (!created)
                {
                    MessageBox.Show("VOLTRIX Hub is already running. Check the tray icon.", AppInfo.Name);
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
