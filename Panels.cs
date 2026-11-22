using System;
using System.Drawing;
using System.Windows.Forms;

namespace Voltrix
{
    // The Assignments tab: a list of key changes.
    class AssignPanel : Panel
    {
        VToggle onToggle;
        ListBox list;
        VButton add;
        VButton remove;

        public AssignPanel()
        {
            BackColor = Theme.Panel;
            DoubleBuffered = true;

            onToggle = new VToggle("Key assignments are on");
            onToggle.Checked = AppState.AssignmentsOn;
            onToggle.Changed += delegate(object sender, EventArgs e)
            {
                AppState.AssignmentsOn = onToggle.Checked;
                SettingsStore.Save();
                AppState.RaiseSettingsChanged();
            };

            list = new ListBox();
            list.BorderStyle = BorderStyle.None;
            list.BackColor = Theme.Panel2;
            list.ForeColor = Theme.Text;
            list.Font = Theme.Body;
            list.IntegralHeight = false;

            add = new VButton("Add assignment", true);
            add.Click += delegate(object sender, EventArgs e) { AddOne(); };
            remove = new VButton("Remove selected", false);
            remove.Click += delegate(object sender, EventArgs e) { RemoveOne(); };

            Controls.Add(onToggle);
            Controls.Add(list);
            Controls.Add(add);
            Controls.Add(remove);

            AppState.SettingsChanged += delegate(object sender, EventArgs e) { onToggle.Checked = AppState.AssignmentsOn; };
            RefreshList();
            DoLayout();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && list != null && onToggle != null)
            {
                RefreshList();
                onToggle.Checked = AppState.AssignmentsOn;
            }
        }

        void RefreshList()
        {
            list.Items.Clear();
            foreach (Assignment a in AppState.Assignments)
            {
                list.Items.Add(a.Describe());
            }
        }

        void AddOne()
        {
            using (AssignDialog d = new AssignDialog())
            {
                if (d.ShowDialog(FindForm()) == DialogResult.OK && d.Result != null)
                {
                    AppState.Assignments.RemoveAll(delegate(Assignment x) { return x.Vk == d.Result.Vk; });
                    AppState.Assignments.Add(d.Result);
                    SettingsStore.Save();
                    KeyHook.Reload();
                    RefreshList();
                }
            }
        }

        void RemoveOne()
        {
            int i = list.SelectedIndex;
            if (i < 0 || i >= AppState.Assignments.Count) return;
            AppState.Assignments.RemoveAt(i);
            SettingsStore.Save();
            KeyHook.Reload();
            RefreshList();
        }

        void DoLayout()
        {
            int w = Width - 32;
            if (w < 100) w = 100;
            onToggle.SetBounds(16, 100, w, 36);
            int top = 148;
            int bottom = Math.Max(top + 60, Height - 68);
            list.SetBounds(16, top, w, bottom - top);
            int half = (w - 12) / 2;
            add.SetBounds(16, Height - 56, half, 40);
            remove.SetBounds(16 + half + 12, Height - 56, half, 40);
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
            TextRenderer.DrawText(g, "Assignments", Theme.Big, new Rectangle(16, 14, Width - 32, 36), Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "Change what a key does. Works only while VOLTRIX Hub is running. Tray icon > Pause is your emergency switch.", Theme.Small, new Rectangle(16, 54, Width - 32, 40), Theme.Muted, TextFormatFlags.Left | TextFormatFlags.WordBreak);
        }
    }

    // The Game Mode tab: blocked keys and per-game colours.
    class GamePanel : Panel
    {
        VToggle manual;
        VToggle win;
        VToggle altTab;
        VToggle altF4;
        VToggle menu;
        VToggle auto;
        ListBox list;
        VButton add;
        VButton remove;
        Label status;
        Label gamesTitle;

        public GamePanel()
        {
            BackColor = Theme.Panel;
            DoubleBuffered = true;
            AutoScroll = true;
            AutoScrollMinSize = new Size(0, 640);

            Label title = new Label();
            title.Text = "Game Mode";
            title.Font = Theme.Big;
            title.ForeColor = Theme.Text;
            title.BackColor = Theme.Panel;
            title.SetBounds(16, 14, 300, 36);
            Controls.Add(title);

            Label desc = new Label();
            desc.Text = "Game Mode blocks keys that can throw you out of a game. It turns on by itself when a game from your list is running.";
            desc.Font = Theme.Small;
            desc.ForeColor = Theme.Muted;
            desc.BackColor = Theme.Panel;
            desc.SetBounds(16, 54, 460, 40);
            Controls.Add(desc);

            manual = MakeToggle("Game Mode on now", 100);
            win = MakeToggle("Block the Windows keys", 144);
            altTab = MakeToggle("Block Alt + Tab", 188);
            altF4 = MakeToggle("Block Alt + F4", 232);
            menu = MakeToggle("Block the Menu key", 276);
            auto = MakeToggle("Turn on by itself when a game below runs", 320);
            SyncToggles();

            gamesTitle = new Label();
            gamesTitle.Text = "Your games";
            gamesTitle.Font = Theme.Bold;
            gamesTitle.ForeColor = Theme.Text;
            gamesTitle.BackColor = Theme.Panel;
            gamesTitle.SetBounds(16, 376, 300, 24);
            Controls.Add(gamesTitle);

            list = new ListBox();
            list.BorderStyle = BorderStyle.None;
            list.BackColor = Theme.Panel2;
            list.ForeColor = Theme.Text;
            list.Font = Theme.Body;
            list.IntegralHeight = false;
            list.SetBounds(16, 406, 440, 120);
            Controls.Add(list);

            add = new VButton("Add game", true);
            add.SetBounds(16, 536, 214, 40);
            add.Click += delegate(object sender, EventArgs e) { AddOne(); };
            Controls.Add(add);
            remove = new VButton("Remove selected", false);
            remove.SetBounds(242, 536, 214, 40);
            remove.Click += delegate(object sender, EventArgs e) { RemoveOne(); };
            Controls.Add(remove);

            status = new Label();
            status.Font = Theme.Small;
            status.ForeColor = Theme.Muted;
            status.BackColor = Theme.Panel;
            status.SetBounds(16, 590, 440, 24);
            Controls.Add(status);

            AppState.SettingsChanged += delegate(object sender, EventArgs e)
            {
                SyncToggles();
                UpdateStatus();
            };
            RefreshList();
            UpdateStatus();
        }

        VToggle MakeToggle(string text, int y)
        {
            VToggle t = new VToggle(text);
            t.SetBounds(16, y, 440, 36);
            t.Changed += delegate(object sender, EventArgs e) { Collect(); };
            Controls.Add(t);
            return t;
        }

        void SyncToggles()
        {
            manual.Checked = AppState.GameManual;
            win.Checked = AppState.BlockWin;
            altTab.Checked = AppState.BlockAltTab;
            altF4.Checked = AppState.BlockAltF4;
            menu.Checked = AppState.BlockMenu;
            auto.Checked = AppState.GameAuto;
        }

        void Collect()
        {
            AppState.GameManual = manual.Checked;
            AppState.BlockWin = win.Checked;
            AppState.BlockAltTab = altTab.Checked;
            AppState.BlockAltF4 = altF4.Checked;
            AppState.BlockMenu = menu.Checked;
            AppState.GameAuto = auto.Checked;
            SettingsStore.Save();
            if (AppState.Watcher != null)
            {
                AppState.Watcher.Apply();
            }
            UpdateStatus();
        }

        void UpdateStatus()
        {
            string game = AppState.Watcher == null ? null : AppState.Watcher.ActiveGame;
            if (game != null)
            {
                status.Text = "Game running: " + game + ". Game Mode is on.";
            }
            else if (AppState.GameBlocking)
            {
                status.Text = "Game Mode is on.";
            }
            else
            {
                status.Text = "Game Mode is off.";
            }
        }

        void RefreshList()
        {
            list.Items.Clear();
            foreach (GameEntry g in AppState.Games)
            {
                list.Items.Add(g.Describe());
            }
        }

        void AddOne()
        {
            using (GameDialog d = new GameDialog())
            {
                if (d.ShowDialog(FindForm()) == DialogResult.OK && d.Result != null)
                {
                    AppState.Games.RemoveAll(delegate(GameEntry x) { return x.Exe.ToLower() == d.Result.Exe.ToLower(); });
                    AppState.Games.Add(d.Result);
                    SettingsStore.Save();
                    RefreshList();
                }
            }
        }

        void RemoveOne()
        {
            int i = list.SelectedIndex;
            if (i < 0 || i >= AppState.Games.Count) return;
            AppState.Games.RemoveAt(i);
            SettingsStore.Save();
            RefreshList();
        }
    }
}
