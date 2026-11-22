using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace Voltrix
{
    // Small helper for settings stored as "name=value;name=value".
    static class Kv
    {
        public static Dictionary<string, string> Parse(string s)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            if (s == null) return d;
            foreach (string part in s.Split(';'))
            {
                int i = part.IndexOf('=');
                if (i > 0) d[part.Substring(0, i)] = part.Substring(i + 1);
            }
            return d;
        }

        public static int Int(Dictionary<string, string> d, string k, int def)
        {
            string v;
            int n;
            if (d.TryGetValue(k, out v) && int.TryParse(v, out n)) return n;
            return def;
        }

        public static bool Bool(Dictionary<string, string> d, string k, bool def)
        {
            string v;
            if (d.TryGetValue(k, out v)) return v == "1";
            return def;
        }

        public static string Str(Dictionary<string, string> d, string k, string def)
        {
            string v;
            if (d.TryGetValue(k, out v)) return v;
            return def;
        }

        public static Color Col(Dictionary<string, string> d, string k, Color def)
        {
            string v;
            int n;
            if (d.TryGetValue(k, out v) && int.TryParse(v, out n)) return Color.FromArgb(255, Color.FromArgb(n));
            return def;
        }

        public static string B(bool b)
        {
            return b ? "1" : "0";
        }
    }

    // How the HUD looks and what is in each of the nine screen squares.
    class HudConfig
    {
        public static readonly string[] WidgetKeys = new string[] { "fps", "cpu", "ram", "ping", "clock", "timer", "profile", "game", "gamemode", "dpi" };
        public static readonly string[] WidgetTitles = new string[] { "FPS", "CPU load", "RAM use", "Ping", "Clock", "Session timer", "Active profile", "Game running", "Game Mode", "Mouse DPI" };
        public static readonly string[] TemplateNames = new string[] { "Minimal FPS", "Competitive", "Streamer", "Full stats", "Clean clock" };
        public static readonly string[] FontChoices = new string[] { "Segoe UI", "Consolas", "Bahnschrift", "Arial" };

        public string[] Cells = new string[9];
        public int FontSize = 18;
        public Color TextColor = Color.White;
        public Color LabelColor = Color.FromArgb(41, 171, 226);
        public int Opacity = 90;
        public bool Pill = true;
        public bool Outline = true;
        public bool BoldText = true;
        public bool ShowLabels = true;
        public bool OnlyInGame = false;
        public int Margin = 24;
        public int Monitor = 0;
        public string FontName = "Segoe UI";

        public HudConfig()
        {
            for (int i = 0; i < 9; i++) Cells[i] = "";
            Cells[0] = "fps";
        }

        public static string TitleOf(string key)
        {
            for (int i = 0; i < WidgetKeys.Length; i++)
            {
                if (WidgetKeys[i] == key) return WidgetTitles[i];
            }
            return key;
        }

        public bool Uses(string key)
        {
            foreach (string c in Cells)
            {
                foreach (string k in c.Split(','))
                {
                    if (k == key) return true;
                }
            }
            return false;
        }

        // Puts a widget in a square. If it is already there it is removed. A widget lives in one square only.
        public void Toggle(int cell, string key)
        {
            bool wasHere = false;
            foreach (string k in Cells[cell].Split(',')) if (k == key) wasHere = true;
            for (int i = 0; i < 9; i++)
            {
                List<string> keep = new List<string>();
                foreach (string k in Cells[i].Split(','))
                {
                    if (k.Length > 0 && k != key) keep.Add(k);
                }
                Cells[i] = string.Join(",", keep.ToArray());
            }
            if (!wasHere)
            {
                Cells[cell] = Cells[cell].Length == 0 ? key : Cells[cell] + "," + key;
            }
        }

        public HudConfig Clone()
        {
            return Parse(ToText());
        }

        public string ToText()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("size=" + FontSize + ";text=" + TextColor.ToArgb() + ";label=" + LabelColor.ToArgb());
            sb.Append(";op=" + Opacity + ";pill=" + Kv.B(Pill) + ";outline=" + Kv.B(Outline) + ";bold=" + Kv.B(BoldText));
            sb.Append(";labels=" + Kv.B(ShowLabels) + ";game=" + Kv.B(OnlyInGame) + ";margin=" + Margin + ";mon=" + Monitor);
            sb.Append(";font=" + FontName.Replace(";", " ").Replace("=", " "));
            for (int i = 0; i < 9; i++) sb.Append(";c" + i + "=" + Cells[i]);
            return sb.ToString();
        }

        public static HudConfig Parse(string text)
        {
            HudConfig h = new HudConfig();
            Dictionary<string, string> d = Kv.Parse(text);
            h.FontSize = Kv.Int(d, "size", h.FontSize);
            h.TextColor = Kv.Col(d, "text", h.TextColor);
            h.LabelColor = Kv.Col(d, "label", h.LabelColor);
            h.Opacity = Kv.Int(d, "op", h.Opacity);
            h.Pill = Kv.Bool(d, "pill", h.Pill);
            h.Outline = Kv.Bool(d, "outline", h.Outline);
            h.BoldText = Kv.Bool(d, "bold", h.BoldText);
            h.ShowLabels = Kv.Bool(d, "labels", h.ShowLabels);
            h.OnlyInGame = Kv.Bool(d, "game", h.OnlyInGame);
            h.Margin = Kv.Int(d, "margin", h.Margin);
            h.Monitor = Kv.Int(d, "mon", h.Monitor);
            h.FontName = Kv.Str(d, "font", h.FontName);
            if (d.Count > 0)
            {
                for (int i = 0; i < 9; i++) h.Cells[i] = Kv.Str(d, "c" + i, "");
            }
            if (h.FontSize < 8) h.FontSize = 8;
            if (h.FontSize > 60) h.FontSize = 60;
            if (h.Opacity < 10) h.Opacity = 10;
            if (h.Opacity > 100) h.Opacity = 100;
            return h;
        }

        public static HudConfig Template(string name)
        {
            HudConfig h = new HudConfig();
            for (int i = 0; i < 9; i++) h.Cells[i] = "";
            if (name == "Minimal FPS")
            {
                h.Cells[0] = "fps"; h.Pill = false; h.FontSize = 22; h.Opacity = 100;
            }
            else if (name == "Competitive")
            {
                h.Cells[0] = "fps,ping"; h.Cells[2] = "clock"; h.FontSize = 16; h.Pill = true;
            }
            else if (name == "Streamer")
            {
                h.Cells[2] = "fps,cpu,ram"; h.Cells[0] = "clock"; h.FontSize = 15; h.Opacity = 85;
            }
            else if (name == "Full stats")
            {
                h.Cells[0] = "fps,cpu,ram,ping"; h.Cells[2] = "clock,timer"; h.Cells[6] = "profile,game,gamemode,dpi"; h.FontSize = 14;
            }
            else
            {
                h.Cells[1] = "clock"; h.FontSize = 30; h.Pill = false; h.ShowLabels = false; h.Opacity = 80;
            }
            return h;
        }
    }

    // How the crosshair looks.
    class CrossConfig
    {
        public static readonly string[] Styles = new string[] { "Cross", "Dot", "Circle", "T shape", "Square", "Diamond" };
        public static readonly string[] TemplateNames = new string[] { "Classic green", "Red dot", "Pro white", "Cyan circle", "Tiny dot", "Gap T" };

        public string Style = "Cross";
        public int Size = 10;
        public int Thickness = 2;
        public int Gap = 4;
        public int DotSize = 2;
        public bool CentreDot = false;
        public bool Outline = true;
        public Color Colour = Color.FromArgb(0, 255, 0);
        public Color OutlineColour = Color.Black;
        public int Opacity = 100;
        public int OffsetX = 0;
        public int OffsetY = 0;
        public int Monitor = 0;

        public CrossConfig Clone()
        {
            return Parse(ToText());
        }

        public string ToText()
        {
            return "style=" + Style + ";size=" + Size + ";th=" + Thickness + ";gap=" + Gap + ";dot=" + DotSize + ";cdot=" + Kv.B(CentreDot)
                + ";outline=" + Kv.B(Outline) + ";col=" + Colour.ToArgb() + ";ocol=" + OutlineColour.ToArgb() + ";op=" + Opacity
                + ";ox=" + OffsetX + ";oy=" + OffsetY + ";mon=" + Monitor;
        }

        public static CrossConfig Parse(string text)
        {
            CrossConfig c = new CrossConfig();
            Dictionary<string, string> d = Kv.Parse(text);
            c.Style = Kv.Str(d, "style", c.Style);
            c.Size = Kv.Int(d, "size", c.Size);
            c.Thickness = Kv.Int(d, "th", c.Thickness);
            c.Gap = Kv.Int(d, "gap", c.Gap);
            c.DotSize = Kv.Int(d, "dot", c.DotSize);
            c.CentreDot = Kv.Bool(d, "cdot", c.CentreDot);
            c.Outline = Kv.Bool(d, "outline", c.Outline);
            c.Colour = Kv.Col(d, "col", c.Colour);
            c.OutlineColour = Kv.Col(d, "ocol", c.OutlineColour);
            c.Opacity = Kv.Int(d, "op", c.Opacity);
            c.OffsetX = Kv.Int(d, "ox", c.OffsetX);
            c.OffsetY = Kv.Int(d, "oy", c.OffsetY);
            c.Monitor = Kv.Int(d, "mon", c.Monitor);
            if (c.Size < 1) c.Size = 1;
            if (c.Thickness < 1) c.Thickness = 1;
            if (c.Opacity < 10) c.Opacity = 10;
            if (c.Opacity > 100) c.Opacity = 100;
            return c;
        }

        public static CrossConfig Template(string name)
        {
            CrossConfig c = new CrossConfig();
            if (name == "Red dot")
            {
                c.Style = "Dot"; c.DotSize = 3; c.Colour = Color.FromArgb(255, 40, 40);
            }
            else if (name == "Pro white")
            {
                c.Style = "Cross"; c.Size = 8; c.Gap = 5; c.Thickness = 1; c.Colour = Color.White;
            }
            else if (name == "Cyan circle")
            {
                c.Style = "Circle"; c.Size = 14; c.Thickness = 2; c.CentreDot = true; c.Colour = Color.FromArgb(20, 220, 220);
            }
            else if (name == "Tiny dot")
            {
                c.Style = "Dot"; c.DotSize = 1; c.Outline = true; c.Colour = Color.FromArgb(255, 255, 60);
            }
            else if (name == "Gap T")
            {
                c.Style = "T shape"; c.Size = 9; c.Gap = 3; c.Thickness = 2; c.Colour = Color.FromArgb(255, 130, 20);
            }
            return c;
        }

        public int Extent()
        {
            int e = Size + Gap + Thickness + 6;
            if (Style == "Dot") e = DotSize + 6;
            return Math.Max(e, DotSize + 6);
        }

        public void Draw(Graphics g, int cx, int cy)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (Outline)
            {
                DrawShapes(g, cx, cy, OutlineColour, Thickness + 2f, DotSize + 1);
            }
            DrawShapes(g, cx, cy, Colour, Thickness, DotSize);
        }

        void DrawShapes(Graphics g, int cx, int cy, Color c, float th, float dot)
        {
            using (Pen pen = new Pen(c, th))
            using (SolidBrush brush = new SolidBrush(c))
            {
                int s = Size;
                int gp = Gap;
                if (Style == "Cross" || Style == "T shape")
                {
                    g.DrawLine(pen, cx - gp - s, cy, cx - gp, cy);
                    g.DrawLine(pen, cx + gp, cy, cx + gp + s, cy);
                    g.DrawLine(pen, cx, cy + gp, cx, cy + gp + s);
                    if (Style == "Cross")
                    {
                        g.DrawLine(pen, cx, cy - gp - s, cx, cy - gp);
                    }
                }
                else if (Style == "Dot")
                {
                    g.FillEllipse(brush, cx - dot, cy - dot, dot * 2, dot * 2);
                }
                else if (Style == "Circle")
                {
                    g.DrawEllipse(pen, cx - s, cy - s, s * 2, s * 2);
                }
                else if (Style == "Square")
                {
                    g.DrawRectangle(pen, cx - s, cy - s, s * 2, s * 2);
                }
                else if (Style == "Diamond")
                {
                    g.DrawPolygon(pen, new Point[] { new Point(cx, cy - s), new Point(cx + s, cy), new Point(cx, cy + s), new Point(cx - s, cy) });
                }
                if (CentreDot && Style != "Dot")
                {
                    g.FillEllipse(brush, cx - dot, cy - dot, dot * 2, dot * 2);
                }
            }
        }
    }

    // Numbers shown on the HUD (CPU, RAM, ping, clock).
    static class Stats
    {
        [StructLayout(LayoutKind.Sequential)]
        struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll")]
        static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX m);

        public static DateTime AppStart = DateTime.Now;
        public static int Cpu = -1;
        public static int Ram = -1;
        public static int PingMs = -1;
        static PerformanceCounter cpuCounter;
        static bool cpuFailed;
        static int pingTick;
        static bool pinging;

        public static void Tick()
        {
            try
            {
                if (cpuCounter == null && !cpuFailed)
                {
                    cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    cpuCounter.NextValue();
                }
                if (cpuCounter != null) Cpu = (int)Math.Round(cpuCounter.NextValue());
            }
            catch (Exception)
            {
                cpuFailed = true;
                cpuCounter = null;
            }
            try
            {
                MEMORYSTATUSEX m = new MEMORYSTATUSEX();
                m.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref m)) Ram = (int)m.dwMemoryLoad;
            }
            catch (Exception)
            {
            }
            pingTick++;
            if (!pinging && pingTick % 3 == 1)
            {
                pinging = true;
                System.Threading.ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    try
                    {
                        using (Ping p = new Ping())
                        {
                            PingReply r = p.Send("1.1.1.1", 1000);
                            PingMs = r.Status == IPStatus.Success ? (int)r.RoundtripTime : -1;
                        }
                    }
                    catch (Exception)
                    {
                        PingMs = -1;
                    }
                    pinging = false;
                });
            }
        }
    }

    // FPS of the program you are playing. Windows does not let one program read another's FPS,
    // so VOLTRIX Hub runs PresentMon (free, open source) and shows its numbers.
    static class FpsMonitor
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);

        public static string Status = "PresentMon not set up yet.";
        public static int Fps = -1;

        static Process proc;
        static Dictionary<int, int> counts = new Dictionary<int, int>();
        static Dictionary<int, int> lastSecond = new Dictionary<int, int>();
        static int pidCol = -1;
        static bool gotHeader;
        static bool triedLegacy;
        static DateTime started;
        static readonly object gate = new object();

        public static string ToolsDir()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VOLTRIX Hub"), "tools");
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            return dir;
        }

        public static string ToolPath()
        {
            string[] dirs = new string[] { ToolsDir(), AppDomain.CurrentDomain.BaseDirectory };
            foreach (string d in dirs)
            {
                try
                {
                    string[] files = System.IO.Directory.GetFiles(d, "PresentMon*.exe");
                    if (files.Length > 0) return files[0];
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        public static bool IsAdmin()
        {
            try
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool Running
        {
            get { return proc != null && !proc.HasExited; }
        }

        public static void Start()
        {
            if (Running) return;
            string path = ToolPath();
            if (path == null)
            {
                Status = "PresentMon.exe not found. Put it in the tools folder.";
                Fps = -1;
                return;
            }
            Launch(path, triedLegacy);
        }

        static void Launch(string path, bool legacy)
        {
            try
            {
                gotHeader = false;
                pidCol = -1;
                lock (gate) { counts.Clear(); lastSecond.Clear(); }
                ProcessStartInfo psi = new ProcessStartInfo(path);
                psi.Arguments = legacy
                    ? "-output_stdout -stop_existing_session -session_name VOLTRIXHub"
                    : "--output_stdout --no_console_stats --stop_existing_session --session_name VOLTRIXHub";
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                Process p = new Process();
                p.StartInfo = psi;
                p.EnableRaisingEvents = true;
                p.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) { OnLine(e.Data); };
                string err = "";
                p.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null && e.Data.Length > 0) err = e.Data;
                };
                p.Exited += delegate(object sender, EventArgs e)
                {
                    double secs = (DateTime.Now - started).TotalSeconds;
                    if (secs < 5 && !legacy && !triedLegacy)
                    {
                        triedLegacy = true;
                        Status = "Trying older PresentMon options...";
                        return;
                    }
                    Status = err.Length > 0 ? "PresentMon stopped: " + err : "PresentMon stopped. Try running VOLTRIX Hub as administrator.";
                    Fps = -1;
                };
                started = DateTime.Now;
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                proc = p;
                Status = IsAdmin() ? "PresentMon is running." : "PresentMon is running. If FPS stays empty, run VOLTRIX Hub as administrator.";
            }
            catch (Exception ex)
            {
                Status = "Could not start PresentMon: " + ex.Message;
                Fps = -1;
            }
        }

        static void OnLine(string line)
        {
            if (line == null || line.Length == 0) return;
            string[] f = line.Split(',');
            if (!gotHeader)
            {
                for (int i = 0; i < f.Length; i++)
                {
                    string h = f[i].Trim().Trim('"');
                    if (h == "ProcessID" || h == "ProcessId") pidCol = i;
                }
                if (pidCol >= 0) gotHeader = true;
                return;
            }
            int pid;
            if (pidCol < f.Length && int.TryParse(f[pidCol].Trim().Trim('"'), out pid))
            {
                lock (gate)
                {
                    int n;
                    counts.TryGetValue(pid, out n);
                    counts[pid] = n + 1;
                }
            }
        }

        // Call once per second.
        public static void Tick()
        {
            if (!Running)
            {
                if (triedLegacy && proc != null && proc.HasExited && Status.StartsWith("Trying"))
                {
                    string path = ToolPath();
                    if (path != null) Launch(path, true);
                }
                return;
            }
            uint pid = 0;
            GetWindowThreadProcessId(GetForegroundWindow(), out pid);
            int value = -1;
            lock (gate)
            {
                lastSecond = counts;
                counts = new Dictionary<int, int>();
                int n;
                if (lastSecond.TryGetValue((int)pid, out n)) value = n;
            }
            Fps = value;
        }

        public static void Stop()
        {
            try
            {
                if (proc != null && !proc.HasExited) proc.Kill();
            }
            catch (Exception)
            {
            }
            proc = null;
            Fps = -1;
            triedLegacy = false;
        }

        public static string Text
        {
            get { return Fps >= 0 ? Fps.ToString() : "--"; }
        }
    }

    // Draws the HUD. Used by the real overlay and by the small preview.
    static class HudRenderer
    {
        static string[] Line(string key, HudConfig cfg, bool preview)
        {
            string label = "";
            string value = "";
            switch (key)
            {
                case "fps": label = "FPS"; value = preview ? "144" : FpsMonitor.Text; break;
                case "cpu": label = "CPU"; value = preview ? "23%" : (Stats.Cpu >= 0 ? Stats.Cpu + "%" : "--"); break;
                case "ram": label = "RAM"; value = preview ? "41%" : (Stats.Ram >= 0 ? Stats.Ram + "%" : "--"); break;
                case "ping": label = "PING"; value = preview ? "18 ms" : (Stats.PingMs >= 0 ? Stats.PingMs + " ms" : "--"); break;
                case "clock": label = ""; value = preview ? "21:45" : DateTime.Now.ToString("HH:mm"); break;
                case "timer":
                    label = "TIME";
                    if (preview) value = "00:42:10";
                    else
                    {
                        DateTime since = (AppState.Watcher != null && AppState.Watcher.ActiveGame != null && AppState.GameStart != DateTime.MinValue) ? AppState.GameStart : Stats.AppStart;
                        TimeSpan t = DateTime.Now - since;
                        value = ((int)t.TotalHours).ToString("00") + ":" + t.Minutes.ToString("00") + ":" + t.Seconds.ToString("00");
                    }
                    break;
                case "profile": label = "PROFILE"; value = preview ? "Work" : (AppState.ActiveProfile.Length > 0 ? AppState.ActiveProfile : "none"); break;
                case "game": label = "GAME"; value = preview ? "valorant" : (AppState.Watcher != null && AppState.Watcher.ActiveGame != null ? AppState.Watcher.ActiveGame : "--"); break;
                case "gamemode": label = "GAME MODE"; value = preview ? "ON" : (AppState.GameBlocking ? "ON" : "OFF"); break;
                case "dpi": label = "DPI"; value = preview ? "1600" : AppState.Dpi.ToString(); break;
                default: return null;
            }
            if (!cfg.ShowLabels) label = "";
            return new string[] { label, value };
        }

        static void DrawText(Graphics g, string text, Font font, Color c, float x, float y, bool outline, StringFormat sf)
        {
            if (outline)
            {
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddString(text, font.FontFamily, (int)font.Style, font.Size, new PointF(x, y), sf);
                    using (Pen p = new Pen(Color.FromArgb(230, 0, 0, 0), Math.Max(2f, font.Size / 7f)))
                    {
                        p.LineJoin = LineJoin.Round;
                        g.DrawPath(p, gp);
                    }
                    using (SolidBrush b = new SolidBrush(c))
                    {
                        g.FillPath(b, gp);
                    }
                }
            }
            else
            {
                using (SolidBrush b = new SolidBrush(c))
                {
                    g.DrawString(text, font, b, x, y, sf);
                }
            }
        }

        public static void Draw(Graphics g, Size area, HudConfig cfg, bool preview)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            float scale = preview ? area.Height / 540f : 1f;
            float px = Math.Max(6f, cfg.FontSize * scale);
            float margin = cfg.Margin * scale;
            using (Font font = new Font(cfg.FontName, px, cfg.BoldText ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel))
            using (StringFormat sf = StringFormat.GenericTypographic)
            {
                float lineH = px * 1.2f;
                float padX = cfg.Pill ? px * 0.6f : 0f;
                float padY = cfg.Pill ? px * 0.25f : 0f;
                float gap = Math.Max(2f, px * 0.25f);
                using (SolidBrush pillBrush = new SolidBrush(Color.FromArgb(30, 30, 36)))
                {
                    for (int cell = 0; cell < 9; cell++)
                    {
                        if (cfg.Cells[cell].Length == 0) continue;
                        List<string[]> lines = new List<string[]>();
                        foreach (string key in cfg.Cells[cell].Split(','))
                        {
                            string[] l = Line(key, cfg, preview);
                            if (l != null) lines.Add(l);
                        }
                        if (lines.Count == 0) continue;
                        float totalH = lines.Count * (lineH + padY * 2) + (lines.Count - 1) * gap;
                        int col = cell % 3;
                        int row = cell / 3;
                        float y = row == 0 ? margin : (row == 1 ? (area.Height - totalH) / 2f : area.Height - margin - totalH);
                        foreach (string[] l in lines)
                        {
                            string lab = l[0].Length > 0 ? l[0] + " " : "";
                            float lw = lab.Length > 0 ? g.MeasureString(lab, font, 10000, sf).Width : 0f;
                            float vw = g.MeasureString(l[1], font, 10000, sf).Width;
                            float w = lw + vw + padX * 2;
                            float x = col == 0 ? margin : (col == 1 ? (area.Width - w) / 2f : area.Width - margin - w);
                            if (cfg.Pill)
                            {
                                Rectangle r = new Rectangle((int)x, (int)y, (int)w, (int)(lineH + padY * 2));
                                using (GraphicsPath gp = Theme.Round(r, Math.Max(3, (int)(px * 0.4f))))
                                {
                                    g.FillPath(pillBrush, gp);
                                }
                            }
                            float tx = x + padX;
                            float ty = y + padY;
                            if (lab.Length > 0) DrawText(g, lab, font, cfg.LabelColor, tx, ty, cfg.Outline && !cfg.Pill, sf);
                            DrawText(g, l[1], font, cfg.TextColor, tx + lw, ty, cfg.Outline && !cfg.Pill, sf);
                            y += lineH + padY * 2 + gap;
                        }
                    }
                }
            }
        }
    }

    // A transparent window you can click through. Always on top.
    class OverlayBase : Form
    {
        public static readonly Color Key = Color.FromArgb(2, 2, 2);

        public OverlayBase()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Key;
            TransparencyKey = Key;
            DoubleBuffered = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x08000000;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        public static Screen PickScreen(int index)
        {
            Screen[] all = Screen.AllScreens;
            if (index < 0 || index >= all.Length) index = 0;
            return all[index];
        }
    }

    class HudForm : OverlayBase
    {
        Timer timer;

        public HudForm()
        {
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += delegate(object sender, EventArgs e)
            {
                Stats.Tick();
                FpsMonitor.Tick();
                Invalidate();
            };
            timer.Start();
        }

        public void ApplyConfig()
        {
            HudConfig c = AppState.Hud;
            Screen s = PickScreen(c.Monitor);
            Bounds = s.Bounds;
            Opacity = c.Opacity / 100.0;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            HudConfig c = AppState.Hud;
            if (c.OnlyInGame && (AppState.Watcher == null || AppState.Watcher.ActiveGame == null)) return;
            HudRenderer.Draw(e.Graphics, ClientSize, c, false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && timer != null) timer.Dispose();
            base.Dispose(disposing);
        }
    }

    class CrossForm : OverlayBase
    {
        public void ApplyConfig()
        {
            CrossConfig c = AppState.Cross;
            Screen s = PickScreen(c.Monitor);
            int w = c.Extent() * 2 + 4;
            Size = new Size(w, w);
            int cx = s.Bounds.X + s.Bounds.Width / 2 + c.OffsetX;
            int cy = s.Bounds.Y + s.Bounds.Height / 2 + c.OffsetY;
            Location = new Point(cx - w / 2, cy - w / 2);
            Opacity = c.Opacity / 100.0;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            AppState.Cross.Draw(e.Graphics, ClientSize.Width / 2, ClientSize.Height / 2);
        }
    }

    // Shows, hides and refreshes the two overlays.
    static class Overlays
    {
        static HudForm hud;
        static CrossForm cross;

        public static void Apply()
        {
            if (AppState.HudOn)
            {
                if (hud == null || hud.IsDisposed)
                {
                    hud = new HudForm();
                    hud.ApplyConfig();
                    hud.Show();
                }
                hud.ApplyConfig();
                if (AppState.Hud.Uses("fps")) FpsMonitor.Start();
                else FpsMonitor.Stop();
            }
            else
            {
                if (hud != null)
                {
                    hud.Close();
                    hud = null;
                }
                FpsMonitor.Stop();
            }

            if (AppState.CrossOn)
            {
                if (cross == null || cross.IsDisposed)
                {
                    cross = new CrossForm();
                    cross.ApplyConfig();
                    cross.Show();
                }
                cross.ApplyConfig();
            }
            else if (cross != null)
            {
                cross.Close();
                cross = null;
            }
        }

        public static void ToggleHud()
        {
            AppState.HudOn = !AppState.HudOn;
            SettingsStore.Save();
            Apply();
            AppState.RaiseSettingsChanged();
        }

        public static void ToggleCross()
        {
            AppState.CrossOn = !AppState.CrossOn;
            SettingsStore.Save();
            Apply();
            AppState.RaiseSettingsChanged();
        }

        public static void Shutdown()
        {
            FpsMonitor.Stop();
            if (hud != null) hud.Close();
            if (cross != null) cross.Close();
            hud = null;
            cross = null;
        }
    }
}
