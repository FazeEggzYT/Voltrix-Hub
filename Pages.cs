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

    // Devices page: a list of device cards.
    class DevicesPage : Page
    {
        DeviceCard keyboard;
        DeviceCard mouse;

        public event EventHandler<CardEventArgs> OpenDevice;

        public DevicesPage()
        {
            keyboard = new DeviceCard("PRO", "Logitech PRO keyboard. USB. Preview data: no device is connected yet.", true, true);
            mouse = new DeviceCard("G502 HERO", "Mouse support is planned for a later version.", false, false);
            keyboard.Opened += delegate(object sender, CardEventArgs e)
            {
                if (OpenDevice != null)
                {
                    OpenDevice(this, e);
                }
            };
            Controls.Add(keyboard);
            Controls.Add(mouse);
            DoLayout();
        }

        void DoLayout()
        {
            int w = Math.Min(ClientSize.Width - 48, 900);
            if (w < 300) w = 300;
            int x = 24;
            keyboard.SetBounds(x, 96, w, 112);
            mouse.SetBounds(x, 96 + 112 + 16, w, 112);
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
            SelectTab("light");
            DoLayout();
        }

        public void SelectTab(string tab)
        {
            currentTab = tab;
            tabLight.Selected = tab == "light";
            tabAssign.Selected = tab == "assign";
            tabGame.Selected = tab == "game";
            Invalidate();
        }

        void DoLayout()
        {
            back.Location = new Point(16, 14);
            tabLight.Location = new Point(16, 90);
            tabAssign.Location = new Point(16, 148);
            tabGame.Location = new Point(16, 206);
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
            Theme.FillRound(g, panel, 14, Theme.Panel, Theme.Border);
            TextRenderer.DrawText(g, TabName(), Theme.Big, new Rectangle(panel.X + 16, panel.Y + 14, panel.Width - 32, 36), Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "This screen arrives in the next version (v0.1.1).", Theme.Body, new Rectangle(panel.X + 16, panel.Y + 60, panel.Width - 32, 60), Theme.Muted, TextFormatFlags.Left | TextFormatFlags.WordBreak);

            int bx = panel.Right + 20;
            Rectangle board = new Rectangle(bx, 90, Math.Max(200, ClientSize.Width - bx - 24), 300);
            Theme.FillRound(g, board, 14, Color.FromArgb(236, 236, 236), Color.Empty);
            TextRenderer.DrawText(g, "Keyboard picture arrives in v0.1.1", Theme.Bold, board, Color.FromArgb(90, 90, 90), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
