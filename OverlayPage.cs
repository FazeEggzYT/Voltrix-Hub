using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Voltrix
{
    // A slider with a name and a value shown on the right.
    class SliderRow
    {
        public VSlider S;
        public Label Val;
        public string Unit = "";

        public void Set(int v)
        {
            S.Value = v;
            Val.Text = v + Unit;
        }
    }

    // A scrolling column of controls, laid out top to bottom.
    class StackPanel : Panel
    {
        protected int y = 12;
        protected const int X = 16;
        protected const int CW = 500;

        public StackPanel()
        {
            BackColor = Theme.Bg;
            AutoScroll = true;
            DoubleBuffered = true;
        }

        protected Label AddText(string text, Font f, Color c, int h)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = f;
            l.ForeColor = c;
            l.BackColor = Theme.Bg;
            l.SetBounds(X, y, CW, h);
            Controls.Add(l);
            y += h + 6;
            return l;
        }

        protected VToggle AddToggle(string text, bool on, Action<bool> set)
        {
            VToggle t = new VToggle(text);
            t.BackColor = Theme.Bg;
            t.Checked = on;
            t.SetBounds(X, y, CW, 36);
            t.Changed += delegate(object sender, EventArgs e) { set(t.Checked); };
            Controls.Add(t);
            y += 40;
            return t;
        }

        protected SliderRow AddSlider(string label, int min, int max, int val, string unit, Action<int> set)
        {
            SliderRow row = new SliderRow();
            row.Unit = unit;
            Label name = new Label();
            name.Text = label;
            name.Font = Theme.Body;
            name.ForeColor = Theme.Text;
            name.BackColor = Theme.Bg;
            name.SetBounds(X, y, 300, 22);
            Controls.Add(name);
            row.Val = new Label();
            row.Val.Font = Theme.Bold;
            row.Val.ForeColor = Theme.Accent;
            row.Val.BackColor = Theme.Bg;
            row.Val.TextAlign = ContentAlignment.MiddleRight;
            row.Val.SetBounds(X + CW - 140, y, 140, 22);
            Controls.Add(row.Val);
            row.S = new VSlider(min, max, 1);
            row.S.BackColor = Theme.Bg;
            row.S.SetBounds(X, y + 24, CW, 30);
            row.S.Changed += delegate(object sender, EventArgs e)
            {
                row.Val.Text = row.S.Value + row.Unit;
                set(row.S.Value);
            };
            Controls.Add(row.S);
            row.Set(val);
            y += 58;
            return row;
        }

        protected ChoiceBar AddChoice(string[] items, int sel, Action<int> pick)
        {
            ChoiceBar c = new ChoiceBar(items);
            c.BackColor = Theme.Bg;
            c.SelectedIndex = sel;
            c.SetBounds(X, y, CW, 38);
            c.Picked += delegate(object sender, EventArgs e) { pick(c.SelectedIndex); };
            Controls.Add(c);
            y += 48;
            return c;
        }

        protected VButton AddButton(string text, bool primary, int x, int w, EventHandler click)
        {
            VButton b = new VButton(text, primary);
            b.BackColor = Theme.Bg;
            b.SetBounds(x, y, w, 40);
            b.Click += click;
            Controls.Add(b);
            return b;
        }

        protected ComboBox AddCombo(string[] items, int sel)
        {
            ComboBox c = new ComboBox();
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.FlatStyle = FlatStyle.Flat;
            c.BackColor = Theme.Panel2;
            c.ForeColor = Theme.Text;
            c.Font = Theme.Body;
            c.Items.AddRange(items);
            c.SelectedIndex = sel;
            c.SetBounds(X, y, CW, 28);
            Controls.Add(c);
            y += 38;
            return c;
        }

        protected void Gap(int n)
        {
            y += n;
        }

        protected void Done()
        {
            AutoScrollMinSize = new Size(0, y + 24);
        }

        public static bool PickColor(ref Color c)
        {
            using (ColorDialog dlg = new ColorDialog())
            {
                dlg.FullOpen = true;
                dlg.Color = c;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    c = Color.FromArgb(255, dlg.Color);
                    return true;
                }
            }
            return false;
        }

        public static string[] ScreenNames()
        {
            int n = Math.Max(1, Math.Min(4, Screen.AllScreens.Length));
            string[] s = new string[n];
            for (int i = 0; i < n; i++) s[i] = "Screen " + (i + 1);
            return s;
        }
    }

    // A mock screen: shows the HUD and the nine squares. Click a square to place a widget.
    class HudPreview : PaintControl
    {
        int hover = -1;

        public event EventHandler<CardEventArgs> CellClicked;

        public HudPreview()
        {
            Cursor = Cursors.Hand;
            Size = new Size(500, 281);
        }

        int CellAt(Point p)
        {
            int col = Math.Max(0, Math.Min(2, p.X * 3 / Math.Max(1, Width)));
            int row = Math.Max(0, Math.Min(2, p.Y * 3 / Math.Max(1, Height)));
            return row * 3 + col;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int c = CellAt(e.Location);
            if (c != hover)
            {
                hover = c;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && CellClicked != null)
            {
                CellClicked(this, new CardEventArgs(CellAt(e.Location).ToString()));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle all = new Rectangle(0, 0, Width, Height);
            using (LinearGradientBrush bg = new LinearGradientBrush(all, Color.FromArgb(34, 40, 56), Color.FromArgb(14, 16, 24), 60f))
            {
                g.FillRectangle(bg, all);
            }
            int cw = Width / 3;
            int ch = Height / 3;
            if (hover >= 0)
            {
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(40, Theme.Accent)))
                {
                    g.FillRectangle(hb, (hover % 3) * cw, (hover / 3) * ch, cw, ch);
                }
            }
            using (Pen grid = new Pen(Color.FromArgb(70, 255, 255, 255)))
            {
                grid.DashStyle = DashStyle.Dash;
                g.DrawLine(grid, cw, 0, cw, Height);
                g.DrawLine(grid, cw * 2, 0, cw * 2, Height);
                g.DrawLine(grid, 0, ch, Width, ch);
                g.DrawLine(grid, 0, ch * 2, Width, ch * 2);
            }
            for (int i = 0; i < 9; i++)
            {
                if (AppState.Hud.Cells[i].Length == 0)
                {
                    TextRenderer.DrawText(g, "+", Theme.Big, new Rectangle((i % 3) * cw, (i / 3) * ch, cw, ch), Color.FromArgb(90, 255, 255, 255), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
            HudRenderer.Draw(g, Size, AppState.Hud, true);
            using (Pen b = new Pen(Theme.Border))
            {
                g.DrawRectangle(b, 0, 0, Width - 1, Height - 1);
            }
        }
    }

    class CrossPreview : PaintControl
    {
        public CrossPreview()
        {
            Size = new Size(500, 170);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle all = new Rectangle(0, 0, Width, Height);
            using (LinearGradientBrush bg = new LinearGradientBrush(all, Color.FromArgb(70, 90, 70), Color.FromArgb(20, 30, 24), 90f))
            {
                g.FillRectangle(bg, all);
            }
            using (SolidBrush b = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
            {
                g.FillRectangle(b, 30, Height - 50, 120, 40);
                g.FillRectangle(b, Width - 170, 20, 140, 60);
            }
            AppState.Cross.Draw(g, Width / 2, Height / 2);
            using (Pen p = new Pen(Theme.Border))
            {
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            }
        }
    }

    // The HUD tab.
    class HudTab : StackPanel
    {
        HudPreview preview;
        ComboBox widget;
        SliderRow size;
        SliderRow opacity;
        SliderRow margin;
        VToggle pill;
        VToggle outline;
        VToggle bold;
        VToggle labels;
        VToggle onlyGame;
        VToggle show;
        ChoiceBar font;
        ChoiceBar monitor;
        ChoiceBar slot;
        ComboBox template;

        public HudTab()
        {
            AddText("HUD overlay", Theme.Big, Theme.Text, 34);
            show = AddToggle("Show the HUD   (Ctrl + Alt + H)", AppState.HudOn, delegate(bool on)
            {
                AppState.HudOn = on;
                Changed();
            });
            AddText("Pick a widget, then click a square on the screen below to put it there. Click the same square again to take it off.", Theme.Small, Theme.Muted, 36);
            widget = AddCombo(HudConfig.WidgetTitles, 0);
            preview = new HudPreview();
            preview.SetBounds(X, y, 500, 281);
            preview.CellClicked += delegate(object sender, CardEventArgs e)
            {
                int cell = int.Parse(e.Tab);
                AppState.Hud.Toggle(cell, HudConfig.WidgetKeys[widget.SelectedIndex]);
                Changed();
            };
            Controls.Add(preview);
            y += 291;
            AddButton("Clear all squares", false, X, 200, delegate(object sender, EventArgs e)
            {
                for (int i = 0; i < 9; i++) AppState.Hud.Cells[i] = "";
                Changed();
            });
            y += 52;

            AddText("Templates", Theme.Bold, Theme.Text, 24);
            template = AddCombo(HudConfig.TemplateNames, 0);
            AddButton("Use this template", true, X, 200, delegate(object sender, EventArgs e)
            {
                bool keepOn = AppState.HudOn;
                AppState.Hud = HudConfig.Template(HudConfig.TemplateNames[template.SelectedIndex]);
                Reload();
                Changed();
            });
            y += 52;

            AddText("Look", Theme.Bold, Theme.Text, 24);
            size = AddSlider("Text size", 8, 60, AppState.Hud.FontSize, " px", delegate(int v) { AppState.Hud.FontSize = v; Changed(); });
            opacity = AddSlider("Opacity", 10, 100, AppState.Hud.Opacity, " %", delegate(int v) { AppState.Hud.Opacity = v; Changed(); });
            margin = AddSlider("Distance from screen edge", 0, 200, AppState.Hud.Margin, " px", delegate(int v) { AppState.Hud.Margin = v; Changed(); });
            pill = AddToggle("Dark boxes behind the text", AppState.Hud.Pill, delegate(bool on) { AppState.Hud.Pill = on; Changed(); });
            outline = AddToggle("Outline on text (when there are no boxes)", AppState.Hud.Outline, delegate(bool on) { AppState.Hud.Outline = on; Changed(); });
            bold = AddToggle("Bold text", AppState.Hud.BoldText, delegate(bool on) { AppState.Hud.BoldText = on; Changed(); });
            labels = AddToggle("Show names (FPS, CPU...) before the numbers", AppState.Hud.ShowLabels, delegate(bool on) { AppState.Hud.ShowLabels = on; Changed(); });
            onlyGame = AddToggle("Only show while a game from my list runs", AppState.Hud.OnlyInGame, delegate(bool on) { AppState.Hud.OnlyInGame = on; Changed(); });
            AddButton("Text colour...", false, X, 200, delegate(object sender, EventArgs e)
            {
                Color c = AppState.Hud.TextColor;
                if (PickColor(ref c)) { AppState.Hud.TextColor = c; Changed(); }
            });
            AddButton("Name colour...", false, X + 212, 200, delegate(object sender, EventArgs e)
            {
                Color c = AppState.Hud.LabelColor;
                if (PickColor(ref c)) { AppState.Hud.LabelColor = c; Changed(); }
            });
            y += 52;
            AddText("Font", Theme.Bold, Theme.Text, 24);
            font = AddChoice(HudConfig.FontChoices, Math.Max(0, Array.IndexOf(HudConfig.FontChoices, AppState.Hud.FontName)), delegate(int i) { AppState.Hud.FontName = HudConfig.FontChoices[i]; Changed(); });
            AddText("Screen", Theme.Bold, Theme.Text, 24);
            monitor = AddChoice(ScreenNames(), Math.Min(AppState.Hud.Monitor, ScreenNames().Length - 1), delegate(int i) { AppState.Hud.Monitor = i; Changed(); });

            AddText("My templates (3 slots)", Theme.Bold, Theme.Text, 24);
            slot = AddChoice(new string[] { "Slot 1", "Slot 2", "Slot 3" }, 0, delegate(int i) { });
            AddButton("Save to slot", false, X, 200, delegate(object sender, EventArgs e)
            {
                AppState.HudSlots[slot.SelectedIndex] = AppState.Hud.ToText();
                SettingsStore.Save();
            });
            AddButton("Load slot", false, X + 212, 200, delegate(object sender, EventArgs e)
            {
                string t = AppState.HudSlots[slot.SelectedIndex];
                if (t.Length == 0) return;
                AppState.Hud = HudConfig.Parse(t);
                Reload();
                Changed();
            });
            y += 52;
            Done();
        }

        void Reload()
        {
            HudConfig h = AppState.Hud;
            size.Set(h.FontSize);
            opacity.Set(h.Opacity);
            margin.Set(h.Margin);
            pill.Checked = h.Pill;
            outline.Checked = h.Outline;
            bold.Checked = h.BoldText;
            labels.Checked = h.ShowLabels;
            onlyGame.Checked = h.OnlyInGame;
            font.SelectedIndex = Math.Max(0, Array.IndexOf(HudConfig.FontChoices, h.FontName));
            monitor.SelectedIndex = Math.Min(h.Monitor, ScreenNames().Length - 1);
        }

        void Changed()
        {
            SettingsStore.Save();
            Overlays.Apply();
            preview.Invalidate();
            AppState.RaiseSettingsChanged();
        }

        public void SyncShow()
        {
            show.Checked = AppState.HudOn;
        }
    }

    // The Crosshair tab.
    class CrossTab : StackPanel
    {
        CrossPreview preview;
        VToggle show;
        ChoiceBar style;
        SliderRow size;
        SliderRow thick;
        SliderRow gap;
        SliderRow dot;
        SliderRow opacity;
        SliderRow ox;
        SliderRow oy;
        VToggle cdot;
        VToggle outline;
        SwatchBar swatches;
        ChoiceBar monitor;
        ChoiceBar slot;
        ComboBox template;

        public CrossTab()
        {
            AddText("Crosshair overlay", Theme.Big, Theme.Text, 34);
            show = AddToggle("Show the crosshair   (Ctrl + Alt + C)", AppState.CrossOn, delegate(bool on)
            {
                AppState.CrossOn = on;
                Changed();
            });
            preview = new CrossPreview();
            preview.SetBounds(X, y, 500, 170);
            Controls.Add(preview);
            y += 182;

            AddText("Shape", Theme.Bold, Theme.Text, 24);
            style = AddChoice(CrossConfig.Styles, Math.Max(0, Array.IndexOf(CrossConfig.Styles, AppState.Cross.Style)), delegate(int i) { AppState.Cross.Style = CrossConfig.Styles[i]; Changed(); });
            size = AddSlider("Size", 1, 60, AppState.Cross.Size, " px", delegate(int v) { AppState.Cross.Size = v; Changed(); });
            thick = AddSlider("Thickness", 1, 12, AppState.Cross.Thickness, " px", delegate(int v) { AppState.Cross.Thickness = v; Changed(); });
            gap = AddSlider("Gap in the middle", 0, 40, AppState.Cross.Gap, " px", delegate(int v) { AppState.Cross.Gap = v; Changed(); });
            dot = AddSlider("Centre dot size", 1, 10, AppState.Cross.DotSize, " px", delegate(int v) { AppState.Cross.DotSize = v; Changed(); });
            opacity = AddSlider("Opacity", 10, 100, AppState.Cross.Opacity, " %", delegate(int v) { AppState.Cross.Opacity = v; Changed(); });
            ox = AddSlider("Move left / right", -300, 300, AppState.Cross.OffsetX, " px", delegate(int v) { AppState.Cross.OffsetX = v; Changed(); });
            oy = AddSlider("Move up / down", -300, 300, AppState.Cross.OffsetY, " px", delegate(int v) { AppState.Cross.OffsetY = v; Changed(); });
            cdot = AddToggle("Add a dot in the centre", AppState.Cross.CentreDot, delegate(bool on) { AppState.Cross.CentreDot = on; Changed(); });
            outline = AddToggle("Dark outline", AppState.Cross.Outline, delegate(bool on) { AppState.Cross.Outline = on; Changed(); });

            AddText("Colour", Theme.Bold, Theme.Text, 24);
            swatches = new SwatchBar();
            swatches.BackColor = Theme.Bg;
            swatches.Selected = AppState.Cross.Colour;
            swatches.SetBounds(X, y, CW, swatches.Height);
            swatches.Picked += delegate(object sender, EventArgs e)
            {
                AppState.Cross.Colour = swatches.Selected;
                Changed();
            };
            Controls.Add(swatches);
            y += swatches.Height + 12;
            AddButton("Custom colour...", false, X, 200, delegate(object sender, EventArgs e)
            {
                Color c = AppState.Cross.Colour;
                if (PickColor(ref c)) { AppState.Cross.Colour = c; swatches.Selected = c; Changed(); }
            });
            AddButton("Outline colour...", false, X + 212, 200, delegate(object sender, EventArgs e)
            {
                Color c = AppState.Cross.OutlineColour;
                if (PickColor(ref c)) { AppState.Cross.OutlineColour = c; Changed(); }
            });
            y += 52;

            AddText("Templates", Theme.Bold, Theme.Text, 24);
            template = AddCombo(CrossConfig.TemplateNames, 0);
            AddButton("Use this template", true, X, 200, delegate(object sender, EventArgs e)
            {
                AppState.Cross = CrossConfig.Template(CrossConfig.TemplateNames[template.SelectedIndex]);
                Reload();
                Changed();
            });
            y += 52;

            AddText("Screen", Theme.Bold, Theme.Text, 24);
            monitor = AddChoice(ScreenNames(), Math.Min(AppState.Cross.Monitor, ScreenNames().Length - 1), delegate(int i) { AppState.Cross.Monitor = i; Changed(); });

            AddText("My templates (3 slots)", Theme.Bold, Theme.Text, 24);
            slot = AddChoice(new string[] { "Slot 1", "Slot 2", "Slot 3" }, 0, delegate(int i) { });
            AddButton("Save to slot", false, X, 200, delegate(object sender, EventArgs e)
            {
                AppState.CrossSlots[slot.SelectedIndex] = AppState.Cross.ToText();
                SettingsStore.Save();
            });
            AddButton("Load slot", false, X + 212, 200, delegate(object sender, EventArgs e)
            {
                string t = AppState.CrossSlots[slot.SelectedIndex];
                if (t.Length == 0) return;
                AppState.Cross = CrossConfig.Parse(t);
                Reload();
                Changed();
            });
            y += 52;
            Done();
        }

        void Reload()
        {
            CrossConfig c = AppState.Cross;
            style.SelectedIndex = Math.Max(0, Array.IndexOf(CrossConfig.Styles, c.Style));
            size.Set(c.Size);
            thick.Set(c.Thickness);
            gap.Set(c.Gap);
            dot.Set(c.DotSize);
            opacity.Set(c.Opacity);
            ox.Set(c.OffsetX);
            oy.Set(c.OffsetY);
            cdot.Checked = c.CentreDot;
            outline.Checked = c.Outline;
            swatches.Selected = c.Colour;
            monitor.SelectedIndex = Math.Min(c.Monitor, ScreenNames().Length - 1);
        }

        void Changed()
        {
            SettingsStore.Save();
            Overlays.Apply();
            preview.Invalidate();
            AppState.RaiseSettingsChanged();
        }

        public void SyncShow()
        {
            show.Checked = AppState.CrossOn;
        }
    }

    // The FPS tab: explains PresentMon and shows its status.
    class FpsTab : StackPanel
    {
        Label status;
        Label path;
        Timer timer;

        public FpsTab()
        {
            AddText("FPS counter", Theme.Big, Theme.Text, 34);
            AddText("Windows does not let one program read another program's FPS. So VOLTRIX Hub uses PresentMon, a free and open-source tool, to measure it and shows the number on your HUD.", Theme.Body, Theme.Muted, 64);
            AddText("Set up (one time):", Theme.Bold, Theme.Text, 24);
            AddText("1. Press the first button and download the PresentMon console file (PresentMon-x.x.x-x64.exe).\r\n2. Press the second button and put that file in the folder that opens.\r\n3. Restart VOLTRIX Hub as administrator (the third button). PresentMon needs it.\r\n4. On the HUD tab, put the FPS widget in a square and turn the HUD on.", Theme.Body, Theme.Text, 110);
            AddButton("Open the PresentMon download page", true, X, CW, delegate(object sender, EventArgs e) { Open("https://github.com/GameTechDev/PresentMon/releases"); });
            y += 48;
            AddButton("Open the tools folder", false, X, CW, delegate(object sender, EventArgs e) { Open(FpsMonitor.ToolsDir()); });
            y += 48;
            AddButton("Restart VOLTRIX Hub as administrator", false, X, CW, delegate(object sender, EventArgs e) { RestartAdmin(); });
            y += 48;
            AddButton("Check FPS now", false, X, CW, delegate(object sender, EventArgs e)
            {
                FpsMonitor.Start();
                Refresh2();
            });
            y += 52;
            status = AddText("", Theme.Bold, Theme.Accent, 44);
            path = AddText("", Theme.Small, Theme.Muted, 40);
            Done();
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += delegate(object sender, EventArgs e) { if (Visible) Refresh2(); };
            timer.Start();
            Refresh2();
        }

        void Refresh2()
        {
            string tool = FpsMonitor.ToolPath();
            status.Text = FpsMonitor.Status + (FpsMonitor.Fps >= 0 ? "   Now: " + FpsMonitor.Fps + " FPS" : "");
            path.Text = (tool == null ? "PresentMon not found." : "Using " + tool) + "   Administrator: " + (FpsMonitor.IsAdmin() ? "yes" : "no");
        }

        static void Open(string target)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(target);
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
            catch (Exception)
            {
            }
        }

        void RestartAdmin()
        {
            try
            {
                string exe = Application.ExecutablePath;
                string cmd = "/c ping 127.0.0.1 -n 3 > nul & powershell -NoProfile -Command \"Start-Process -FilePath '" + exe + "' -Verb RunAs\"";
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
                AppState.Exiting = true;
                FindForm().Close();
            }
            catch (Exception)
            {
            }
        }
    }

    // The Overlay page: tabs for HUD, Crosshair and FPS.
    class OverlayPage : Page
    {
        ChoiceBar tabs;
        HudTab hud;
        CrossTab cross;
        FpsTab fps;

        public OverlayPage()
        {
            tabs = new ChoiceBar(new string[] { "HUD", "Crosshair", "FPS" });
            tabs.SelectedIndex = 0;
            tabs.Picked += delegate(object sender, EventArgs e) { ShowTab(); };
            hud = new HudTab();
            cross = new CrossTab();
            fps = new FpsTab();
            Controls.Add(tabs);
            Controls.Add(hud);
            Controls.Add(cross);
            Controls.Add(fps);
            AppState.SettingsChanged += delegate(object sender, EventArgs e)
            {
                hud.SyncShow();
                cross.SyncShow();
            };
            ShowTab();
            DoLayout();
        }

        void ShowTab()
        {
            hud.Visible = tabs.SelectedIndex == 0;
            cross.Visible = tabs.SelectedIndex == 1;
            fps.Visible = tabs.SelectedIndex == 2;
        }

        void DoLayout()
        {
            tabs.SetBounds(24, 96, 360, 40);
            int h = Math.Max(200, ClientSize.Height - 148 - 12);
            int w = Math.Max(300, ClientSize.Width - 48);
            hud.SetBounds(24, 148, w, h);
            cross.SetBounds(24, 148, w, h);
            fps.SetBounds(24, 148, w, h);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            DoLayout();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            TextRenderer.DrawText(e.Graphics, "Overlay", Theme.Title, new Rectangle(24, 24, 400, 50), Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }
}
