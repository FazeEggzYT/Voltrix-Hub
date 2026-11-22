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

    // An on/off switch with a label.
    class VToggle : PaintControl
    {
        bool isOn;
        bool hover;

        public event EventHandler Changed;

        public VToggle(string text)
        {
            Text = text;
            Cursor = Cursors.Hand;
            Height = 36;
        }

        public bool Checked
        {
            get { return isOn; }
            set { isOn = value; Invalidate(); }
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
            if (e.Button != MouseButtons.Left) return;
            isOn = !isOn;
            Invalidate();
            if (Changed != null)
            {
                Changed(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Panel);
            TextRenderer.DrawText(g, Text, Theme.Body, new Rectangle(0, 0, Width - 60, Height), hover ? Theme.Text : Color.FromArgb(200, 208, 220), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            Rectangle track = new Rectangle(Width - 50, (Height - 24) / 2, 46, 24);
            Theme.FillRound(g, track, 12, isOn ? Theme.Accent : Color.FromArgb(52, 62, 78), Color.Empty);
            int knob = 18;
            int kx = isOn ? track.Right - knob - 4 : track.X + 4;
            g.FillEllipse(Brushes.White, kx, track.Y + 3, knob, knob);
        }
    }

    // A row of choices, one selected (like a segmented switch).
    class ChoiceBar : PaintControl
    {
        string[] items;
        int selected = -1;

        public event EventHandler Picked;

        public ChoiceBar(string[] choices)
        {
            items = choices;
            Cursor = Cursors.Hand;
            Height = 40;
        }

        public int SelectedIndex
        {
            get { return selected; }
            set { selected = value; Invalidate(); }
        }

        Rectangle Cell(int i)
        {
            int gap = 6;
            int w = (Width - gap * (items.Length - 1)) / items.Length;
            return new Rectangle(i * (w + gap), 0, w, Height);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            for (int i = 0; i < items.Length; i++)
            {
                if (Cell(i).Contains(e.Location))
                {
                    selected = i;
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
            for (int i = 0; i < items.Length; i++)
            {
                Rectangle r = Cell(i);
                bool on = i == selected;
                Theme.FillRound(g, r, 8, on ? Theme.Accent : Theme.Panel2, on ? Color.Empty : Theme.Border);
                TextRenderer.DrawText(g, items[i], Theme.Bold, r, on ? Color.FromArgb(4, 32, 46) : Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    // A horizontal slider for whole numbers.
    class VSlider : PaintControl
    {
        int min = 0;
        int max = 100;
        int step = 1;
        int val;
        bool drag;

        public event EventHandler Changed;

        public VSlider(int minimum, int maximum, int stepSize)
        {
            min = minimum;
            max = maximum;
            step = stepSize;
            val = minimum;
            Cursor = Cursors.Hand;
            Height = 30;
        }

        public int Value
        {
            get { return val; }
            set
            {
                int v = value;
                if (v < min) v = min;
                if (v > max) v = max;
                val = v;
                Invalidate();
            }
        }

        void SetFromX(int x)
        {
            int left = 10;
            int span = Math.Max(1, Width - 20);
            double f = (double)(x - left) / span;
            if (f < 0) f = 0;
            if (f > 1) f = 1;
            int raw = min + (int)Math.Round(f * (max - min));
            int snapped = min + ((raw - min + step / 2) / step) * step;
            if (snapped > max) snapped = max;
            if (snapped != val)
            {
                val = snapped;
                Invalidate();
                if (Changed != null)
                {
                    Changed(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            drag = true;
            SetFromX(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (drag) SetFromX(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            drag = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Panel);
            int left = 10;
            int span = Math.Max(1, Width - 20);
            int cy = Height / 2;
            Theme.FillRound(g, new Rectangle(left, cy - 3, span, 6), 3, Theme.Panel2, Theme.Border);
            int kx = left + (int)((double)(val - min) / Math.Max(1, max - min) * span);
            Theme.FillRound(g, new Rectangle(left, cy - 3, Math.Max(2, kx - left), 6), 3, Theme.Accent, Color.Empty);
            g.FillEllipse(Brushes.White, kx - 9, cy - 9, 18, 18);
        }
    }
}
