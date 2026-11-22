using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Voltrix
{
    // Installs VOLTRIX Hub for the current user (no admin needed) and removes it again.
    // Files go to %LOCALAPPDATA%\Programs\VOLTRIX Hub, shortcuts to the Start Menu and Desktop.
    static class Installer
    {
        const string UninstallKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\VOLTRIX Hub";
        const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

        public static string InstallDir
        {
            get { return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"), "VOLTRIX Hub"); }
        }

        public static string InstalledExe
        {
            get { return Path.Combine(InstallDir, "VOLTRIX-Hub.exe"); }
        }

        public static bool RunningFromInstall()
        {
            return string.Equals(Path.GetFullPath(Application.ExecutablePath), Path.GetFullPath(InstalledExe), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsInstalled()
        {
            return File.Exists(InstalledExe);
        }

        static string StartMenuLink()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "VOLTRIX Hub.lnk");
        }

        static string DesktopLink()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "VOLTRIX Hub.lnk");
        }

        // Makes a .lnk shortcut using Windows Script Host (no extra libraries).
        static void MakeShortcut(string linkPath, string target)
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            if (t == null) return;
            object shell = Activator.CreateInstance(t);
            object link = t.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { linkPath });
            Type lt = link.GetType();
            lt.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, link, new object[] { target });
            lt.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, link, new object[] { Path.GetDirectoryName(target) });
            lt.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, link, new object[] { "VOLTRIX Hub" });
            lt.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, link, null);
        }

        // Copies the app to the install folder, adds shortcuts and the Windows uninstall entry.
        // Returns a message. On success, the installed copy is started and this one should close.
        public static string Install(bool desktop, bool startMenu, out bool ok)
        {
            ok = false;
            try
            {
                Directory.CreateDirectory(InstallDir);
                string source = Application.ExecutablePath;
                if (!RunningFromInstall())
                {
                    File.Copy(source, InstalledExe, true);
                }
                if (startMenu) MakeShortcut(StartMenuLink(), InstalledExe);
                if (desktop) MakeShortcut(DesktopLink(), InstalledExe);

                using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(UninstallKey))
                {
                    k.SetValue("DisplayName", "VOLTRIX Hub");
                    k.SetValue("DisplayVersion", AppInfo.Version);
                    k.SetValue("Publisher", "VOLTRIX");
                    k.SetValue("InstallLocation", InstallDir);
                    k.SetValue("DisplayIcon", InstalledExe);
                    k.SetValue("UninstallString", "\"" + InstalledExe + "\" --uninstall");
                    k.SetValue("NoModify", 1, Microsoft.Win32.RegistryValueKind.DWord);
                    k.SetValue("NoRepair", 1, Microsoft.Win32.RegistryValueKind.DWord);
                }
                ok = true;
                return "Installed to " + InstallDir;
            }
            catch (Exception ex)
            {
                return "Could not install: " + ex.Message;
            }
        }

        // Starts the installed copy a moment after this one closes.
        public static void StartInstalledSoon()
        {
            string cmd = "/c ping 127.0.0.1 -n 3 > nul & start \"\" \"" + InstalledExe + "\"";
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", cmd);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }

        // Removes shortcuts, startup entry, uninstall entry and (a moment later) the program folder.
        public static string Uninstall(bool deleteSettings)
        {
            try
            {
                try { File.Delete(StartMenuLink()); } catch (Exception) { }
                try { File.Delete(DesktopLink()); } catch (Exception) { }
                try
                {
                    Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false);
                    using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, true))
                    {
                        if (k != null) k.DeleteValue("VOLTRIX Hub", false);
                    }
                }
                catch (Exception) { }

                string cmd = "/c ping 127.0.0.1 -n 3 > nul & rmdir /s /q \"" + InstallDir + "\"";
                if (deleteSettings)
                {
                    string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VOLTRIX Hub");
                    cmd += " & rmdir /s /q \"" + data + "\"";
                }
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
                return "VOLTRIX Hub was removed.";
            }
            catch (Exception ex)
            {
                return "Could not fully uninstall: " + ex.Message;
            }
        }

        // Called from Main when Windows runs the uninstall entry (--uninstall).
        public static void RunUninstallFromArgs()
        {
            DialogResult r = MessageBox.Show("Remove VOLTRIX Hub from this PC?", AppInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
            DialogResult keep = MessageBox.Show("Also delete your settings and profiles?\r\n\r\nYes = delete them too\r\nNo = keep them", AppInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            int me = Process.GetCurrentProcess().Id;
            foreach (Process p in Process.GetProcessesByName("VOLTRIX-Hub"))
            {
                if (p.Id != me)
                {
                    try { p.Kill(); } catch (Exception) { }
                }
            }
            string msg = Uninstall(keep == DialogResult.Yes);
            MessageBox.Show(msg, AppInfo.Name);
        }
    }
}
