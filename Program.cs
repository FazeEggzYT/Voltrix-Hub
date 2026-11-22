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
                SettingsStore.Load();
                Theme.Apply(AppState.ThemeName, AppState.AccentColor, AppState.UiFont);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                SplashForm splash = null;
                if (AppState.ShowSplash && !AppState.StartMinimised)
                {
                    splash = new SplashForm();
                    splash.Show();
                    splash.SetStep("Loading your settings", 0.15f);
                    splash.Pump(350);
                    splash.SetStep("Looking for your devices", 0.45f);
                    splash.Pump(150);
                }
                try
                {
                    AppState.Last = Scanner.Scan();
                }
                catch (Exception)
                {
                    AppState.Last = null;
                }
                if (splash != null)
                {
                    ScanResult r = AppState.Last;
                    splash.SetDevices(r == null ? "" : r.KeyboardName, r == null ? "" : r.MouseName);
                    splash.SetStep("Devices ready", 0.75f);
                    splash.Pump(900);
                    splash.SetStep("Getting things ready", 0.95f);
                    splash.Pump(250);
                }
                Application.Run(new MainForm(splash));
            }
        }
    }
}
