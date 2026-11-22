using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Voltrix
{
    // A saved set of settings: lighting, key assignments, Game Mode keys, mouse DPI.
    class Profile
    {
        public string Name = "";
        public Color Light = Color.FromArgb(41, 171, 226);
        public List<Assignment> Assignments = new List<Assignment>();
        public bool BlockWin = true;
        public bool BlockAltTab = true;
        public bool BlockAltF4;
        public bool BlockMenu;
        public int Dpi = 800;
        public int RateMs = 1;

        public static Profile FromCurrent(string name)
        {
            Profile p = new Profile();
            p.Name = name;
            p.Light = AppState.LightColor;
            foreach (Assignment a in AppState.Assignments)
            {
                Assignment c = new Assignment();
                c.Vk = a.Vk;
                c.Kind = a.Kind;
                c.Arg = a.Arg;
                p.Assignments.Add(c);
            }
            p.BlockWin = AppState.BlockWin;
            p.BlockAltTab = AppState.BlockAltTab;
            p.BlockAltF4 = AppState.BlockAltF4;
            p.BlockMenu = AppState.BlockMenu;
            p.Dpi = AppState.Dpi;
            p.RateMs = AppState.RateMs;
            return p;
        }

        static string One(bool b)
        {
            return b ? "1" : "0";
        }

        public string ToText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("light=" + Light.ToArgb());
            sb.AppendLine("blockWin=" + One(BlockWin));
            sb.AppendLine("blockAltTab=" + One(BlockAltTab));
            sb.AppendLine("blockAltF4=" + One(BlockAltF4));
            sb.AppendLine("blockMenu=" + One(BlockMenu));
            sb.AppendLine("dpi=" + Dpi);
            sb.AppendLine("rateMs=" + RateMs);
            foreach (Assignment a in Assignments)
            {
                string arg = (a.Arg ?? "").Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
                sb.AppendLine("assign=" + a.Vk + "\t" + a.Kind + "\t" + arg);
            }
            return sb.ToString();
        }

        public static Profile Parse(string name, string text)
        {
            Profile p = new Profile();
            p.Name = name;
            string[] lines = text.Split('\n');
            foreach (string raw in lines)
            {
                string line = raw.TrimEnd('\r');
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1);
                int n;
                if (key == "light")
                {
                    if (int.TryParse(val.Trim(), out n)) p.Light = Color.FromArgb(255, Color.FromArgb(n));
                }
                else if (key == "blockWin") p.BlockWin = val.Trim() == "1";
                else if (key == "blockAltTab") p.BlockAltTab = val.Trim() == "1";
                else if (key == "blockAltF4") p.BlockAltF4 = val.Trim() == "1";
                else if (key == "blockMenu") p.BlockMenu = val.Trim() == "1";
                else if (key == "dpi")
                {
                    if (int.TryParse(val.Trim(), out n) && n >= 100 && n <= 25600) p.Dpi = n;
                }
                else if (key == "rateMs")
                {
                    if (int.TryParse(val.Trim(), out n) && n >= 1 && n <= 8) p.RateMs = n;
                }
                else if (key == "assign")
                {
                    string[] parts = val.Split('\t');
                    int vk;
                    if (parts.Length >= 3 && int.TryParse(parts[0], out vk))
                    {
                        Assignment a = new Assignment();
                        a.Vk = vk;
                        a.Kind = parts[1];
                        a.Arg = parts[2];
                        p.Assignments.Add(a);
                    }
                }
            }
            return p;
        }
    }

    // Profile files live in %APPDATA%\VOLTRIX Hub\profiles as name.vprofile
    static class ProfileStore
    {
        static string Dir()
        {
            string dir = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VOLTRIX Hub"), "profiles");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public static string SafeName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }

        public static List<string> Names()
        {
            List<string> list = new List<string>();
            try
            {
                foreach (string f in Directory.GetFiles(Dir(), "*.vprofile"))
                {
                    list.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
            catch (Exception)
            {
            }
            list.Sort();
            return list;
        }

        public static Profile Load(string name)
        {
            try
            {
                string f = Path.Combine(Dir(), SafeName(name) + ".vprofile");
                if (!File.Exists(f)) return null;
                return Profile.Parse(name, File.ReadAllText(f));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool Save(Profile p)
        {
            try
            {
                File.WriteAllText(Path.Combine(Dir(), SafeName(p.Name) + ".vprofile"), p.ToText());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Delete(string name)
        {
            try
            {
                string f = Path.Combine(Dir(), SafeName(name) + ".vprofile");
                if (File.Exists(f)) File.Delete(f);
            }
            catch (Exception)
            {
            }
        }
    }

    static class ProfileApply
    {
        // Makes a profile the current settings and sends them to the devices.
        // markActive is true when the user chose it (it becomes the active profile).
        public static string Apply(Profile p, bool markActive)
        {
            AppState.LightColor = p.Light;
            AppState.Assignments.Clear();
            foreach (Assignment a in p.Assignments)
            {
                AppState.Assignments.Add(a);
            }
            AppState.BlockWin = p.BlockWin;
            AppState.BlockAltTab = p.BlockAltTab;
            AppState.BlockAltF4 = p.BlockAltF4;
            AppState.BlockMenu = p.BlockMenu;
            AppState.Dpi = p.Dpi;
            AppState.RateMs = p.RateMs;
            if (markActive)
            {
                AppState.ActiveProfile = p.Name;
                SettingsStore.Save();
            }
            KeyHook.Reload();

            string raw;
            StringBuilder sb = new StringBuilder();
            ScanResult s = AppState.Last;
            if (s != null && s.LinkPath.Length > 0)
            {
                sb.Append("Keyboard: " + KeyboardLight.SetAll(p.Light, out raw) + " ");
            }
            if (s != null && s.Mouse != null)
            {
                sb.Append("Mouse: " + MouseControl.SetDpi(p.Dpi, out raw) + " ");
                MouseControl.SetRate(p.RateMs, out raw);
            }
            AppState.RaiseLightChanged();
            AppState.RaiseSettingsChanged();
            if (sb.Length == 0)
            {
                return "Applied. No device is linked yet, so only the app settings changed.";
            }
            return sb.ToString().Trim();
        }
    }

    // The Profiles page.
    class ProfilesPage : Page
    {
        ListBox list;
        VButton save;
        VButton apply;
        VButton delete;
        VButton export;
        VButton import;
        Label status;
        List<string> names = new List<string>();

        public ProfilesPage()
        {
            list = new ListBox();
            list.BorderStyle = BorderStyle.None;
            list.BackColor = Theme.Panel;
            list.ForeColor = Theme.Text;
            list.Font = Theme.Body;
            list.IntegralHeight = false;

            save = new VButton("Save current settings as profile", true);
            save.Click += delegate(object sender, EventArgs e) { SaveNew(); };
            apply = new VButton("Apply selected", false);
            apply.Click += delegate(object sender, EventArgs e) { ApplySelected(); };
            delete = new VButton("Delete selected", false);
            delete.Click += delegate(object sender, EventArgs e) { DeleteSelected(); };
            export = new VButton("Export...", false);
            export.Click += delegate(object sender, EventArgs e) { ExportSelected(); };
            import = new VButton("Import...", false);
            import.Click += delegate(object sender, EventArgs e) { ImportOne(); };

            status = new Label();
            status.Font = Theme.Small;
            status.ForeColor = Theme.Muted;
            status.BackColor = Theme.Bg;

            Controls.Add(list);
            Controls.Add(save);
            Controls.Add(apply);
            Controls.Add(delete);
            Controls.Add(export);
            Controls.Add(import);
            Controls.Add(status);
            RefreshList();
            DoLayout();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && list != null)
            {
                RefreshList();
            }
        }

        void RefreshList()
        {
            names = ProfileStore.Names();
            list.Items.Clear();
            foreach (string n in names)
            {
                list.Items.Add(n == AppState.ActiveProfile ? n + "   (active)" : n);
            }
            if (names.Count == 0)
            {
                status.Text = "No profiles yet. Set things up, then press Save.";
            }
        }

        string Selected()
        {
            int i = list.SelectedIndex;
            if (i < 0 || i >= names.Count)
            {
                status.Text = "Pick a profile in the list first.";
                return null;
            }
            return names[i];
        }

        void SaveNew()
        {
            using (PromptDialog d = new PromptDialog("Save profile", "Name for this profile", AppState.ActiveProfile))
            {
                if (d.ShowDialog(FindForm()) == DialogResult.OK && d.Result != null)
                {
                    Profile p = Profile.FromCurrent(d.Result);
                    if (ProfileStore.Save(p))
                    {
                        AppState.ActiveProfile = p.Name;
                        SettingsStore.Save();
                        status.Text = "Saved profile " + p.Name + ".";
                    }
                    else
                    {
                        status.Text = "Could not save the profile.";
                    }
                    RefreshList();
                }
            }
        }

        void ApplySelected()
        {
            string n = Selected();
            if (n == null) return;
            Profile p = ProfileStore.Load(n);
            if (p == null)
            {
                status.Text = "Could not read that profile.";
                return;
            }
            status.Text = ProfileApply.Apply(p, true);
            RefreshList();
        }

        void DeleteSelected()
        {
            string n = Selected();
            if (n == null) return;
            ProfileStore.Delete(n);
            if (AppState.ActiveProfile == n)
            {
                AppState.ActiveProfile = "";
                SettingsStore.Save();
            }
            status.Text = "Deleted " + n + ".";
            RefreshList();
        }

        void ExportSelected()
        {
            string n = Selected();
            if (n == null) return;
            Profile p = ProfileStore.Load(n);
            if (p == null) return;
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "VOLTRIX profile (*.vprofile)|*.vprofile";
                dlg.FileName = ProfileStore.SafeName(n) + ".vprofile";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(dlg.FileName, p.ToText());
                        status.Text = "Exported to " + dlg.FileName;
                    }
                    catch (Exception ex)
                    {
                        status.Text = "Could not export: " + ex.Message;
                    }
                }
            }
        }

        void ImportOne()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "VOLTRIX profile (*.vprofile)|*.vprofile";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string name = Path.GetFileNameWithoutExtension(dlg.FileName);
                        Profile p = Profile.Parse(name, File.ReadAllText(dlg.FileName));
                        ProfileStore.Save(p);
                        status.Text = "Imported " + name + ".";
                        RefreshList();
                    }
                    catch (Exception ex)
                    {
                        status.Text = "Could not import: " + ex.Message;
                    }
                }
            }
        }

        void DoLayout()
        {
            int w = Math.Min(ClientSize.Width - 48, 760);
            if (w < 300) w = 300;
            int x = 24;
            save.SetBounds(x, 96, 300, 40);
            int ly = 150;
            int bottom = Math.Max(ly + 80, ClientSize.Height - 120);
            list.SetBounds(x, ly, w, bottom - ly);
            int by = bottom + 12;
            int bw = (w - 3 * 10) / 4;
            apply.SetBounds(x, by, bw, 40);
            delete.SetBounds(x + (bw + 10), by, bw, 40);
            export.SetBounds(x + 2 * (bw + 10), by, bw, 40);
            import.SetBounds(x + 3 * (bw + 10), by, bw, 40);
            status.SetBounds(x, by + 48, w, 40);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            DoLayout();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            TextRenderer.DrawText(e.Graphics, "Profiles", Theme.Title, new Rectangle(24, 24, 400, 50), Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }
}
