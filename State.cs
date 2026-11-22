using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Voltrix
{
    // One key change: when you press Vk, do something else.
    class Assignment
    {
        public int Vk;
        public string Kind; // key, action, run, text, block
        public string Arg;

        public static string KeyName(int vk)
        {
            switch (vk)
            {
                case 0x5B: return "Left Windows";
                case 0x5C: return "Right Windows";
                case 0x5D: return "Menu";
                case 0xA0: return "Left Shift";
                case 0xA1: return "Right Shift";
                case 0xA2: return "Left Ctrl";
                case 0xA3: return "Right Ctrl";
                case 0xA4: return "Left Alt";
                case 0xA5: return "Right Alt";
                case 0x14: return "Caps Lock";
            }
            return ((Keys)vk).ToString();
        }

        public string Describe()
        {
            string what;
            if (Kind == "key")
            {
                int t;
                int.TryParse(Arg, out t);
                what = "press " + KeyName(t);
            }
            else if (Kind == "action") what = Arg;
            else if (Kind == "run") what = "open " + Arg;
            else if (Kind == "text") what = "type \"" + Arg + "\"";
            else what = "disabled";
            return KeyName(Vk) + "   >   " + what;
        }
    }

    // A game (program name) and the lighting colour to use while it runs.
    class GameEntry
    {
        public string Exe; // name without .exe
        public Color Colour;
        public string Profile = ""; // optional profile to apply while it runs

        public string Describe()
        {
            string extra = Profile.Length > 0 ? ", profile " + Profile : "";
            return Exe + ".exe   (colour " + Colour.R + ", " + Colour.G + ", " + Colour.B + extra + ")";
        }
    }

    // Things the whole app shares.
    static class AppState
    {
        public static ScanResult Last;
        public static GameWatcher Watcher;
        public static bool Exiting = false;
        public static Color LightColor = Color.FromArgb(41, 171, 226);
        public static bool AssignmentsOn = true;
        public static bool GameManual = false;
        public static bool GameAuto = true;
        public static bool GameBlocking = false; // true while Game Mode is really active
        public static bool BlockWin = true;
        public static bool BlockAltTab = true;
        public static bool BlockAltF4 = false;
        public static bool BlockMenu = false;
        public static int Dpi = 800;
        public static int RateMs = 1;
        public static bool CheckOnStart = true;
        public static bool PreRelease = true;
        public static bool StartMinimised = false;
        public static string ActiveProfile = "";
        public static string ThemeName = "Voltrix Blue";
        public static Color AccentColor = Color.FromArgb(41, 171, 226);
        public static string UiFont = "Segoe UI";
        public static bool ShowSplash = true;
        public static bool CloseToTray = false;
        public static bool AutoRescan = false;
        public static bool ReapplyProfile = true;
        public static bool OverlayHotkeys = true;
        public static bool AlwaysOnTop = false;
        public static bool RememberWindow = true;
        public static string WinBounds = "";
        public static bool ConfirmExit = false;
        public static bool GHubWarning = true;
        public static bool HudOn = false;
        public static bool CrossOn = false;
        public static HudConfig Hud = new HudConfig();
        public static CrossConfig Cross = new CrossConfig();
        public static string[] HudSlots = new string[] { "", "", "" };
        public static string[] CrossSlots = new string[] { "", "", "" };
        public static DateTime GameStart = DateTime.MinValue;
        public static List<Assignment> Assignments = new List<Assignment>();
        public static List<GameEntry> Games = new List<GameEntry>();

        public static event EventHandler LightChanged;
        public static event EventHandler SettingsChanged;

        public static void RaiseLightChanged()
        {
            if (LightChanged != null)
            {
                LightChanged(null, EventArgs.Empty);
            }
        }

        public static void RaiseSettingsChanged()
        {
            if (SettingsChanged != null)
            {
                SettingsChanged(null, EventArgs.Empty);
            }
        }
    }

    // Saves settings in %APPDATA%\VOLTRIX Hub\settings.txt as name=value lines.
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

        static bool Flag(string v)
        {
            return v == "1";
        }

        public static void Load()
        {
            try
            {
                string f = FilePath();
                if (!File.Exists(f)) return;
                AppState.Assignments.Clear();
                AppState.Games.Clear();
                foreach (string line in File.ReadAllLines(f))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1);
                    int n;
                    if (key == "lightColor")
                    {
                        if (int.TryParse(val.Trim(), out n))
                        {
                            AppState.LightColor = Color.FromArgb(255, Color.FromArgb(n));
                        }
                    }
                    else if (key == "assignmentsOn") AppState.AssignmentsOn = Flag(val.Trim());
                    else if (key == "gameManual") AppState.GameManual = Flag(val.Trim());
                    else if (key == "gameAuto") AppState.GameAuto = Flag(val.Trim());
                    else if (key == "blockWin") AppState.BlockWin = Flag(val.Trim());
                    else if (key == "blockAltTab") AppState.BlockAltTab = Flag(val.Trim());
                    else if (key == "blockAltF4") AppState.BlockAltF4 = Flag(val.Trim());
                    else if (key == "blockMenu") AppState.BlockMenu = Flag(val.Trim());
                    else if (key == "checkOnStart") AppState.CheckOnStart = Flag(val.Trim());
                    else if (key == "preRelease") AppState.PreRelease = Flag(val.Trim());
                    else if (key == "startMinimised") AppState.StartMinimised = Flag(val.Trim());
                    else if (key == "activeProfile") AppState.ActiveProfile = val.Trim();
                    else if (key == "theme") AppState.ThemeName = val.Trim();
                    else if (key == "uiFont") AppState.UiFont = val.Trim();
                    else if (key == "accent")
                    {
                        if (int.TryParse(val.Trim(), out n)) AppState.AccentColor = Color.FromArgb(255, Color.FromArgb(n));
                    }
                    else if (key == "showSplash") AppState.ShowSplash = Flag(val.Trim());
                    else if (key == "closeToTray") AppState.CloseToTray = Flag(val.Trim());
                    else if (key == "autoRescan") AppState.AutoRescan = Flag(val.Trim());
                    else if (key == "reapplyProfile") AppState.ReapplyProfile = Flag(val.Trim());
                    else if (key == "overlayHotkeys") AppState.OverlayHotkeys = Flag(val.Trim());
                    else if (key == "alwaysOnTop") AppState.AlwaysOnTop = Flag(val.Trim());
                    else if (key == "rememberWindow") AppState.RememberWindow = Flag(val.Trim());
                    else if (key == "winBounds") AppState.WinBounds = val.Trim();
                    else if (key == "confirmExit") AppState.ConfirmExit = Flag(val.Trim());
                    else if (key == "ghubWarning") AppState.GHubWarning = Flag(val.Trim());
                    else if (key.StartsWith("hk_")) Hotkeys.Set(key.Substring(3), val.Trim());
                    else if (key == "hudOn") AppState.HudOn = Flag(val.Trim());
                    else if (key == "crossOn") AppState.CrossOn = Flag(val.Trim());
                    else if (key == "hud") AppState.Hud = HudConfig.Parse(val);
                    else if (key == "cross") AppState.Cross = CrossConfig.Parse(val);
                    else if (key == "hudSlot1") AppState.HudSlots[0] = val;
                    else if (key == "hudSlot2") AppState.HudSlots[1] = val;
                    else if (key == "hudSlot3") AppState.HudSlots[2] = val;
                    else if (key == "crossSlot1") AppState.CrossSlots[0] = val;
                    else if (key == "crossSlot2") AppState.CrossSlots[1] = val;
                    else if (key == "crossSlot3") AppState.CrossSlots[2] = val;
                    else if (key == "dpi")
                    {
                        if (int.TryParse(val.Trim(), out n) && n >= 100 && n <= 25600) AppState.Dpi = n;
                    }
                    else if (key == "rateMs")
                    {
                        if (int.TryParse(val.Trim(), out n) && n >= 1 && n <= 8) AppState.RateMs = n;
                    }
                    else if (key == "assign")
                    {
                        string[] p = val.Split('\t');
                        int vk;
                        if (p.Length >= 3 && int.TryParse(p[0], out vk))
                        {
                            Assignment a = new Assignment();
                            a.Vk = vk;
                            a.Kind = p[1];
                            a.Arg = p[2];
                            AppState.Assignments.Add(a);
                        }
                    }
                    else if (key == "game")
                    {
                        string[] p = val.Split('\t');
                        int argb;
                        if (p.Length >= 2 && int.TryParse(p[1], out argb))
                        {
                            GameEntry g = new GameEntry();
                            g.Exe = p[0];
                            g.Colour = Color.FromArgb(255, Color.FromArgb(argb));
                            if (p.Length >= 3) g.Profile = p[2];
                            AppState.Games.Add(g);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        static string Clean(string s)
        {
            if (s == null) return "";
            return s.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }

        public static void Save()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("lightColor=" + AppState.LightColor.ToArgb());
                lines.Add("assignmentsOn=" + (AppState.AssignmentsOn ? "1" : "0"));
                lines.Add("gameManual=" + (AppState.GameManual ? "1" : "0"));
                lines.Add("gameAuto=" + (AppState.GameAuto ? "1" : "0"));
                lines.Add("blockWin=" + (AppState.BlockWin ? "1" : "0"));
                lines.Add("blockAltTab=" + (AppState.BlockAltTab ? "1" : "0"));
                lines.Add("blockAltF4=" + (AppState.BlockAltF4 ? "1" : "0"));
                lines.Add("blockMenu=" + (AppState.BlockMenu ? "1" : "0"));
                lines.Add("checkOnStart=" + (AppState.CheckOnStart ? "1" : "0"));
                lines.Add("preRelease=" + (AppState.PreRelease ? "1" : "0"));
                lines.Add("startMinimised=" + (AppState.StartMinimised ? "1" : "0"));
                lines.Add("activeProfile=" + Clean(AppState.ActiveProfile));
                lines.Add("theme=" + Clean(AppState.ThemeName));
                lines.Add("uiFont=" + Clean(AppState.UiFont));
                lines.Add("accent=" + AppState.AccentColor.ToArgb());
                lines.Add("showSplash=" + (AppState.ShowSplash ? "1" : "0"));
                lines.Add("closeToTray=" + (AppState.CloseToTray ? "1" : "0"));
                lines.Add("autoRescan=" + (AppState.AutoRescan ? "1" : "0"));
                lines.Add("reapplyProfile=" + (AppState.ReapplyProfile ? "1" : "0"));
                lines.Add("overlayHotkeys=" + (AppState.OverlayHotkeys ? "1" : "0"));
                lines.Add("alwaysOnTop=" + (AppState.AlwaysOnTop ? "1" : "0"));
                lines.Add("rememberWindow=" + (AppState.RememberWindow ? "1" : "0"));
                lines.Add("winBounds=" + Clean(AppState.WinBounds));
                lines.Add("confirmExit=" + (AppState.ConfirmExit ? "1" : "0"));
                lines.Add("ghubWarning=" + (AppState.GHubWarning ? "1" : "0"));
                foreach (HotkeyDef hd in Hotkeys.All)
                {
                    lines.Add("hk_" + hd.Id + "=" + hd.Mods + ":" + hd.Vk);
                }
                lines.Add("hudOn=" + (AppState.HudOn ? "1" : "0"));
                lines.Add("crossOn=" + (AppState.CrossOn ? "1" : "0"));
                lines.Add("hud=" + Clean(AppState.Hud.ToText()));
                lines.Add("cross=" + Clean(AppState.Cross.ToText()));
                for (int i = 0; i < 3; i++)
                {
                    lines.Add("hudSlot" + (i + 1) + "=" + Clean(AppState.HudSlots[i]));
                    lines.Add("crossSlot" + (i + 1) + "=" + Clean(AppState.CrossSlots[i]));
                }
                lines.Add("dpi=" + AppState.Dpi);
                lines.Add("rateMs=" + AppState.RateMs);
                foreach (Assignment a in AppState.Assignments)
                {
                    lines.Add("assign=" + a.Vk + "\t" + a.Kind + "\t" + Clean(a.Arg));
                }
                foreach (GameEntry g in AppState.Games)
                {
                    lines.Add("game=" + Clean(g.Exe) + "\t" + g.Colour.ToArgb() + "\t" + Clean(g.Profile));
                }
                File.WriteAllLines(FilePath(), lines.ToArray());
            }
            catch (Exception)
            {
            }
        }
    }
}
