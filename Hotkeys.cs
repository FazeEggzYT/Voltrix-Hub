using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Voltrix
{
    class HotkeyDef
    {
        public string Id;
        public string Label;
        public int Mods; // 1 = Ctrl, 2 = Alt, 4 = Shift
        public int Vk;
        public int DefMods;
        public int DefVk;
    }

    // Shortcuts for the app. They work anywhere, even inside games.
    static class Hotkeys
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vk);

        public static List<HotkeyDef> All = new List<HotkeyDef>();
        public static bool Capturing;
        public static Action<string> Handler;

        static Hotkeys()
        {
            Add("hud", "Show or hide the HUD", 3, 0x48);
            Add("cross", "Show or hide the crosshair", 3, 0x43);
            Add("window", "Show or hide this window", 3, 0x56);
            Add("assign", "Pause or resume key assignments", 3, 0x50);
            Add("game", "Turn Game Mode on or off", 3, 0x47);
        }

        static void Add(string id, string label, int mods, int vk)
        {
            HotkeyDef d = new HotkeyDef();
            d.Id = id;
            d.Label = label;
            d.Mods = mods;
            d.Vk = vk;
            d.DefMods = mods;
            d.DefVk = vk;
            All.Add(d);
        }

        public static void Set(string id, string text)
        {
            foreach (HotkeyDef d in All)
            {
                if (d.Id != id) continue;
                string[] p = text.Split(':');
                int m, v;
                if (p.Length == 2 && int.TryParse(p[0], out m) && int.TryParse(p[1], out v))
                {
                    d.Mods = m;
                    d.Vk = v;
                }
            }
        }

        public static void ResetAll()
        {
            foreach (HotkeyDef d in All)
            {
                d.Mods = d.DefMods;
                d.Vk = d.DefVk;
            }
        }

        static int CurrentMods()
        {
            int m = 0;
            if ((GetAsyncKeyState(0x11) & 0x8000) != 0) m |= 1;
            if ((GetAsyncKeyState(0x12) & 0x8000) != 0) m |= 2;
            if ((GetAsyncKeyState(0x10) & 0x8000) != 0) m |= 4;
            return m;
        }

        // Finds the shortcut for a key press, or null.
        public static HotkeyDef Match(int vk)
        {
            if (vk < 8) return null;
            int mods = CurrentMods();
            foreach (HotkeyDef d in All)
            {
                if (d.Vk == vk && d.Mods == mods)
                {
                    return d;
                }
            }
            return null;
        }

        public static void Fire(string id)
        {
            if (Handler != null)
            {
                Handler(id);
            }
        }

        public static string Describe(int mods, int vk)
        {
            if (vk == 0) return "Not set";
            string s = "";
            if ((mods & 1) != 0) s += "Ctrl+";
            if ((mods & 2) != 0) s += "Alt+";
            if ((mods & 4) != 0) s += "Shift+";
            return s + ((Keys)vk).ToString();
        }

        public static string Describe(HotkeyDef d)
        {
            return Describe(d.Mods, d.Vk);
        }
    }

    // A small window that waits for you to press the new shortcut.
    class HotkeyDialog : Form
    {
        Label info;
        Label shown;
        public int Mods;
        public int Vk;
        public bool Cleared;

        public HotkeyDialog(string name)
        {
            Text = "Change shortcut";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 190);
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Body;

            Label title = new Label();
            title.Text = name;
            title.Font = Theme.Bold;
            title.ForeColor = Theme.Text;
            title.BackColor = Theme.Bg;
            title.SetBounds(20, 16, 380, 26);
            Controls.Add(title);

            info = new Label();
            info.Text = "Press the new shortcut now. Use Ctrl, Alt or Shift with a key.";
            info.ForeColor = Theme.Muted;
            info.BackColor = Theme.Bg;
            info.SetBounds(20, 48, 380, 40);
            Controls.Add(info);

            shown = new Label();
            shown.Text = "...";
            shown.Font = Theme.Big;
            shown.ForeColor = Theme.Accent;
            shown.BackColor = Theme.Bg;
            shown.TextAlign = ContentAlignment.MiddleLeft;
            shown.SetBounds(20, 92, 380, 36);
            Controls.Add(shown);

            VButton clear = new VButton("Remove shortcut", false);
            clear.BackColor = Theme.Bg;
            clear.SetBounds(20, 136, 170, 40);
            clear.Click += delegate(object sender, EventArgs e)
            {
                Cleared = true;
                Mods = 0;
                Vk = 0;
                DialogResult = DialogResult.OK;
            };
            Controls.Add(clear);

            VButton cancel = new VButton("Cancel", false);
            cancel.BackColor = Theme.Bg;
            cancel.SetBounds(200, 136, 120, 40);
            cancel.Click += delegate(object sender, EventArgs e) { DialogResult = DialogResult.Cancel; };
            Controls.Add(cancel);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu)
            {
                return true;
            }
            int mods = 0;
            if ((keyData & Keys.Control) != 0) mods |= 1;
            if ((keyData & Keys.Alt) != 0) mods |= 2;
            if ((keyData & Keys.Shift) != 0) mods |= 4;
            if (key == Keys.Escape && mods == 0)
            {
                DialogResult = DialogResult.Cancel;
                return true;
            }
            if (mods == 0)
            {
                info.Text = "Hold Ctrl, Alt or Shift as well, so it does not clash with typing.";
                return true;
            }
            Mods = mods;
            Vk = (int)key;
            shown.Text = Hotkeys.Describe(Mods, Vk);
            DialogResult = DialogResult.OK;
            return true;
        }
    }
}
