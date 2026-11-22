using System;
using System.Drawing;
using System.Windows.Forms;

namespace Voltrix
{
    // A rounded text button. Primary buttons use the accent colour.
    class VButton : PaintControl
    {
        bool primary;
        bool hover;

        public VButton(string text, bool isPrimary)
        {
            Text = text;
            primary = isPrimary;
            Cursor = Cursors.Hand;
            Height = 40;
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
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            Color fill;
            Color text;
            if (primary)
            {
                fill = hover ? Color.FromArgb(70, 190, 240) : Theme.Accent;
                text = Color.FromArgb(4, 32, 46);
            }
            else
            {
                fill = hover ? Color.FromArgb(36, 45, 60) : Theme.Panel2;
                text = Theme.Text;
            }
            Theme.FillRound(g, new Rectangle(0, 0, Width, Height), 10, fill, primary ? Color.Empty : Theme.Border);
            TextRenderer.DrawText(g, Text, Theme.Bold, new Rectangle(0, 0, Width, Height), text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // A row of colour squares. Click one to pick it.
    class SwatchBar : PaintControl
    {
        public static readonly Color[] Colors = new Color[]
        {
            Color.FromArgb(255, 255, 255), Color.FromArgb(255, 40, 40), Color.FromArgb(255, 130, 20), Color.FromArgb(255, 220, 30),
            Color.FromArgb(60, 220, 80), Color.FromArgb(20, 220, 200), Color.FromArgb(41, 171, 226), Color.FromArgb(70, 90, 255),
            Color.FromArgb(160, 80, 255), Color.FromArgb(255, 60, 200)
        };

        const int Size = 40;
        const int Gap = 8;
        const int PerRow = 5;
        Color selected = Color.White;

        public event EventHandler Picked;

        public SwatchBar()
        {
            Cursor = Cursors.Hand;
            Height = 2 * Size + Gap;
        }

        public Color Selected
        {
            get { return selected; }
            set { selected = value; Invalidate(); }
        }

        Rectangle CellRect(int i)
        {
            return new Rectangle((i % PerRow) * (Size + Gap), (i / PerRow) * (Size + Gap), Size, Size);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            for (int i = 0; i < Colors.Length; i++)
            {
                if (CellRect(i).Contains(e.Location))
                {
                    selected = Colors[i];
                    Invalidate();
                    if (Picked != null)
                    {
                        Picked(this, EventArgs.Empty);
                    }
                    return;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Panel);
            for (int i = 0; i < Colors.Length; i++)
            {
                Rectangle r = CellRect(i);
                bool on = Colors[i].ToArgb() == selected.ToArgb();
                Theme.FillRound(g, r, 8, Colors[i], on ? Theme.Text : Theme.Border);
                if (on)
                {
                    Rectangle inner = new Rectangle(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6);
                    Theme.FillRound(g, inner, 10, Color.Empty, Theme.Accent);
                }
            }
        }
    }
}
