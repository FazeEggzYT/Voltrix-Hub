using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Voltrix
{
    // Simple line icons drawn with GDI+. Everything is drawn on a 24x24 grid and scaled.
    static class Icons
    {
        static PointF Pt(Rectangle r, float x, float y)
        {
            float s = r.Width / 24f;
            return new PointF(r.X + x * s, r.Y + y * s);
        }

        static RectangleF Rc(Rectangle r, float x, float y, float w, float h)
        {
            float s = r.Width / 24f;
            return new RectangleF(r.X + x * s, r.Y + y * s, w * s, h * s);
        }

        static void Line(Graphics g, Pen p, Rectangle r, float x1, float y1, float x2, float y2)
        {
            g.DrawLine(p, Pt(r, x1, y1), Pt(r, x2, y2));
        }

        public static void Draw(Graphics g, string name, Rectangle r, Color c)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = r.Width / 24f;
            using (Pen p = new Pen(c, 1.8f * scale))
            using (SolidBrush b = new SolidBrush(c))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;

                if (name == "kbd")
                {
                    RectangleF k = Rc(r, 2, 6, 20, 12);
                    Rectangle ki = new Rectangle((int)k.X, (int)k.Y, (int)k.Width, (int)k.Height);
                    using (GraphicsPath path = Theme.Round(ki, (int)(2 * scale)))
                    {
                        g.DrawPath(p, path);
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        RectangleF d = Rc(r, 5.2f + i * 4f, 9.2f, 1.6f, 1.6f);
                        g.FillEllipse(b, d);
                    }
                    Line(g, p, r, 7, 14, 17, 14);
                }
                else if (name == "users")
                {
                    g.DrawEllipse(p, Rc(r, 6, 5, 6, 6));
                    g.DrawArc(p, Rc(r, 3, 14, 12, 10), 180, 180);
                    g.DrawEllipse(p, Rc(r, 14.5f, 6, 4.5f, 4.5f));
                    g.DrawArc(p, Rc(r, 15, 14, 7, 7), 200, 140);
                }
                else if (name == "down")
                {
                    Line(g, p, r, 12, 3, 12, 15);
                    Line(g, p, r, 7, 10, 12, 15);
                    Line(g, p, r, 17, 10, 12, 15);
                    Line(g, p, r, 5, 20, 19, 20);
                }
                else if (name == "gear")
                {
                    g.DrawEllipse(p, Rc(r, 7, 7, 10, 10));
                    for (int i = 0; i < 8; i++)
                    {
                        double a = i * Math.PI / 4.0;
                        float x1 = 12f + (float)Math.Cos(a) * 6.5f;
                        float y1 = 12f + (float)Math.Sin(a) * 6.5f;
                        float x2 = 12f + (float)Math.Cos(a) * 9.5f;
                        float y2 = 12f + (float)Math.Sin(a) * 9.5f;
                        Line(g, p, r, x1, y1, x2, y2);
                    }
                }
                else if (name == "sun")
                {
                    g.DrawEllipse(p, Rc(r, 8, 8, 8, 8));
                    for (int i = 0; i < 8; i++)
                    {
                        double a = i * Math.PI / 4.0;
                        float x1 = 12f + (float)Math.Cos(a) * 6.5f;
                        float y1 = 12f + (float)Math.Sin(a) * 6.5f;
                        float x2 = 12f + (float)Math.Cos(a) * 9.5f;
                        float y2 = 12f + (float)Math.Sin(a) * 9.5f;
                        Line(g, p, r, x1, y1, x2, y2);
                    }
                }
                else if (name == "plus")
                {
                    RectangleF k = Rc(r, 4, 4, 16, 16);
                    g.DrawRectangle(p, k.X, k.Y, k.Width, k.Height);
                    Line(g, p, r, 12, 8, 12, 16);
                    Line(g, p, r, 8, 12, 16, 12);
                }
                else if (name == "game")
                {
                    RectangleF k = Rc(r, 2, 7, 20, 11);
                    Rectangle ki = new Rectangle((int)k.X, (int)k.Y, (int)k.Width, (int)k.Height);
                    using (GraphicsPath path = Theme.Round(ki, (int)(5 * scale)))
                    {
                        g.DrawPath(p, path);
                    }
                    Line(g, p, r, 7, 10.5f, 7, 14.5f);
                    Line(g, p, r, 5, 12.5f, 9, 12.5f);
                    g.FillEllipse(b, Rc(r, 15f, 11f, 2f, 2f));
                    g.FillEllipse(b, Rc(r, 18f, 12.5f, 2f, 2f));
                }
                else if (name == "back")
                {
                    Line(g, p, r, 19, 12, 5, 12);
                    Line(g, p, r, 12, 5, 5, 12);
                    Line(g, p, r, 12, 19, 5, 12);
                }
            }
        }

        // The VOLTRIX logo: a V with a bolt.
        public static void DrawLogo(Graphics g, Rectangle r, Color c)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            PointF[] v = new PointF[]
            {
                Pt(r, 2, 3), Pt(r, 9, 3), Pt(r, 12, 13), Pt(r, 15, 3), Pt(r, 22, 3), Pt(r, 14, 22), Pt(r, 10, 22)
            };
            using (SolidBrush b = new SolidBrush(c))
            {
                g.FillPolygon(b, v);
            }
            PointF[] bolt = new PointF[]
            {
                Pt(r, 13, 6), Pt(r, 9.5f, 12.5f), Pt(r, 12, 12.5f), Pt(r, 11, 17.5f), Pt(r, 14.5f, 11), Pt(r, 12, 11)
            };
            using (SolidBrush w = new SolidBrush(Color.White))
            {
                g.FillPolygon(w, bolt);
            }
        }

        // A small keyboard picture used on the device card.
        public static void DrawMiniKeyboard(Graphics g, Rectangle r)
        {
            Theme.FillRound(g, r, 8, Color.FromArgb(10, 10, 12), Theme.Border);
            int[] counts = new int[] { 14, 15, 15, 14, 12 };
            int padX = r.Width / 20;
            int padY = r.Height / 8;
            int rowH = (r.Height - padY * 2) / 5;
            int gap = 2;
            using (SolidBrush b = new SolidBrush(Theme.Panel2))
            {
                for (int row = 0; row < 5; row++)
                {
                    int n = counts[row];
                    int w = (r.Width - padX * 2 - gap * (n - 1)) / n;
                    for (int i = 0; i < n; i++)
                    {
                        Rectangle k = new Rectangle(r.X + padX + i * (w + gap), r.Y + padY + row * rowH, w, rowH - gap);
                        g.FillRectangle(b, k);
                    }
                }
            }
        }

        // A small mouse picture used on the (disabled) mouse card.
        public static void DrawMiniMouse(Graphics g, Rectangle r, Color c)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int w = r.Height * 2 / 3;
            Rectangle body = new Rectangle(r.X + (r.Width - w) / 2, r.Y + 2, w, r.Height - 4);
            using (Pen p = new Pen(c, 2f))
            {
                using (GraphicsPath path = Theme.Round(body, w / 2))
                {
                    g.DrawPath(p, path);
                }
                g.DrawLine(p, body.X + w / 2, body.Y, body.X + w / 2, body.Y + body.Height / 3);
                g.DrawLine(p, body.X, body.Y + body.Height / 3, body.Right, body.Y + body.Height / 3);
            }
        }
    }
}
