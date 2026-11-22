using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Voltrix
{
    class UpdateInfo
    {
        public string Tag = "";
        public string Version = "";
        public bool PreRelease;
        public string Body = "";
        public string AssetUrl = "";
    }

    // Reads the newest release from GitHub and can replace the EXE with it.
    static class Updater
    {
        public const string Repo = "FazeEggzYT/Voltrix-Hub";

        public static UpdateInfo Latest;      // a newer release, or null
        public static string Message = "Not checked yet.";
        public static bool Busy;
        public static event EventHandler Checked;

        static Control sync;

        public static void Init(Control owner)
        {
            sync = owner;
        }

        // The numbers in a tag like v0.5.0 -> 0.5.0
        public static string CleanVersion(string tag)
        {
            string t = tag.Trim();
            if (t.StartsWith("v") || t.StartsWith("V")) t = t.Substring(1);
            int end = 0;
            while (end < t.Length && (char.IsDigit(t[end]) || t[end] == '.')) end++;
            return t.Substring(0, end).Trim('.');
        }

        static bool IsNewer(string tag)
        {
            try
            {
                string v = CleanVersion(tag);
                if (v.IndexOf('.') < 0) return false;
                return new Version(v) > new Version(AppInfo.Version);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void CheckAsync()
        {
            if (Busy) return;
            Busy = true;
            Message = "Checking...";
            RaiseChecked();
            bool pre = AppState.PreRelease;
            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                UpdateInfo found = null;
                string msg;
                try
                {
                    found = Fetch(pre, out msg);
                }
                catch (Exception ex)
                {
                    msg = "Could not check: " + ex.Message;
                }
                string finalMsg = msg;
                UpdateInfo finalFound = found;
                Post(delegate
                {
                    Busy = false;
                    Latest = finalFound;
                    Message = finalMsg;
                    RaiseChecked();
                });
            });
        }

        static void Post(MethodInvoker action)
        {
            if (sync != null && sync.IsHandleCreated)
            {
                sync.BeginInvoke(action);
            }
        }

        static void RaiseChecked()
        {
            if (Checked != null)
            {
                Checked(null, EventArgs.Empty);
            }
        }

        static UpdateInfo Fetch(bool includePre, out string msg)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | ServicePointManager.SecurityProtocol;
            string json;
            using (WebClient wc = new WebClient())
            {
                wc.Headers["User-Agent"] = "VOLTRIX-Hub";
                json = wc.DownloadString("https://api.github.com/repos/" + Repo + "/releases?per_page=15");
            }
            JavaScriptSerializer js = new JavaScriptSerializer();
            js.MaxJsonLength = int.MaxValue;
            IEnumerable releases = js.DeserializeObject(json) as IEnumerable;
            if (releases == null)
            {
                msg = "GitHub gave an answer I could not read.";
                return null;
            }
            foreach (object o in releases)
            {
                IDictionary<string, object> r = o as IDictionary<string, object>;
                if (r == null) continue;
                bool draft = r.ContainsKey("draft") && r["draft"] is bool && (bool)r["draft"];
                bool pre = r.ContainsKey("prerelease") && r["prerelease"] is bool && (bool)r["prerelease"];
                if (draft) continue;
                if (pre && !includePre) continue;
                string tag = r.ContainsKey("tag_name") && r["tag_name"] != null ? r["tag_name"].ToString() : "";
                if (tag.Length == 0) continue;
                if (!IsNewer(tag))
                {
                    msg = "You are up to date (" + AppInfo.Full + "). Newest release: " + tag + ".";
                    return null;
                }
                UpdateInfo info = new UpdateInfo();
                info.Tag = tag;
                info.Version = CleanVersion(tag);
                info.PreRelease = pre;
                info.Body = r.ContainsKey("body") && r["body"] != null ? r["body"].ToString() : "";
                IEnumerable assets = r.ContainsKey("assets") ? r["assets"] as IEnumerable : null;
                if (assets != null)
                {
                    foreach (object ao in assets)
                    {
                        IDictionary<string, object> a = ao as IDictionary<string, object>;
                        if (a == null) continue;
                        string name = a.ContainsKey("name") && a["name"] != null ? a["name"].ToString() : "";
                        if (name.ToLower() == "voltrix-hub.exe" && a.ContainsKey("browser_download_url"))
                        {
                            info.AssetUrl = a["browser_download_url"].ToString();
                        }
                    }
                }
                msg = info.AssetUrl.Length > 0
                    ? "New version available: " + tag
                    : "Version " + tag + " exists but has no VOLTRIX-Hub.exe attached yet. Try again in a minute.";
                if (info.AssetUrl.Length == 0) return null;
                return info;
            }
            msg = "No releases found on GitHub.";
            return null;
        }

        // Downloads the new EXE, then restarts into it. Runs the download off the UI thread.
        public static void InstallAsync(UpdateInfo info, Action<string> progress, Action done)
        {
            if (Busy) return;
            Busy = true;
            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                string result = "";
                bool ok = false;
                string tmp = Path.Combine(Path.GetTempPath(), "VOLTRIX-Hub-update.exe");
                try
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | ServicePointManager.SecurityProtocol;
                    Post(delegate { progress("Downloading " + info.Tag + "..."); });
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers["User-Agent"] = "VOLTRIX-Hub";
                        wc.DownloadFile(info.AssetUrl, tmp);
                    }
                    FileInfo fi = new FileInfo(tmp);
                    if (!fi.Exists || fi.Length < 50000)
                    {
                        result = "The download looked wrong. Nothing was changed.";
                    }
                    else
                    {
                        ok = true;
                    }
                }
                catch (Exception ex)
                {
                    result = "Download failed: " + ex.Message;
                }
                bool finalOk = ok;
                string finalResult = result;
                Post(delegate
                {
                    Busy = false;
                    if (!finalOk)
                    {
                        progress(finalResult);
                        return;
                    }
                    try
                    {
                        string exe = Application.ExecutablePath;
                        string cmd = "/c ping 127.0.0.1 -n 3 > nul & move /y \"" + tmp + "\" \"" + exe + "\" & start \"\" \"" + exe + "\"";
                        ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", cmd);
                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = false;
                        Process.Start(psi);
                        progress("Restarting...");
                        done();
                    }
                    catch (Exception ex2)
                    {
                        progress("Could not start the update: " + ex2.Message);
                    }
                });
            });
        }

        // The changelog baked into this EXE.
        public static string EmbeddedChangelog()
        {
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Changelog.md"))
                {
                    if (s == null) return "Changelog not found in this build.";
                    using (StreamReader r = new StreamReader(s))
                    {
                        return Fix(r.ReadToEnd());
                    }
                }
            }
            catch (Exception)
            {
                return "Could not read the changelog.";
            }
        }

        public static string Fix(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }
    }

    // The Updates page: version, check, install, changelog.
    class UpdatesPage : Page
    {
        VButton check;
        VButton install;
        ChoiceBar view;
        TextBox box;
        Label status;

        public UpdatesPage()
        {
            check = new VButton("Check for updates", true);
            check.Click += delegate(object sender, EventArgs e) { Updater.CheckAsync(); };
            install = new VButton("Download and install", false);
            install.Visible = false;
            install.Click += delegate(object sender, EventArgs e)
            {
                if (Updater.Latest == null) return;
                install.Enabled = false;
                Updater.InstallAsync(Updater.Latest, delegate(string text) { status.Text = text; }, delegate { FindForm().Close(); });
            };

            view = new ChoiceBar(new string[] { "Changelog (this version)", "New version notes" });
            view.SelectedIndex = 0;
            view.Picked += delegate(object sender, EventArgs e) { ShowText(); };

            box = new TextBox();
            box.Multiline = true;
            box.ReadOnly = true;
            box.ScrollBars = ScrollBars.Vertical;
            box.BorderStyle = BorderStyle.None;
            box.BackColor = Theme.Panel;
            box.ForeColor = Theme.Text;
            box.Font = new Font("Consolas", 9.5f);

            status = new Label();
            status.Font = Theme.Body;
            status.ForeColor = Theme.Muted;
            status.BackColor = Theme.Bg;
            status.TextAlign = ContentAlignment.MiddleLeft;

            Controls.Add(check);
            Controls.Add(install);
            Controls.Add(status);
            Controls.Add(view);
            Controls.Add(box);
            Updater.Checked += delegate(object sender, EventArgs e) { Refresh2(); };
            Refresh2();
            DoLayout();
        }

        void Refresh2()
        {
            status.Text = Updater.Message;
            install.Visible = Updater.Latest != null;
            install.Enabled = true;
            check.Enabled = !Updater.Busy;
            ShowText();
        }

        void ShowText()
        {
            if (view.SelectedIndex == 1)
            {
                box.Text = Updater.Latest != null ? Updater.Fix(Updater.Latest.Body) : "No newer version found yet. Press Check for updates.";
            }
            else
            {
                box.Text = Updater.EmbeddedChangelog();
            }
        }

        void DoLayout()
        {
            int w = Math.Min(ClientSize.Width - 48, 800);
            if (w < 300) w = 300;
            int x = 24;
            check.SetBounds(x, 96, 200, 40);
            install.SetBounds(x + 212, 96, 220, 40);
            status.SetBounds(x, 144, w, 28);
            view.SetBounds(x, 184, Math.Min(w, 420), 40);
            box.SetBounds(x, 236, w, Math.Max(80, ClientSize.Height - 236 - 20));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            DoLayout();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            TextRenderer.DrawText(e.Graphics, "Updates", Theme.Title, new Rectangle(24, 24, 300, 50), Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, AppInfo.Name + " " + AppInfo.Full, Theme.Body, new Rectangle(300, 24, 500, 50), Theme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }

    // The Settings page.
    class SettingsPage : Page
    {
        const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        VToggle startup;
        VToggle minimised;
        VToggle checkStart;
        VToggle pre;
        VToggle desktopLink;
        VButton installBtn;
        Label installStatus;
        Label about;

        public SettingsPage()
        {
            startup = new VToggle("Start VOLTRIX Hub with Windows");
            startup.Changed += delegate(object sender, EventArgs e) { SetStartup(startup.Checked); };
            minimised = new VToggle("Start hidden in the tray");
            minimised.Changed += delegate(object sender, EventArgs e) { AppState.StartMinimised = minimised.Checked; SettingsStore.Save(); };
            checkStart = new VToggle("Check for updates when the app starts");
            checkStart.Changed += delegate(object sender, EventArgs e) { AppState.CheckOnStart = checkStart.Checked; SettingsStore.Save(); };
            pre = new VToggle("Include pre-releases when checking for updates");
            pre.Changed += delegate(object sender, EventArgs e) { AppState.PreRelease = pre.Checked; SettingsStore.Save(); };

            desktopLink = new VToggle("Add a desktop shortcut when installing");
            desktopLink.Checked = true;
            installBtn = new VButton("Install VOLTRIX Hub", true);
            installBtn.Click += delegate(object sender, EventArgs e) { InstallClicked(); };
            installStatus = new Label();
            installStatus.Font = Theme.Small;
            installStatus.ForeColor = Theme.Muted;
            installStatus.BackColor = Theme.Bg;

            about = new Label();
            about.Font = Theme.Small;
            about.ForeColor = Theme.Muted;
            about.BackColor = Theme.Bg;
            about.Text = "Version " + AppInfo.Full + "\r\nUpdates come from github.com/" + Updater.Repo + "\r\nYour settings and profiles are in %APPDATA%\\VOLTRIX Hub";

            Controls.Add(startup);
            Controls.Add(minimised);
            Controls.Add(checkStart);
            Controls.Add(pre);
            Controls.Add(desktopLink);
            Controls.Add(installBtn);
            Controls.Add(installStatus);
            Controls.Add(about);
            UpdateInstallUi();
            DoLayout();
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
                FindForm().Close();
                return;
            }
            bool ok;
            installStatus.Text = Installer.Install(desktopLink.Checked, true, out ok);
            if (ok)
            {
                Installer.StartInstalledSoon();
                FindForm().Close();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && startup != null)
            {
                startup.Checked = GetStartup();
                minimised.Checked = AppState.StartMinimised;
                checkStart.Checked = AppState.CheckOnStart;
                pre.Checked = AppState.PreRelease;
                UpdateInstallUi();
            }
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

        void DoLayout()
        {
            int w = Math.Min(ClientSize.Width - 48, 600);
            if (w < 300) w = 300;
            int x = 24;
            startup.SetBounds(x, 96, w, 36);
            minimised.SetBounds(x, 140, w, 36);
            checkStart.SetBounds(x, 184, w, 36);
            pre.SetBounds(x, 228, w, 36);
            desktopLink.SetBounds(x, 290, w, 36);
            installBtn.SetBounds(x, 334, Math.Min(w, 360), 40);
            installStatus.SetBounds(x, 380, w, 40);
            about.SetBounds(x, 430, w, 70);
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
