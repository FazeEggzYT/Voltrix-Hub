using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Voltrix
{
    // All the settings, in sections, in a scrolling column.
    class SettingsHost : StackPanel
    {
        const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        VToggle startup;
        VToggle desktopLink;
        VButton installBtn;
        Label installStatus;
        ComboBox themeBox;
        ChoiceBar fontBar;
        SwatchBar accent;

        public SettingsHost()
        {
            AddText("General", Theme.Big, Theme.Text, 34);
            startup = AddToggle("Start VOLTRIX Hub with Windows", GetStartup(), delegate(bool on) { SetStartup(on); });
            AddToggle("Start hidden in the tray", AppState.StartMinimised, delegate(bool on) { AppState.StartMinimised = on; SettingsStore.Save(); });
            AddToggle("Close button hides the app to the tray", AppState.CloseToTray, delegate(bool on) { AppState.CloseToTray = on; SettingsStore.Save(); });
            AddToggle("Show the start-up screen", AppState.ShowSplash, delegate(bool on) { AppState.ShowSplash = on; SettingsStore.Save(); });
            AddToggle("Apply my active profile when the app starts", AppState.ReapplyProfile, delegate(bool on) { AppState.ReapplyProfile = on; SettingsStore.Save(); });
            AddToggle("Refresh the device list every 30 seconds", AppState.AutoRescan, delegate(bool on) { AppState.AutoRescan = on; SettingsStore.Save(); });
            AddToggle("Overlay hotkeys (Ctrl+Alt+H HUD, Ctrl+Alt+C crosshair)", AppState.OverlayHotkeys, delegate(bool on) { AppState.OverlayHotkeys = on; SettingsStore.Save(); });
            Gap(10);

            AddText("Updates", Theme.Big, Theme.Text, 34);
            AddToggle("Check for updates when the app starts", AppState.CheckOnStart, delegate(bool on) { AppState.CheckOnStart = on; SettingsStore.Save(); });
            AddToggle("Include pre-releases when checking", AppState.PreRelease, delegate(bool on) { AppState.PreRelease = on; SettingsStore.Save(); });
            Gap(10);

            AddText("Appearance", Theme.Big, Theme.Text, 34);
            AddText("Theme, font and accent colour change after a restart.", Theme.Small, Theme.Muted, 22);
            AddText("Theme", Theme.Bold, Theme.Text, 24);
            themeBox = AddCombo(Theme.Names, Math.Max(0, Array.IndexOf(Theme.Names, AppState.ThemeName)));
            themeBox.SelectedIndexChanged += delegate(object sender, EventArgs e)
            {
                AppState.ThemeName = Theme.Names[themeBox.SelectedIndex];
                SettingsStore.Save();
            };
            AddText("Font", Theme.Bold, Theme.Text, 24);
            fontBar = AddChoice(Theme.FontNames, Math.Max(0, Array.IndexOf(Theme.FontNames, AppState.UiFont)), delegate(int i)
            {
                AppState.UiFont = Theme.FontNames[i];
                SettingsStore.Save();
            });
            AddText("Accent colour", Theme.Bold, Theme.Text, 24);
            accent = new SwatchBar();
            accent.BackColor = Theme.Bg;
            accent.Selected = AppState.AccentColor;
            accent.SetBounds(X, y, CW, accent.Height);
            accent.Picked += delegate(object sender, EventArgs e)
            {
                AppState.AccentColor = accent.Selected;
                SettingsStore.Save();
            };
            Controls.Add(accent);
            y += accent.Height + 12;
            AddButton("Custom accent colour...", false, X, 240, delegate(object sender, EventArgs e)
            {
                Color c = AppState.AccentColor;
                if (PickColor(ref c))
                {
                    AppState.AccentColor = c;
                    accent.Selected = c;
                    SettingsStore.Save();
                }
            });
            AddButton("Restart now to apply", true, X + 252, 240, delegate(object sender, EventArgs e) { RestartNow(); });
            y += 52;
            Gap(10);

            AddText("Install", Theme.Big, Theme.Text, 34);
            desktopLink = AddToggle("Add a desktop shortcut when installing", true, delegate(bool on) { });
            installBtn = AddButton("Install VOLTRIX Hub", true, X, 340, delegate(object sender, EventArgs e) { InstallClicked(); });
            y += 48;
            installStatus = AddText("", Theme.Small, Theme.Muted, 40);
            Gap(10);

            AddText("Data", Theme.Big, Theme.Text, 34);
            AddButton("Open the settings folder", false, X, 240, delegate(object sender, EventArgs e) { OpenFolder(); });
            AddButton("Reset all settings", false, X + 252, 240, delegate(object sender, EventArgs e) { ResetAll(); });
            y += 52;
            AddText("Version " + AppInfo.Full + "\r\nUpdates come from github.com/" + Updater.Repo + "\r\nSettings and profiles: %APPDATA%\\VOLTRIX Hub", Theme.Small, Theme.Muted, 60);
            Done();
            UpdateInstallUi();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && startup != null)
            {
                startup.Checked = GetStartup();
                UpdateInstallUi();
            }
        }

        void UpdateInstallUi()
        {
            bool fromInstall = Installer.RunningFromInstall();
            installBtn.Text = fromInstall ? "Uninstall VOLTRIX Hub" : (Installer.IsInstalled() ? "Reinstall and open the installed copy" : "Install VOLTRIX Hub");
            desktopLink.Visible = !fromInstall;
            installStatus.Text = fromInstall
                ? "Installed in " + Installer.InstallDir
                : "Installs for your user only (no admin needed), adds a Start Menu shortcut and an entry in Windows Installed apps.";
            installBtn.Invalidate();
        }

        void InstallClicked()
        {
            if (Installer.RunningFromInstall())
            {
                if (MessageBox.Show("Remove VOLTRIX Hub from this PC?", AppInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                bool wipe = MessageBox.Show("Also delete your settings and profiles?\r\n\r\nYes = delete them too\r\nNo = keep them", AppInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
                installStatus.Text = Installer.Uninstall(wipe);
                AppState.Exiting = true;
                FindForm().Close();
                return;
            }
            bool ok;
            installStatus.Text = Installer.Install(desktopLink.Checked, true, out ok);
            if (ok)
            {
                Installer.StartInstalledSoon();
                AppState.Exiting = true;
                FindForm().Close();
            }
        }

        void RestartNow()
        {
            try
            {
                SettingsStore.Save();
                string cmd = "/c ping 127.0.0.1 -n 3 > nul & start \"\" \"" + Application.ExecutablePath + "\"";
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
                AppState.Exiting = true;
                FindForm().Close();
            }
            catch (Exception)
            {
            }
        }

        static string DataDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VOLTRIX Hub");
        }

        void OpenFolder()
        {
            try
            {
                Directory.CreateDirectory(DataDir());
                ProcessStartInfo psi = new ProcessStartInfo(DataDir());
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
            catch (Exception)
            {
            }
        }

        void ResetAll()
        {
            if (MessageBox.Show("Reset all VOLTRIX Hub settings?\r\n\r\nYour saved profiles are kept. The app will restart.", AppInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                string f = Path.Combine(DataDir(), "settings.txt");
                if (File.Exists(f)) File.Delete(f);
            }
            catch (Exception)
            {
            }
            AppState.Exiting = true;
            string cmd = "/c ping 127.0.0.1 -n 3 > nul & start \"\" \"" + Application.ExecutablePath + "\"";
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", cmd);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process.Start(psi);
            FindForm().Close();
        }

        static bool GetStartup()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    return k != null && k.GetValue("VOLTRIX Hub") != null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void SetStartup(bool on)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (k == null) return;
                    if (on)
                    {
                        k.SetValue("VOLTRIX Hub", "\"" + Application.ExecutablePath + "\"");
                    }
                    else
                    {
                        k.DeleteValue("VOLTRIX Hub", false);
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }

    class SettingsPage : Page
    {
        SettingsHost host;

        public SettingsPage()
        {
            host = new SettingsHost();
            Controls.Add(host);
            DoLayout();
        }

        void DoLayout()
        {
            host.SetBounds(24, 96, Math.Max(300, ClientSize.Width - 48), Math.Max(200, ClientSize.Height - 96 - 12));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            DoLayout();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            TextRenderer.DrawText(e.Graphics, "Settings", Theme.Title, new Rectangle(24, 24, 400, 50), Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }
}
