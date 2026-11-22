using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Voltrix
{
    // The start-up screen: logo, name, what it is doing, and the devices it found.
    class SplashForm : Form
    {
        string step = "Starting...";
        float goal;
        float shown;
        string keyboardName = "";
        string mouseName = "";
        bool devicesKnown;

        public SplashForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 340);
            BackColor = Theme.Bg;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
        }

        public void SetStep(string text, float target)
        {
            step = text;
            goal = target;
        }

        public void SetDevices(string keyboard, string mouse)
        {
            keyboardName = keyboard;
            mouseName = mouse;
            devicesKnown = true;
        }

        // Keeps the window alive and animates the bar for a number of milliseconds.
        public void Pump(int ms)
        {
            DateTime end = DateTime.Now.AddMilliseconds(ms);
            while (DateTime.Now < end)
            {
                shown += (goal - shown) * 0.12f;
                Invalidate();
                Application.DoEvents();
                System.Threading.Thread.Sleep(15);
            }
        }

        public void FadeAndClose()
        {
            goal = 1f;
            for (int i = 0; i < 10; i++)
            {
                Opacity = 1.0 - i / 10.0;
                Application.DoEvents();
                System.Threading.Thread.Sleep(18);
            }
            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle all = new Rectangle(0, 0, Width, Height);
            using (LinearGradientBrush bg = new LinearGradientBrush(all, Theme.Panel, Theme.Bg, 90f))
            {
                g.FillRectangle(bg, all);
            }
            using (Pen border = new Pen(Theme.Border))
            {
                g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            }
            Icons.DrawLogo(g, new Rectangle(Width / 2 - 36, 40, 72, 72), Theme.Accent);
            TextRenderer.DrawText(g, "V O L T R I X   H U B", Theme.Big, new Rectangle(0, 124, Width, 36), Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "Version " + AppInfo.Full, Theme.Small, new Rectangle(0, 160, Width, 22), Theme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            Rectangle track = new Rectangle(80, 214, Width - 160, 6);
            Theme.FillRound(g, track, 3, Theme.Panel2, Color.Empty);
            int fill = (int)(track.Width * Math.Max(0f, Math.Min(1f, shown)));
            if (fill > 6)
            {
                Theme.FillRound(g, new Rectangle(track.X, track.Y, fill, track.Height), 3, Theme.Accent, Color.Empty);
            }
            TextRenderer.DrawText(g, step, Theme.Body, new Rectangle(0, 228, Width, 26), Theme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            if (devicesKnown)
            {
                string k = keyboardName.Length > 0 ? keyboardName : "Keyboard not found";
                string m = mouseName.Length > 0 ? mouseName : "Mouse not found";
                TextRenderer.DrawText(g, k, Theme.Bold, new Rectangle(0, 266, Width, 24), keyboardName.Length > 0 ? Theme.Text : Theme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, m, Theme.Bold, new Rectangle(0, 290, Width, 24), mouseName.Length > 0 ? Theme.Text : Theme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }
}
