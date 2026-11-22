using System;
using System.Drawing;
using System.Windows.Forms;

namespace Voltrix
{
    // Sends DPI and report rate commands to the mouse.
    // These use the standard Logitech HID++ features (0x2201 DPI, 0x8060 report rate).
    static class MouseControl
    {
        static Hidpp OpenLink(out IntPtr handle, out string problem)
        {
            handle = IntPtr.Zero;
            problem = "";
            ScanResult s = AppState.Last;
            if (s == null || s.Mouse == null)
            {
                problem = "No mouse link yet. Open Devices and press Scan first.";
                return null;
            }
            if (s.GHubRunning)
            {
                problem = "Logitech software is running. Close it first, then Scan again.";
                return null;
            }
            handle = HidNative.OpenReadWrite(s.Mouse.Path);
            if (handle == IntPtr.Zero)
            {
                problem = "Could not open the mouse. Is another program using it?";
                return null;
            }
            return new Hidpp(handle, s.Mouse.Len >= 20, 20);
        }

        public static string SetDpi(int dpi, out string raw)
        {
            raw = "";
            IntPtr h;
            string problem;
            Hidpp link = OpenLink(out h, out problem);
            if (link == null)
            {
                return problem;
            }
            try
            {
                int idx = link.FeatureIndex(0x2201);
                if (idx == 0)
                {
                    return "This mouse has no DPI feature (0x2201). Send the scan report to Claude.";
                }
                byte[] r = link.Ask((byte)idx, 0x03, new byte[] { 0x00, (byte)(dpi >> 8), (byte)(dpi & 0xFF) });
                if (r == null)
                {
                    return "The mouse did not answer.";
                }
                raw = "reply " + Scanner.Hex(r, 12);
                if (link.IsError(r))
                {
                    return "The mouse refused that DPI. Send the line below to Claude.";
                }
                byte[] back = link.Ask((byte)idx, 0x02, new byte[] { 0x00 });
                if (back != null && !link.IsError(back) && back.Length > 6)
                {
                    int now = (back[5] << 8) | back[6];
                    return "Done. The mouse says it is now at " + now + " DPI.";
                }
                return "Sent.";
            }
            finally
            {
                HidNative.Close(h);
            }
        }

        public static string SetRate(int ms, out string raw)
        {
            raw = "";
            IntPtr h;
            string problem;
            Hidpp link = OpenLink(out h, out problem);
            if (link == null)
            {
                return problem;
            }
            try
            {
                int idx = link.FeatureIndex(0x8060);
                if (idx == 0)
                {
                    return "This mouse does not use the standard report rate feature (0x8060). The scan report shows what it has.";
                }
                byte[] r = link.Ask((byte)idx, 0x02, new byte[] { (byte)ms });
                if (r == null)
                {
                    return "The mouse did not answer.";
                }
                raw = "reply " + Scanner.Hex(r, 12);
                if (link.IsError(r))
                {
                    return "The mouse refused that report rate. Send the line below to Claude.";
                }
                return "Done. Report rate set to " + (1000 / ms) + " Hz.";
            }
            finally
            {
                HidNative.Close(h);
            }
        }
    }

    // The Performance tab for the mouse: DPI and report rate.
    class MousePanel : Panel
    {
        static readonly int[] Presets = new int[] { 400, 800, 1600, 3200, 6400 };
        static readonly int[] Rates = new int[] { 125, 250, 500, 1000 };

        VSlider slider;
        ChoiceBar presets;
        ChoiceBar rate;
        VButton apply;
        Label dpiValue;
        Label status;
        string raw = "";

        public MousePanel()
        {
            BackColor = Theme.Panel;
            DoubleBuffered = true;
            AutoScroll = true;
            AutoScrollMinSize = new Size(0, 470);

            AddLabel("Performance", Theme.Big, Theme.Text, 16, 14, 300, 36);
            AddLabel("Set how fast the pointer moves and how often the mouse reports.", Theme.Small, Theme.Muted, 16, 54, 460, 24);
            AddLabel("DPI", Theme.Bold, Theme.Text, 16, 96, 100, 24);
            dpiValue = AddLabel("", Theme.Bold, Theme.Accent, 300, 96, 160, 24);
            dpiValue.TextAlign = ContentAlignment.MiddleRight;

            slider = new VSlider(100, 25600, 50);
            slider.Value = AppState.Dpi;
            slider.SetBounds(16, 126, 440, 30);
            slider.Changed += delegate(object sender, EventArgs e)
            {
                AppState.Dpi = slider.Value;
                ShowDpi();
            };
            Controls.Add(slider);

            AddLabel("Quick DPI", Theme.Bold, Theme.Text, 16, 172, 200, 24);
            string[] p = new string[Presets.Length];
            for (int i = 0; i < Presets.Length; i++) p[i] = Presets[i].ToString();
            presets = new ChoiceBar(p);
            presets.SetBounds(16, 202, 440, 40);
            presets.Picked += delegate(object sender, EventArgs e)
            {
                slider.Value = Presets[presets.SelectedIndex];
                AppState.Dpi = slider.Value;
                ShowDpi();
            };
            Controls.Add(presets);

            AddLabel("Report rate (Hz)", Theme.Bold, Theme.Text, 16, 258, 300, 24);
            string[] r = new string[Rates.Length];
            for (int i = 0; i < Rates.Length; i++) r[i] = Rates[i].ToString();
            rate = new ChoiceBar(r);
            rate.SetBounds(16, 288, 440, 40);
            rate.SelectedIndex = RateIndex(AppState.RateMs);
            rate.Picked += delegate(object sender, EventArgs e)
            {
                AppState.RateMs = 1000 / Rates[rate.SelectedIndex];
            };
            Controls.Add(rate);

            apply = new VButton("Apply to mouse", true);
            apply.SetBounds(16, 348, 440, 44);
            apply.Click += delegate(object sender, EventArgs e) { ApplyAll(); };
            Controls.Add(apply);

            status = AddLabel("Pick values, then press Apply.", Theme.Small, Theme.Muted, 16, 404, 440, 60);
            ShowDpi();

            AppState.SettingsChanged += delegate(object sender, EventArgs e)
            {
                slider.Value = AppState.Dpi;
                rate.SelectedIndex = RateIndex(AppState.RateMs);
                ShowDpi();
            };
        }

        Label AddLabel(string text, Font f, Color c, int x, int y, int w, int h)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = f;
            l.ForeColor = c;
            l.BackColor = Theme.Panel;
            l.SetBounds(x, y, w, h);
            Controls.Add(l);
            return l;
        }

        static int RateIndex(int ms)
        {
            int hz = 1000 / Math.Max(1, ms);
            for (int i = 0; i < Rates.Length; i++)
            {
                if (Rates[i] == hz) return i;
            }
            return 3;
        }

        void ShowDpi()
        {
            dpiValue.Text = AppState.Dpi + " DPI";
            int match = -1;
            for (int i = 0; i < Presets.Length; i++)
            {
                if (Presets[i] == AppState.Dpi) match = i;
            }
            presets.SelectedIndex = match;
        }

        void ApplyAll()
        {
            string r1;
            string r2;
            string a = MouseControl.SetDpi(AppState.Dpi, out r1);
            string b = MouseControl.SetRate(AppState.RateMs, out r2);
            SettingsStore.Save();
            status.Text = "DPI: " + a + "\r\nReport rate: " + b + "\r\n" + r1 + "   " + r2;
        }
    }

    // The page for the mouse: Performance and Buttons tabs.
    class MousePage : Page
    {
        IconButton back;
        IconButton tabPerf;
        IconButton tabButtons;
        MousePanel perf;
        AssignPanel buttons;
        string currentTab = "perf";

        public event EventHandler BackClicked;

        public MousePage()
        {
            back = new IconButton("back");
            tabPerf = new IconButton("gear");
            tabButtons = new IconButton("plus");
            back.Click += delegate(object sender, EventArgs e)
            {
                if (BackClicked != null)
                {
                    BackClicked(this, EventArgs.Empty);
                }
            };
            tabPerf.Click += delegate(object sender, EventArgs e) { SelectTab("perf"); };
            tabButtons.Click += delegate(object sender, EventArgs e) { SelectTab("buttons"); };
            perf = new MousePanel();
            buttons = new AssignPanel();
            Controls.Add(back);
            Controls.Add(tabPerf);
            Controls.Add(tabButtons);
            Controls.Add(perf);
            Controls.Add(buttons);
            SelectTab("perf");
            DoLayout();
        }

        public void SelectTab(string tab)
        {
            currentTab = tab;
            tabPerf.Selected = tab == "perf";
            tabButtons.Selected = tab == "buttons";
            perf.Visible = tab == "perf";
            buttons.Visible = tab == "buttons";
            Invalidate();
        }

        void DoLayout()
        {
            back.Location = new Point(16, 14);
            tabPerf.Location = new Point(16, 90);
            tabButtons.Location = new Point(16, 148);
            int wide = Math.Max(300, Math.Min(ClientSize.Width - 104, 500));
            perf.SetBounds(80, 90, wide, Math.Max(300, ClientSize.Height - 120));
            buttons.SetBounds(80, 90, wide, Math.Max(300, ClientSize.Height - 120));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            DoLayout();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            TextRenderer.DrawText(e.Graphics, "G502 HERO", Theme.Title, new Rectangle(76, 12, 500, 50), Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }
}
