using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Voltrix
{
    static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(11, 14, 19);
        public static readonly Color Panel = Color.FromArgb(20, 25, 34);
        public static readonly Color Panel2 = Color.FromArgb(27, 34, 45);
        public static readonly Color Border = Color.FromArgb(38, 48, 64);
        public static readonly Color Text = Color.FromArgb(232, 237, 243);
        public static readonly Color Muted = Color.FromArgb(139, 151, 167);
        public static readonly Color Accent = Color.FromArgb(41, 171, 226);
        public static readonly Color AccentDark = Color.FromArgb(18, 53, 74);

        public static readonly Font Body = new Font("Segoe UI", 10f, FontStyle.Regular);
        public static readonly Font Bold = new Font("Segoe UI", 10f, FontStyle.Bold);
        public static readonly Font Small = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        public static readonly Font Big = new Font("Segoe UI", 16f, FontStyle.Bold);
        public static readonly Font Title = new Font("Segoe UI", 22f, FontStyle.Bold);

        public static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d < 2)
            {
                p.AddRectangle(r);
                return p;
            }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color fill, Color border)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rr = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
            using (GraphicsPath p = Round(rr, radius))
            {
                if (fill.A != 0)
                {
                    using (SolidBrush b = new SolidBrush(fill))
                    {
                        g.FillPath(b, p);
                    }
                }
                if (border.A != 0)
                {
                    using (Pen pen = new Pen(border))
                    {
                        g.DrawPath(pen, p);
                    }
                }
            }
        }
    }
}
