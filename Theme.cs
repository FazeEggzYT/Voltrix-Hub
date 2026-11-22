using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Voltrix
{
    static class Theme
    {
        public static Color Bg;
        public static Color Panel;
        public static Color Panel2;
        public static Color Border;
        public static Color Text;
        public static Color Muted;
        public static Color Accent;
        public static Color AccentDark;

        public static Font Body;
        public static Font Bold;
        public static Font Small;
        public static Font Big;
        public static Font Title;

        public static readonly string[] Names = new string[] { "Voltrix Blue", "Midnight", "Graphite", "Forest", "Crimson" };
        public static readonly string[] FontNames = new string[] { "Segoe UI", "Bahnschrift", "Calibri", "Verdana" };

        static Theme()
        {
            Apply("Voltrix Blue", Color.FromArgb(41, 171, 226), "Segoe UI");
        }

        // Sets all colours and fonts. Call once at start, before any window is made.
        public static void Apply(string name, Color accent, string font)
        {
            switch (name)
            {
                case "Midnight":
                    Bg = Color.FromArgb(10, 9, 20); Panel = Color.FromArgb(19, 17, 36); Panel2 = Color.FromArgb(27, 24, 50);
                    Border = Color.FromArgb(44, 40, 76); Text = Color.FromArgb(236, 234, 248); Muted = Color.FromArgb(150, 146, 180);
                    break;
                case "Graphite":
                    Bg = Color.FromArgb(14, 14, 15); Panel = Color.FromArgb(24, 24, 26); Panel2 = Color.FromArgb(33, 33, 36);
                    Border = Color.FromArgb(52, 52, 57); Text = Color.FromArgb(236, 236, 238); Muted = Color.FromArgb(150, 150, 156);
                    break;
                case "Forest":
                    Bg = Color.FromArgb(9, 16, 13); Panel = Color.FromArgb(16, 27, 22); Panel2 = Color.FromArgb(22, 37, 30);
                    Border = Color.FromArgb(36, 58, 48); Text = Color.FromArgb(230, 242, 236); Muted = Color.FromArgb(134, 160, 146);
                    break;
                case "Crimson":
                    Bg = Color.FromArgb(17, 10, 12); Panel = Color.FromArgb(30, 18, 21); Panel2 = Color.FromArgb(42, 25, 29);
                    Border = Color.FromArgb(66, 40, 46); Text = Color.FromArgb(244, 232, 234); Muted = Color.FromArgb(168, 138, 144);
                    break;
                default:
                    Bg = Color.FromArgb(11, 14, 19); Panel = Color.FromArgb(20, 25, 34); Panel2 = Color.FromArgb(27, 34, 45);
                    Border = Color.FromArgb(38, 48, 64); Text = Color.FromArgb(232, 237, 243); Muted = Color.FromArgb(139, 151, 167);
                    break;
            }
            Accent = accent;
            AccentDark = Color.FromArgb((accent.R * 2 + Bg.R * 8) / 10, (accent.G * 2 + Bg.G * 8) / 10, (accent.B * 2 + Bg.B * 8) / 10);

            string fam = font;
            Body = new Font(fam, 10f, FontStyle.Regular);
            Bold = new Font(fam, 10f, FontStyle.Bold);
            Small = new Font(fam, 8.5f, FontStyle.Regular);
            Big = new Font(fam, 16f, FontStyle.Bold);
            Title = new Font(fam, 22f, FontStyle.Bold);
        }

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
