using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Voltrix
{
    class MainForm : Form
    {
        Panel sidebar;
        Panel content;
        NavButton navDevices;
        NavButton navProfiles;
        NavButton navUpdates;
        NavButton navSettings;
        DevicesPage devicesPage;
        DevicePage devicePage;
        MousePage mousePage;
        ProfilesPage profilesPage;
        OverlayPage overlayPage;
        NavButton navOverlay;
        UpdatesPage updatesPage;
        SettingsPage settingsPage;
        Label versionLabel;
        NotifyIcon tray;
        ToolStripMenuItem pauseItem;
        GameWatcher watcher;
        Icon appIcon;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool DestroyIcon(IntPtr handle);

        SplashForm splash;
        Timer rescanTimer;

        public MainForm(SplashForm splashForm)
        {
            splash = splashForm;
            Text = AppInfo.Name + " " + AppInfo.Full;
            ClientSize = new Size(1000, 640);
            MinimumSize = new Size(820, 560);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Body;
            DoubleBuffered = true;

            appIcon = MakeIcon();
            Icon = appIcon;
            LoadBounds();
            TopMost = AppState.AlwaysOnTop;
            AppState.SettingsChanged += delegate(object sender, EventArgs e) { TopMost = AppState.AlwaysOnTop; };
            Hotkeys.Handler = RunHotkey;

            content = new Panel();
            content.Dock = DockStyle.Fill;
            content.BackColor = Theme.Bg;
            Controls.Add(content);

            BuildSidebar();

            devicesPage = new DevicesPage();
            devicePage = new DevicePage();
            mousePage = new MousePage();
            profilesPage = new ProfilesPage();
            overlayPage = new OverlayPage();
            updatesPage = new UpdatesPage();
            settingsPage = new SettingsPage();

            devicesPage.OpenMouse += delegate(object sender, EventArgs e)
            {
                ShowPage(mousePage);
            };
            mousePage.BackClicked += delegate(object sender, EventArgs e)
            {
                SelectNav(navDevices);
                ShowPage(devicesPage);
            };
            devicesPage.OpenDevice += delegate(object sender, CardEventArgs e)
            {
                devicePage.SelectTab(e.Tab);
                ShowPage(devicePage);
            };
            devicePage.BackClicked += delegate(object sender, EventArgs e)
            {
                SelectNav(navDevices);
                ShowPage(devicesPage);
            };

            BuildTray();
            watcher = new GameWatcher();
            AppState.Watcher = watcher;
            Updater.Init(this);
            Updater.Checked += delegate(object sender, EventArgs e)
            {
                versionLabel.Text = Updater.Latest != null ? "Update available: " + Updater.Latest.Version : "Version " + AppInfo.Full;
            };
            Shown += delegate(object sender, EventArgs e)
            {
                KeyHook.Install(this);
                watcher.Start();
                if (AppState.Last != null)
                {
                    devicesPage.ShowResult(AppState.Last);
                }
                else
                {
                    devicesPage.RunScan();
                }
                Overlays.Apply();
                if (AppState.ReapplyProfile && AppState.ActiveProfile.Length > 0)
                {
                    Profile ap = ProfileStore.Load(AppState.ActiveProfile);
                    if (ap != null)
                    {
                        ProfileApply.Apply(ap, false);
                    }
                }
                if (AppState.CheckOnStart)
                {
                    Updater.CheckAsync();
                }
                if (AppState.StartMinimised)
                {
                    Hide();
                }
                if (splash != null)
                {
                    splash.FadeAndClose();
                    splash = null;
                    Activate();
                }
            };
            rescanTimer = new Timer();
            rescanTimer.Interval = 30000;
            rescanTimer.Tick += delegate(object sender, EventArgs e)
            {
                if (AppState.AutoRescan && Visible && WindowState != FormWindowState.Minimized)
                {
                    devicesPage.RunScan();
                }
            };
            rescanTimer.Start();
            SelectNav(navDevices);
            ShowPage(devicesPage);
        }

        void LoadBounds()
        {
            if (!AppState.RememberWindow || AppState.WinBounds.Length == 0) return;
            try
            {
                string[] p = AppState.WinBounds.Split(',');
                if (p.Length < 4) return;
                Rectangle r = new Rectangle(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
                bool onScreen = false;
                foreach (Screen sc in Screen.AllScreens)
                {
                    Rectangle inter = Rectangle.Intersect(sc.WorkingArea, r);
                    if (inter.Width > 200 && inter.Height > 150) onScreen = true;
                }
                if (!onScreen || r.Width < MinimumSize.Width || r.Height < MinimumSize.Height) return;
                StartPosition = FormStartPosition.Manual;
                Bounds = r;
                if (p.Length > 4 && p[4] == "1")
                {
                    WindowState = FormWindowState.Maximized;
                }
            }
            catch (Exception)
            {
            }
        }

        void SaveBounds()
        {
            try
            {
                if (WindowState == FormWindowState.Minimized) return;
                Rectangle r = WindowState == FormWindowState.Normal ? Bounds : base.RestoreBounds;
                AppState.WinBounds = r.X + "," + r.Y + "," + r.Width + "," + r.Height + "," + (WindowState == FormWindowState.Maximized ? "1" : "0");
                SettingsStore.Save();
            }
            catch (Exception)
            {
            }
        }

        void RunHotkey(string id)
        {
            if (id == "hud")
            {
                Overlays.ToggleHud();
            }
            else if (id == "cross")
            {
                Overlays.ToggleCross();
            }
            else if (id == "window")
            {
                if (Visible && WindowState != FormWindowState.Minimized)
                {
                    Hide();
                }
                else
                {
                    RestoreFromTray();
                }
            }
            else if (id == "assign")
            {
                AppState.AssignmentsOn = !AppState.AssignmentsOn;
                SettingsStore.Save();
                AppState.RaiseSettingsChanged();
            }
            else if (id == "game")
            {
                AppState.GameManual = !AppState.GameManual;
                SettingsStore.Save();
                if (watcher != null)
                {
                    watcher.Apply();
                }
                AppState.RaiseSettingsChanged();
            }
        }

        Icon MakeIcon()
        {
            try
            {
                using (System.IO.Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AppIcon.ico"))
                {
                    if (s != null)
                    {
                        return new Icon(s);
                    }
                }
            }
            catch (Exception)
            {
            }
            using (Bitmap bmp = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    Icons.DrawLogo(g, new Rectangle(0, 0, 32, 32), Theme.Accent);
                }
                IntPtr h = bmp.GetHicon();
                Icon tmp = Icon.FromHandle(h);
                Icon copy = (Icon)tmp.Clone();
                tmp.Dispose();
                DestroyIcon(h);
                return copy;
            }
        }

        void BuildSidebar()
        {
            sidebar = new Panel();
            sidebar.Dock = DockStyle.Left;
            sidebar.Width = 210;
            sidebar.BackColor = Theme.Panel;

            Label version = new Label();
            versionLabel = version;
            version.Dock = DockStyle.Bottom;
            version.Height = 44;
            version.TextAlign = ContentAlignment.MiddleLeft;
            version.Padding = new Padding(16, 0, 0, 0);
            version.ForeColor = Theme.Muted;
            version.BackColor = Theme.Panel;
            version.Font = Theme.Small;
            version.Text = "Version " + AppInfo.Full;
            sidebar.Controls.Add(version);

            LogoBox logo = new LogoBox();
            logo.SetBounds(0, 12, 209, 56);
            sidebar.Controls.Add(logo);

            navDevices = MakeNav("Devices", "kbd", 84);
            navProfiles = MakeNav("Profiles", "users", 132);
            navOverlay = MakeNav("Overlay", "grid", 180);
            navUpdates = MakeNav("Updates", "down", 228);
            navSettings = MakeNav("Settings", "gear", 276);

            Panel line = new Panel();
            line.Dock = DockStyle.Right;
            line.Width = 1;
            line.BackColor = Theme.Border;
            sidebar.Controls.Add(line);

            Controls.Add(sidebar);
        }

        NavButton MakeNav(string text, string icon, int top)
        {
            NavButton b = new NavButton(text, icon);
            b.SetBounds(12, top, 186, 44);
            b.Click += NavClicked;
            sidebar.Controls.Add(b);
            return b;
        }

        void NavClicked(object sender, EventArgs e)
        {
            NavButton b = (NavButton)sender;
            SelectNav(b);
            if (b == navDevices) ShowPage(devicesPage);
            else if (b == navProfiles) ShowPage(profilesPage);
            else if (b == navOverlay) ShowPage(overlayPage);
            else if (b == navUpdates) ShowPage(updatesPage);
            else ShowPage(settingsPage);
        }

        void SelectNav(NavButton active)
        {
            navDevices.Selected = active == navDevices;
            navProfiles.Selected = active == navProfiles;
            navOverlay.Selected = active == navOverlay;
            navUpdates.Selected = active == navUpdates;
            navSettings.Selected = active == navSettings;
        }

        void ShowPage(Page p)
        {
            content.SuspendLayout();
            content.Controls.Clear();
            p.Dock = DockStyle.Fill;
            content.Controls.Add(p);
            content.ResumeLayout();
            p.Invalidate();
        }

        void BuildTray()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem open = new ToolStripMenuItem("Open " + AppInfo.Name);
            open.Click += delegate(object sender, EventArgs e) { RestoreFromTray(); };
            ToolStripMenuItem exit = new ToolStripMenuItem("Exit");
            exit.Click += delegate(object sender, EventArgs e) { AppState.Exiting = true; Close(); };
            pauseItem = new ToolStripMenuItem(AppState.AssignmentsOn ? "Pause key assignments" : "Resume key assignments");
            pauseItem.Click += delegate(object sender, EventArgs e)
            {
                AppState.AssignmentsOn = !AppState.AssignmentsOn;
                pauseItem.Text = AppState.AssignmentsOn ? "Pause key assignments" : "Resume key assignments";
                SettingsStore.Save();
                AppState.RaiseSettingsChanged();
            };
            AppState.SettingsChanged += delegate(object sender, EventArgs e)
            {
                pauseItem.Text = AppState.AssignmentsOn ? "Pause key assignments" : "Resume key assignments";
            };
            menu.Items.Add(open);
            menu.Items.Add(pauseItem);
            menu.Items.Add(exit);

            tray = new NotifyIcon();
            tray.Icon = appIcon;
            tray.Text = AppInfo.Name;
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
            tray.DoubleClick += delegate(object sender, EventArgs e) { RestoreFromTray(); };
        }

        void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (AppState.RememberWindow && Visible)
            {
                SaveBounds();
            }
            if (AppState.CloseToTray && !AppState.Exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            if (AppState.ConfirmExit && !AppState.Exiting && e.CloseReason == CloseReason.UserClosing)
            {
                if (MessageBox.Show("Close VOLTRIX Hub? Your key assignments and overlays will stop.", AppInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Overlays.Shutdown();
            KeyHook.Uninstall();
            if (watcher != null)
            {
                watcher.Stop();
            }
            if (tray != null)
            {
                tray.Visible = false;
                tray.Dispose();
            }
            base.OnFormClosed(e);
        }
    }
}
