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
        PlaceholderPage profilesPage;
        PlaceholderPage updatesPage;
        PlaceholderPage settingsPage;
        NotifyIcon tray;
        Icon appIcon;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool DestroyIcon(IntPtr handle);

        public MainForm()
        {
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

            content = new Panel();
            content.Dock = DockStyle.Fill;
            content.BackColor = Theme.Bg;
            Controls.Add(content);

            BuildSidebar();

            devicesPage = new DevicesPage();
            devicePage = new DevicePage();
            profilesPage = new PlaceholderPage("Profiles", "Profiles arrive in a later version. They will hold your lighting, key assignments and macros.");
            updatesPage = new PlaceholderPage("Updates", "VOLTRIX Hub " + AppInfo.Full + "\r\nThe auto-updater and changelog arrive in a later version.");
            settingsPage = new PlaceholderPage("Settings", "Settings arrive in a later version.");

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
            SelectNav(navDevices);
            ShowPage(devicesPage);
        }

        Icon MakeIcon()
        {
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
            navUpdates = MakeNav("Updates", "down", 180);
            navSettings = MakeNav("Settings", "gear", 228);

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
            else if (b == navUpdates) ShowPage(updatesPage);
            else ShowPage(settingsPage);
        }

        void SelectNav(NavButton active)
        {
            navDevices.Selected = active == navDevices;
            navProfiles.Selected = active == navProfiles;
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
            exit.Click += delegate(object sender, EventArgs e) { Close(); };
            menu.Items.Add(open);
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (tray != null)
            {
                tray.Visible = false;
                tray.Dispose();
            }
            base.OnFormClosed(e);
        }
    }
}
