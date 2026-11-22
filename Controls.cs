using System;
using System.Drawing;
using System.Windows.Forms;

namespace Voltrix
{
    // Base for controls we paint ourselves.
    class PaintControl : Control
    {
        public PaintControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, false);
        }
    }

    // A button in the left sidebar: icon plus text.
    class NavButton : PaintControl
    {
        string icon;
        bool selected;
        bool hover;

        public NavButton(string text, string iconName)
        {
            Text = text;
            icon = iconName;
            Cursor = Cursors.Hand;
            Height = 44;
        }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Panel);
            if (selected)
            {
                Theme.FillRound(g, new Rectangle(0, 0, Width, Height), 8, Theme.Panel2, Color.Empty);
            }
            else if (hover)
            {
                Theme.FillRound(g, new Rectangle(0, 0, Width, Height), 8, Color.FromArgb(24, 30, 40), Color.Empty);
            }
            Color c = selected ? Theme.Accent : Theme.Muted;
            Icons.Draw(g, icon, new Rectangle(14, (Height - 22) / 2, 22, 22), c);
            TextRenderer.DrawText(g, Text, Theme.Bold, new Rectangle(46, 0, Width - 46, Height), selected ? Theme.Text : Theme.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }

    // A square icon button (back arrow, device tabs).
    class IconButton : PaintControl
    {
        string icon;
        bool selected;
        bool hover;

        public IconButton(string iconName)
        {
            icon = iconName;
            Cursor = Cursors.Hand;
            Size = new Size(48, 48);
        }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Bg);
            Color fill = selected ? Theme.Accent : (hover ? Theme.Panel2 : Color.Empty);
            if (fill.A != 0)
            {
                Theme.FillRound(g, new Rectangle(0, 0, Width, Height), 8, fill, Color.Empty);
            }
            Color c = selected ? Color.FromArgb(4, 32, 46) : Theme.Text;
            int s = Math.Min(Width, Height) / 2;
            Icons.Draw(g, icon, new Rectangle((Width - s) / 2, (Height - s) / 2, s, s), c);
        }
    }

    // The VOLTRIX logo and name at the top of the sidebar.
    class LogoBox : PaintControl
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Panel);
            Icons.DrawLogo(g, new Rectangle(16, (Height - 28) / 2, 28, 28), Theme.Accent);
            TextRenderer.DrawText(g, "V O L T R I X", Theme.Bold, new Rectangle(52, 0, Width - 52, Height), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }

    class CardEventArgs : EventArgs
    {
        string tab;

        public CardEventArgs(string tabName)
        {
            tab = tabName;
        }

        public string Tab
        {
            get { return tab; }
        }
    }

    // A device card on the Devices page.
    class DeviceCard : PaintControl
    {
        string title;
        string subtitle;
        bool isKeyboard;
        bool enabledCard;
        bool hover;
        public bool ShowQuick = true;

        public event EventHandler<CardEventArgs> Opened;

        public DeviceCard(string titleText, string subtitleText, bool keyboard, bool enabled)
        {
            title = titleText;
            subtitle = subtitleText;
            isKeyboard = keyboard;
            enabledCard = enabled;
            Height = 112;
            if (enabled)
            {
                Cursor = Cursors.Hand;
            }
        }

        public void SetEnabled(bool on)
        {
            enabledCard = on;
            Cursor = on ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        public void SetSubtitle(string text)
        {
            subtitle = text;
            Invalidate();
        }

        Rectangle LightingRect()
        {
            return new Rectangle(Width - 16 - 46 * 2 - 8, (Height - 46) / 2, 46, 46);
        }

        Rectangle GameRect()
        {
            return new Rectangle(Width - 16 - 46, (Height - 46) / 2, 46, 46);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!enabledCard || e.Button != MouseButtons.Left || Opened == null)
            {
                return;
            }
            if (ShowQuick && LightingRect().Contains(e.Location))
            {
                Opened(this, new CardEventArgs("light"));
            }
            else if (ShowQuick && GameRect().Contains(e.Location))
            {
                Opened(this, new CardEventArgs("game"));
            }
            else
            {
                Opened(this, new CardEventArgs("light"));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Bg);
            Color fill = (enabledCard && hover) ? Theme.Panel2 : Theme.Panel;
            Theme.FillRound(g, new Rectangle(0, 0, Width, Height), 14, fill, Theme.Border);

            Rectangle pic = new Rectangle(16, 16, 170, Height - 32);
            if (isKeyboard)
            {
                Icons.DrawMiniKeyboard(g, pic);
            }
            else
            {
                Icons.DrawMiniMouse(g, pic, Theme.Muted);
            }

            Color titleColor = enabledCard ? Theme.Text : Theme.Muted;
            TextRenderer.DrawText(g, title, Theme.Big, new Rectangle(206, 22, Width - 206 - 130, 32), titleColor, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, subtitle, Theme.Body, new Rectangle(206, 56, Width - 206 - 130, 40), Theme.Muted, TextFormatFlags.Left | TextFormatFlags.WordBreak);

            if (enabledCard && ShowQuick)
            {
                Rectangle l = LightingRect();
                Rectangle m = GameRect();
                Theme.FillRound(g, l, 10, Theme.Panel2, Theme.Border);
                Theme.FillRound(g, m, 10, Theme.Panel2, Theme.Border);
                Icons.Draw(g, "sun", new Rectangle(l.X + 11, l.Y + 11, 24, 24), Theme.Text);
                Icons.Draw(g, "game", new Rectangle(m.X + 11, m.Y + 11, 24, 24), Theme.Text);
            }
        }
    }
}
