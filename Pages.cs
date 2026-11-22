using System;
using System.Drawing;
using System.Windows.Forms;

namespace Voltrix
{
    // Base for every page shown in the main area.
    class Page : Panel
    {
        public Page()
        {
            DoubleBuffered = true;
            BackColor = Theme.Bg;
            ResizeRedraw = true;
        }
    }

    // Devices page: a list of device cards and the scanner.
    class DevicesPage : Page
    {
        DeviceCard keyboard;
        DeviceCard mouse;
        VButton scanButton;
        VButton copyButton;
        VButton detailsButton;
        bool showDetails;
        TextBox log;
        Label scanState;

        public event EventHandler<CardEventArgs> OpenDevice;
        public event EventHandler OpenMouse;

        public DevicesPage()
        {
            keyboard = new DeviceCard("PRO", "Logitech PRO keyboard.", true, true);
            mouse = new DeviceCard("G502 HERO", "Logitech G502 HERO mouse.", false, false);
            mouse.ShowQuick = false;
            mouse.Opened += delegate(object sender, CardEventArgs e)
            {
                if (OpenMouse != null)
                {
                    OpenMouse(this, EventArgs.Empty);
                }
            };
            keyboard.Opened += delegate(object sender, CardEventArgs e)
            {
                if (OpenDevice != null)
                {
                    OpenDevice(this, e);
                }
            };

            scanButton = new VButton("Refresh devices", true);
            detailsButton = new VButton("Show details", false);
            detailsButton.Click += delegate(object sender, EventArgs e)
            {
                showDetails = !showDetails;
                detailsButton.Text = showDetails ? "Hide details" : "Show details";
                detailsButton.Invalidate();
                DoLayout();
            };
            scanButton.Click += delegate(object sender, EventArgs e) { RunScan(); };
            copyButton = new VButton("Copy details", false);
            copyButton.Click += delegate(object sender, EventArgs e)
            {
                if (log.Text.Length > 0)
                {
                    Clipboard.SetText(log.Text);
                    scanState.Text = "Details copied.";
                }
            };

            scanState = new Label();
            scanState.ForeColor = Theme.Muted;
            scanState.BackColor = Theme.Bg;
            scanState.Font = Theme.Body;
            scanState.TextAlign = ContentAlignment.MiddleLeft;
            scanState.Text = "";

            log = new TextBox();
            log.Multiline = true;
            log.ReadOnly = true;
            log.ScrollBars = ScrollBars.Vertical;
            log.BorderStyle = BorderStyle.None;
            log.BackColor = Theme.Panel;
            log.ForeColor = Theme.Text;
            log.Font = new Font("Consolas", 9f);

            Controls.Add(keyboard);
            Controls.Add(mouse);
            Controls.Add(scanButton);
            Controls.Add(copyButton);
            Controls.Add(detailsButton);
            Controls.Add(scanState);
            Controls.Add(log);
            DoLayout();
        }

        public void RunScan()
        {
            Cursor = Cursors.WaitCursor;
            scanState.Text = "Refreshing...";
            scanState.Refresh();
            ScanResult r;
            try
            {
                r = Scanner.Scan();
            }
            catch (Exception ex)
            {
                r = new ScanResult();
                r.Summary = "Scan failed: " + ex.Message;
                r.Report = r.Summary;
            }
            AppState.Last = r;
            ShowResult(r);
            Cursor = Cursors.Default;
        }

        // Shows a scan result on the cards and in the report box.
        public void ShowResult(ScanResult r)
        {
            log.Text = r.Report;
            int count = (r.KeyboardName.Length > 0 ? 1 : 0) + (r.Mouse != null ? 1 : 0);
            if (r.GHubRunning && AppState.GHubWarning)
            {
                scanState.Text = "Logitech G HUB is running. Quit it from its tray icon if lighting or keys do not respond.";
            }
            else if (count == 0)
            {
                scanState.Text = "No devices found.";
            }
            else
            {
                scanState.Text = count == 1 ? "1 device connected." : "2 devices connected.";
            }
            if (r.KeyboardName.Length > 0)
            {
                keyboard.SetSubtitle(r.KeyboardName + ". Connected over USB.");
            }
            else if (r.FoundLogitech)
            {
                keyboard.SetSubtitle("Logitech device found, but it could not be set up. Press Show details.");
            }
            else
            {
                keyboard.SetSubtitle("Not found. Plug in the keyboard and press Refresh.");
            }
            if (r.Mouse != null)
            {
                mouse.SetEnabled(true);
                mouse.SetSubtitle(r.MouseName + ". Connected over USB." + (r.MouseDpi > 0 ? " Current DPI " + r.MouseDpi + "." : ""));
            }
            else
            {
                mouse.SetEnabled(false);
                mouse.SetSubtitle("Not found. Plug in the mouse and press Refresh.");
            }
        }

        void DoLayout()
        {
            int w = Math.Min(ClientSize.Width - 48, 1200);
            if (w < 300) w = 300;
            int x = 24;
            keyboard.SetBounds(x, 96, w, 112);
            mouse.SetBounds(x, 96 + 112 + 16, w, 112);
            int y = 96 + 112 + 16 + 112 + 20;
            scanButton.SetBounds(x, y, 170, 40);
            detailsButton.SetBounds(x + 182, y, 150, 40);
            copyButton.SetBounds(x + 344, y, 150, 40);
            copyButton.Visible = showDetails;
            int sx = showDetails ? x + 506 : x + 344;
            scanState.SetBounds(sx, y, Math.Max(100, x + w - sx), 40);
            int ly = y + 52;
            log.Visible = showDetails;
            log.SetBounds(x, ly, w, Math.Max(60, ClientSize.Height - ly - 20));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            DoLayout();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            TextRenderer.DrawText(e.Graphics, "Devices", Theme.Title, new Rectangle(24, 24, 400, 50), Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }

    // The page for one device: back button, title, a tab rail and a content area.
    class DevicePage : Page
    {
        IconButton back;
        IconButton tabLight;
        IconButton tabAssign;
        IconButton tabGame;
        string currentTab = "light";
        LightingPanel lighting;
        AssignPanel assign;
        GamePanel game;

        public event EventHandler BackClicked;

        public DevicePage()
        {
            back = new IconButton("back");
            tabLight = new IconButton("sun");
            tabAssign = new IconButton("plus");
            tabGame = new IconButton("game");
            back.Click += delegate(object sender, EventArgs e)
            {
                if (BackClicked != null)
                {
                    BackClicked(this, EventArgs.Empty);
                }
            };
            tabLight.Click += delegate(object sender, EventArgs e) { SelectTab("light"); };
            tabAssign.Click += delegate(object sender, EventArgs e) { SelectTab("assign"); };
            tabGame.Click += delegate(object sender, EventArgs e) { SelectTab("game"); };
            Controls.Add(back);
            Controls.Add(tabLight);
            Controls.Add(tabAssign);
            Controls.Add(tabGame);
            lighting = new LightingPanel();
            Controls.Add(lighting);
            assign = new AssignPanel();
            Controls.Add(assign);
            game = new GamePanel();
            Controls.Add(game);
            AppState.LightChanged += delegate(object sender, EventArgs e) { Invalidate(); };
            SelectTab("light");
            DoLayout();
        }

        public void SelectTab(string tab)
        {
            currentTab = tab;
            tabLight.Selected = tab == "light";
            tabAssign.Selected = tab == "assign";
            tabGame.Selected = tab == "game";
            lighting.Visible = tab == "light";
            assign.Visible = tab == "assign";
            game.Visible = tab == "game";
            Invalidate();
        }

        void DoLayout()
        {
            back.Location = new Point(16, 14);
            tabLight.Location = new Point(16, 90);
            tabAssign.Location = new Point(16, 148);
            tabGame.Location = new Point(16, 206);
            lighting.SetBounds(80, 90, 320, Math.Max(200, ClientSize.Height - 120));
            int wide = Math.Max(300, Math.Min(ClientSize.Width - 104, 640));
            assign.SetBounds(80, 90, wide, Math.Max(300, ClientSize.Height - 120));
            game.SetBounds(80, 90, wide, Math.Max(300, ClientSize.Height - 120));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            DoLayout();
        }

        string TabName()
        {
            if (currentTab == "assign") return "Assignments";
            if (currentTab == "game") return "Game Mode";
            return "Lighting";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            TextRenderer.DrawText(g, "PRO", Theme.Title, new Rectangle(76, 12, 400, 50), Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            Rectangle panel = new Rectangle(80, 90, 320, Math.Max(200, ClientSize.Height - 120));
            if (currentTab == "light")
            {
                int bx = panel.Right + 20;
                Rectangle board = new Rectangle(bx, 90, Math.Max(200, ClientSize.Width - bx - 24), 300);
                Theme.FillRound(g, board, 14, Color.FromArgb(236, 236, 236), Color.Empty);
                Rectangle glow = new Rectangle(board.X + 24, board.Y + 24, board.Width - 48, board.Height - 48);
                Theme.FillRound(g, glow, 12, AppState.LightColor, Color.Empty);
                Color label = (AppState.LightColor.GetBrightness() > 0.6f) ? Color.FromArgb(30, 30, 30) : Color.White;
                TextRenderer.DrawText(g, "Preview: all keys", Theme.Bold, glow, label, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    // A simple page for sections that arrive in later versions.
    class PlaceholderPage : Page
    {
        string title;
        string message;

        public PlaceholderPage(string titleText, string messageText)
        {
            title = titleText;
            message = messageText;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            TextRenderer.DrawText(g, title, Theme.Title, new Rectangle(24, 24, 500, 50), Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            Rectangle card = new Rectangle(24, 96, Math.Min(ClientSize.Width - 48, 700), 110);
            Theme.FillRound(g, card, 14, Theme.Panel, Theme.Border);
            TextRenderer.DrawText(g, message, Theme.Body, new Rectangle(card.X + 20, card.Y + 16, card.Width - 40, card.Height - 32), Theme.Muted, TextFormatFlags.Left | TextFormatFlags.WordBreak);
        }
    }
}
