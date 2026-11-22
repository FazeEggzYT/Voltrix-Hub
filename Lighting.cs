using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Voltrix
{
    // Things the whole app shares.
    static class AppState
    {
        public static ScanResult Last;
        public static Color LightColor = Color.FromArgb(41, 171, 226);
        public static event EventHandler LightChanged;

        public static void RaiseLightChanged()
        {
            if (LightChanged != null)
            {
                LightChanged(null, EventArgs.Empty);
            }
        }
    }

    // Saves small settings in %APPDATA%\VOLTRIX Hub\settings.txt as name=value lines.
    static class SettingsStore
    {
        static string FilePath()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VOLTRIX Hub");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "settings.txt");
        }

        public static void Load()
        {
            try
            {
                string f = FilePath();
                if (!File.Exists(f)) return;
                foreach (string line in File.ReadAllLines(f))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    if (key == "lightColor")
                    {
                        int argb;
                        if (int.TryParse(val, out argb))
                        {
                            AppState.LightColor = Color.FromArgb(255, Color.FromArgb(argb));
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllLines(FilePath(), new string[] { "lightColor=" + AppState.LightColor.ToArgb() });
            }
            catch (Exception)
            {
            }
        }
    }

    // Sends a lighting command to the keyboard.
    // EXPERIMENTAL: the packet follows the style the open-source keyboard tools use for
    // the older Logitech PRO/G810 family. The first test on real hardware confirms it.
    static class KeyboardLight
    {
        // Sets every key to one colour. Returns a plain-English result and fills the raw log.
        public static string SetAll(Color c, out string raw)
        {
            raw = "";
            ScanResult s = AppState.Last;
            if (s == null || s.LinkPath.Length == 0)
            {
                return "No keyboard link yet. Open Devices and press Scan first.";
            }
            if (s.GHubRunning)
            {
                return "Logitech software is running. Close it first, then Scan again.";
            }
            if (s.LinkLen < 20)
            {
                return "This keyboard link has no long reports. Send the scan report to Claude.";
            }
            IntPtr h = HidNative.OpenReadWrite(s.LinkPath);
            if (h == IntPtr.Zero)
            {
                return "Could not open the keyboard. Is another program using it?";
            }
            try
            {
                byte[] effect = new byte[20];
                effect[0] = 0x11;
                effect[1] = 0xFF;
                effect[2] = 0x0D;
                effect[3] = 0x3C;
                effect[4] = 0x00; // part: keys
                effect[5] = 0x01; // effect: fixed colour
                effect[6] = c.R;
                effect[7] = c.G;
                effect[8] = c.B;
                effect[9] = 0x02;
                if (!HidNative.Write(h, effect))
                {
                    return "The keyboard did not accept the command.";
                }
                byte[] reply = HidNative.Read(h, 20, 400);
                raw = "sent " + Scanner.Hex(effect, 12) + " | reply " + (reply == null ? "none" : Scanner.Hex(reply, 12));

                byte[] commit = new byte[20];
                commit[0] = 0x11;
                commit[1] = 0xFF;
                commit[2] = 0x0C;
                commit[3] = 0x5A;
                HidNative.Write(h, commit);
                HidNative.Read(h, 20, 200);

                if (reply != null && (reply[2] == 0xFF || reply[2] == 0x8F))
                {
                    return "The keyboard said this command is not supported. Send the line below to Claude.";
                }
                return "Sent. Look at your keyboard.";
            }
            finally
            {
                HidNative.Close(h);
            }
        }
    }

    // The Lighting tab content: colour choice, apply and restore.
    class LightingPanel : Panel
    {
        SwatchBar swatches;
        VButton custom;
        VButton apply;
        VButton restore;
        string status = "Pick a colour, then press Apply.";
        string raw = "";

        public LightingPanel()
        {
            BackColor = Theme.Panel;
            DoubleBuffered = true;

            swatches = new SwatchBar();
            swatches.Selected = AppState.LightColor;
            swatches.Picked += delegate(object sender, EventArgs e)
            {
                AppState.LightColor = swatches.Selected;
                SettingsStore.Save();
                AppState.RaiseLightChanged();
            };
            custom = new VButton("Custom colour...", false);
            custom.Click += delegate(object sender, EventArgs e)
            {
                using (ColorDialog dlg = new ColorDialog())
                {
                    dlg.FullOpen = true;
                    dlg.Color = AppState.LightColor;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        AppState.LightColor = Color.FromArgb(255, dlg.Color);
                        swatches.Selected = AppState.LightColor;
                        SettingsStore.Save();
                        AppState.RaiseLightChanged();
                    }
                }
            };
            apply = new VButton("Apply to keyboard", true);
            apply.Click += delegate(object sender, EventArgs e)
            {
                status = KeyboardLight.SetAll(AppState.LightColor, out raw);
                Invalidate();
            };
            restore = new VButton("Restore Logitech lighting", false);
            restore.Click += delegate(object sender, EventArgs e)
            {
                status = "To get Logitech lighting back: unplug the keyboard and plug it in again, or open G HUB. VOLTRIX never changes the keyboard's saved settings.";
                raw = "";
                Invalidate();
            };
            Controls.Add(swatches);
            Controls.Add(custom);
            Controls.Add(apply);
            Controls.Add(restore);
            DoLayout();
        }

        void DoLayout()
        {
            int w = Width - 32;
            swatches.SetBounds(16, 112, w, swatches.Height);
            custom.SetBounds(16, 112 + swatches.Height + 14, w, 40);
            apply.SetBounds(16, 112 + swatches.Height + 14 + 52, w, 44);
            restore.SetBounds(16, 112 + swatches.Height + 14 + 52 + 56, w, 40);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            DoLayout();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            TextRenderer.DrawText(g, "Lighting", Theme.Big, new Rectangle(16, 14, Width - 32, 36), Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "One colour for the whole keyboard.", Theme.Body, new Rectangle(16, 54, Width - 32, 44), Theme.Muted, TextFormatFlags.Left | TextFormatFlags.WordBreak);
            int y = restore.Bottom + 14;
            string text = status;
            if (raw.Length > 0)
            {
                text += "\r\n" + raw;
            }
            TextRenderer.DrawText(g, text, Theme.Small, new Rectangle(16, y, Width - 32, Math.Max(40, Height - y - 10)), Theme.Muted, TextFormatFlags.Left | TextFormatFlags.WordBreak);
        }
    }
}
