using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Voltrix
{
    // Shared look for the small pop-up windows.
    class DialogBase : Form
    {
        protected Label MakeLabel(string text, Font font, Color color, int x, int y, int w, int h)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = font;
            l.ForeColor = color;
            l.BackColor = Theme.Panel;
            l.SetBounds(x, y, w, h);
            Controls.Add(l);
            return l;
        }

        protected ComboBox MakeCombo(int x, int y, int w, bool editable)
        {
            ComboBox c = new ComboBox();
            c.DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
            c.FlatStyle = FlatStyle.Flat;
            c.BackColor = Theme.Panel2;
            c.ForeColor = Theme.Text;
            c.Font = Theme.Body;
            c.SetBounds(x, y, w, 28);
            Controls.Add(c);
            return c;
        }

        public DialogBase(string title, int w, int h)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(w, h);
            BackColor = Theme.Panel;
            ForeColor = Theme.Text;
            Font = Theme.Body;
        }
    }

    // Add one key assignment: pick a key, pick what it should do.
    class AssignDialog : DialogBase
    {
        int srcVk = -1;
        int dstVk = -1;
        Label srcLabel;
        Label dstLabel;
        Label hint;
        Label message;
        VButton srcBtn;
        VButton dstBtn;
        ComboBox kind;
        ComboBox action;
        TextBox arg;

        public Assignment Result;

        static readonly string[] Kinds = new string[]
        {
            "Press another key", "Media or system action", "Open a program or file", "Type some text", "Disable this key"
        };

        public AssignDialog() : base("Add key assignment", 430, 370)
        {
            MakeLabel("1. Which key or mouse button do you want to change?", Theme.Bold, Theme.Text, 20, 16, 390, 24);
            srcBtn = new VButton("Press a key...", false);
            srcBtn.SetBounds(20, 46, 190, 40);
            srcBtn.Click += delegate(object sender, EventArgs e)
            {
                srcBtn.Text = "Now press the key...";
                KeyHook.Capture = delegate(int vk)
                {
                    srcVk = vk;
                    srcLabel.Text = Assignment.KeyName(vk);
                    srcBtn.Text = "Press a key...";
                };
            };
            Controls.Add(srcBtn);
            srcLabel = MakeLabel("(none yet)", Theme.Bold, Theme.Accent, 224, 46, 190, 40);
            srcLabel.TextAlign = ContentAlignment.MiddleLeft;

            MakeLabel("2. What should it do?", Theme.Bold, Theme.Text, 20, 104, 390, 24);
            kind = MakeCombo(20, 134, 390, false);
            kind.Items.AddRange(Kinds);
            kind.SelectedIndex = 0;
            kind.SelectedIndexChanged += delegate(object sender, EventArgs e) { ShowKind(); };

            dstBtn = new VButton("Press the new key...", false);
            dstBtn.SetBounds(20, 182, 190, 40);
            dstBtn.Click += delegate(object sender, EventArgs e)
            {
                dstBtn.Text = "Now press the key...";
                KeyHook.Capture = delegate(int vk)
                {
                    dstBtn.Text = "Press the new key...";
                    if (vk >= 4 && vk <= 6)
                    {
                        message.Text = "A mouse button can only be the thing you change, not the result.";
                        return;
                    }
                    message.Text = "";
                    dstVk = vk;
                    dstLabel.Text = Assignment.KeyName(vk);
                };
            };
            Controls.Add(dstBtn);
            dstLabel = MakeLabel("(none yet)", Theme.Bold, Theme.Accent, 224, 182, 190, 40);
            dstLabel.TextAlign = ContentAlignment.MiddleLeft;

            action = MakeCombo(20, 186, 390, false);
            action.Items.AddRange(KeyHook.ActionNames);
            action.SelectedIndex = 0;

            arg = new TextBox();
            arg.BorderStyle = BorderStyle.FixedSingle;
            arg.BackColor = Theme.Panel2;
            arg.ForeColor = Theme.Text;
            arg.Font = Theme.Body;
            arg.SetBounds(20, 186, 390, 28);
            Controls.Add(arg);

            hint = MakeLabel("", Theme.Small, Theme.Muted, 20, 226, 390, 40);
            message = MakeLabel("", Theme.Small, Color.FromArgb(255, 120, 120), 20, 276, 390, 22);

            VButton ok = new VButton("Save", true);
            ok.SetBounds(20, 314, 190, 40);
            ok.Click += delegate(object sender, EventArgs e) { TrySave(); };
            Controls.Add(ok);
            VButton cancel = new VButton("Cancel", false);
            cancel.SetBounds(224, 314, 186, 40);
            cancel.Click += delegate(object sender, EventArgs e) { DialogResult = DialogResult.Cancel; };
            Controls.Add(cancel);

            ShowKind();
        }

        void ShowKind()
        {
            int k = kind.SelectedIndex;
            dstBtn.Visible = k == 0;
            dstLabel.Visible = k == 0;
            action.Visible = k == 1;
            arg.Visible = k == 2 || k == 3;
            if (k == 0) hint.Text = "The key you press will act like the key you choose here.";
            else if (k == 1) hint.Text = "Runs a media key or a simple action.";
            else if (k == 2) hint.Text = "Type a program or file, for example notepad or C:\\Games\\app.exe";
            else if (k == 3) hint.Text = "Types this text each time you press the key.";
            else hint.Text = "The key will do nothing while VOLTRIX Hub runs.";
        }

        void TrySave()
        {
            if (srcVk < 0)
            {
                message.Text = "Choose the key to change first.";
                return;
            }
            Assignment a = new Assignment();
            a.Vk = srcVk;
            int k = kind.SelectedIndex;
            if (k == 0)
            {
                if (dstVk < 0)
                {
                    message.Text = "Press the new key first.";
                    return;
                }
                a.Kind = "key";
                a.Arg = dstVk.ToString();
            }
            else if (k == 1)
            {
                a.Kind = "action";
                a.Arg = action.SelectedItem.ToString();
            }
            else if (k == 2 || k == 3)
            {
                if (arg.Text.Trim().Length == 0)
                {
                    message.Text = "Type something first.";
                    return;
                }
                a.Kind = k == 2 ? "run" : "text";
                a.Arg = arg.Text.Trim();
            }
            else
            {
                a.Kind = "block";
                a.Arg = "";
            }
            Result = a;
            DialogResult = DialogResult.OK;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            KeyHook.Capture = null;
            base.OnFormClosing(e);
        }
    }

    // Add a game: pick a running program or type its name, and a colour.
    class GameDialog : DialogBase
    {
        ComboBox name;
        ComboBox profile;
        Panel swatch;
        Label message;
        Color colour = Color.FromArgb(255, 60, 60);

        public GameEntry Result;

        public GameDialog() : base("Add game", 430, 330)
        {
            MakeLabel("Program name (for example valorant or csgo)", Theme.Bold, Theme.Text, 20, 16, 390, 24);
            name = MakeCombo(20, 46, 390, true);
            List<string> names = new List<string>();
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.MainWindowTitle.Length > 0 && !names.Contains(p.ProcessName))
                        {
                            names.Add(p.ProcessName);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }
            names.Sort();
            name.Items.AddRange(names.ToArray());

            MakeLabel("Keyboard colour while it runs", Theme.Bold, Theme.Text, 20, 94, 390, 24);
            swatch = new Panel();
            swatch.BackColor = colour;
            swatch.SetBounds(20, 124, 40, 40);
            Controls.Add(swatch);
            VButton pick = new VButton("Choose colour...", false);
            pick.SetBounds(72, 124, 190, 40);
            pick.Click += delegate(object sender, EventArgs e)
            {
                using (ColorDialog dlg = new ColorDialog())
                {
                    dlg.FullOpen = true;
                    dlg.Color = colour;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        colour = Color.FromArgb(255, dlg.Color);
                        swatch.BackColor = colour;
                    }
                }
            };
            Controls.Add(pick);

            MakeLabel("Profile to switch to (optional)", Theme.Bold, Theme.Text, 20, 176, 390, 24);
            profile = MakeCombo(20, 206, 390, false);
            profile.Items.Add("(none, just the colour)");
            foreach (string pn in ProfileStore.Names())
            {
                profile.Items.Add(pn);
            }
            profile.SelectedIndex = 0;

            message = MakeLabel("", Theme.Small, Color.FromArgb(255, 120, 120), 20, 242, 390, 22);

            VButton ok = new VButton("Add", true);
            ok.SetBounds(20, 274, 190, 40);
            ok.Click += delegate(object sender, EventArgs e)
            {
                string n = name.Text.Trim();
                if (n.ToLower().EndsWith(".exe"))
                {
                    n = n.Substring(0, n.Length - 4);
                }
                if (n.Length == 0)
                {
                    message.Text = "Type or pick a program name.";
                    return;
                }
                GameEntry g = new GameEntry();
                g.Exe = n;
                g.Colour = colour;
                g.Profile = profile.SelectedIndex > 0 ? profile.SelectedItem.ToString() : "";
                Result = g;
                DialogResult = DialogResult.OK;
            };
            Controls.Add(ok);
            VButton cancel = new VButton("Cancel", false);
            cancel.SetBounds(224, 274, 186, 40);
            cancel.Click += delegate(object sender, EventArgs e) { DialogResult = DialogResult.Cancel; };
            Controls.Add(cancel);
        }
    }

    // Asks for one line of text.
    class PromptDialog : DialogBase
    {
        TextBox box;
        public string Result;

        public PromptDialog(string title, string label, string initial) : base(title, 400, 170)
        {
            MakeLabel(label, Theme.Bold, Theme.Text, 20, 16, 360, 24);
            box = new TextBox();
            box.BorderStyle = BorderStyle.FixedSingle;
            box.BackColor = Theme.Panel2;
            box.ForeColor = Theme.Text;
            box.Font = Theme.Body;
            box.Text = initial;
            box.SetBounds(20, 48, 360, 28);
            Controls.Add(box);
            VButton ok = new VButton("OK", true);
            ok.SetBounds(20, 108, 170, 40);
            ok.Click += delegate(object sender, EventArgs e)
            {
                if (box.Text.Trim().Length == 0) return;
                Result = box.Text.Trim();
                DialogResult = DialogResult.OK;
            };
            Controls.Add(ok);
            VButton cancel = new VButton("Cancel", false);
            cancel.SetBounds(210, 108, 170, 40);
            cancel.Click += delegate(object sender, EventArgs e) { DialogResult = DialogResult.Cancel; };
            Controls.Add(cancel);
        }
    }
}
