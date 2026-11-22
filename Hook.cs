using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Voltrix
{
    // Watches every key press on the PC (a Windows "low level keyboard hook").
    // It can change a key, run an action, or block a key. It only works while VOLTRIX Hub runs.
    static class KeyHook
    {
        [StructLayout(LayoutKind.Sequential)]
        struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr extra;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MSLLHOOKSTRUCT
        {
            public int x;
            public int y;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr extra;
        }

        delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int id, HookProc fn, IntPtr module, uint thread);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr GetModuleHandle(string name);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);

        const int WH_KEYBOARD_LL = 13;
        const int WH_MOUSE_LL = 14;
        const uint KEYUP = 2;

        static HookProc proc;
        static HookProc mouseProc;
        static IntPtr hook = IntPtr.Zero;
        static IntPtr mouseHook = IntPtr.Zero;
        static Control sync;
        static Dictionary<int, Assignment> map = new Dictionary<int, Assignment>();
        static Dictionary<int, int> held = new Dictionary<int, int>();
        static List<int> swallowed = new List<int>();

        // When set, the next key press is given to this function and not to Windows.
        public static Action<int> Capture;

        public static bool Installed
        {
            get { return hook != IntPtr.Zero; }
        }

        public static bool Install(Control owner)
        {
            if (hook != IntPtr.Zero) return true;
            sync = owner;
            proc = new HookProc(Handler);
            IntPtr module = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
            hook = SetWindowsHookEx(WH_KEYBOARD_LL, proc, module, 0);
            mouseProc = new HookProc(MouseHandler);
            mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, module, 0);
            Reload();
            return hook != IntPtr.Zero;
        }

        public static void Uninstall()
        {
            if (hook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hook);
                hook = IntPtr.Zero;
            }
            if (mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(mouseHook);
                mouseHook = IntPtr.Zero;
            }
        }

        // Rebuilds the lookup from the saved assignments.
        public static void Reload()
        {
            Dictionary<int, Assignment> m = new Dictionary<int, Assignment>();
            foreach (Assignment a in AppState.Assignments)
            {
                m[a.Vk] = a;
            }
            map = m;
        }

        static IntPtr Handler(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                try
                {
                    KBDLLHOOKSTRUCT k = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    bool injected = (k.flags & 0x10) != 0;
                    if (!injected)
                    {
                        int msg = wParam.ToInt32();
                        bool down = msg == 0x100 || msg == 0x104;
                        bool altDown = (k.flags & 0x20) != 0;
                        if (Handle((int)k.vkCode, down, altDown))
                        {
                            return new IntPtr(1);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return CallNextHookEx(hook, code, wParam, lParam);
        }

        // Middle button and the two side buttons act like keys 4, 5 and 6.
        static IntPtr MouseHandler(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                try
                {
                    MSLLHOOKSTRUCT m = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    if ((m.flags & 1) == 0)
                    {
                        int msg = wParam.ToInt32();
                        int vk = 0;
                        bool down = false;
                        if (msg == 0x207) { vk = 4; down = true; }
                        else if (msg == 0x208) { vk = 4; }
                        else if (msg == 0x20B || msg == 0x20C)
                        {
                            int which = (int)((m.mouseData >> 16) & 0xFFFF);
                            vk = which == 1 ? 5 : (which == 2 ? 6 : 0);
                            down = msg == 0x20B;
                        }
                        if (vk != 0 && Handle(vk, down, false))
                        {
                            return new IntPtr(1);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return CallNextHookEx(mouseHook, code, wParam, lParam);
        }

        // Returns true if this key press should NOT reach other programs.
        static bool Handle(int vk, bool down, bool altDown)
        {
            if (!down)
            {
                int target;
                if (held.TryGetValue(vk, out target))
                {
                    held.Remove(vk);
                    Send(target, false);
                    return true;
                }
                return swallowed.Remove(vk);
            }

            if (swallowed.Contains(vk))
            {
                return true; // key held down, repeating
            }

            if (Capture != null)
            {
                Action<int> cb = Capture;
                Capture = null;
                swallowed.Add(vk);
                int captured = vk;
                sync.BeginInvoke(new MethodInvoker(delegate { cb(captured); }));
                return true;
            }

            if (AppState.GameBlocking && IsBlocked(vk, altDown))
            {
                swallowed.Add(vk);
                return true;
            }

            if (AppState.AssignmentsOn)
            {
                Assignment a;
                if (map.TryGetValue(vk, out a))
                {
                    if (a.Kind == "key")
                    {
                        int target;
                        if (int.TryParse(a.Arg, out target))
                        {
                            held[vk] = target;
                            Send(target, true);
                            return true;
                        }
                        return false;
                    }
                    swallowed.Add(vk);
                    if (a.Kind != "block")
                    {
                        Assignment copy = a;
                        sync.BeginInvoke(new MethodInvoker(delegate { Run(copy); }));
                    }
                    return true;
                }
            }
            return false;
        }

        static bool IsBlocked(int vk, bool altDown)
        {
            if ((vk == 0x5B || vk == 0x5C) && AppState.BlockWin) return true;
            if (vk == 0x5D && AppState.BlockMenu) return true;
            if (vk == 0x09 && altDown && AppState.BlockAltTab) return true;
            if (vk == 0x73 && altDown && AppState.BlockAltF4) return true;
            return false;
        }

        static void Send(int vk, bool down)
        {
            uint flags = down ? 0u : KEYUP;
            bool extended = (vk >= 0x21 && vk <= 0x28) || vk == 0x2D || vk == 0x2E || vk == 0xA3 || vk == 0xA5 || vk == 0x5B || vk == 0x5C || vk == 0x5D;
            if (extended)
            {
                flags |= 1u;
            }
            keybd_event((byte)vk, 0, flags, UIntPtr.Zero);
        }

        static void Tap(int vk)
        {
            Send(vk, true);
            Send(vk, false);
        }

        public static readonly string[] ActionNames = new string[]
        {
            "Play / Pause", "Next track", "Previous track", "Mute", "Volume up", "Volume down", "Open Calculator", "Lock PC"
        };

        static void Run(Assignment a)
        {
            try
            {
                if (a.Kind == "action")
                {
                    switch (a.Arg)
                    {
                        case "Play / Pause": Tap(0xB3); break;
                        case "Next track": Tap(0xB0); break;
                        case "Previous track": Tap(0xB1); break;
                        case "Mute": Tap(0xAD); break;
                        case "Volume up": Tap(0xAF); break;
                        case "Volume down": Tap(0xAE); break;
                        case "Open Calculator": Process.Start("calc.exe"); break;
                        case "Lock PC": Process.Start("rundll32.exe", "user32.dll,LockWorkStation"); break;
                    }
                }
                else if (a.Kind == "run")
                {
                    Process.Start(a.Arg);
                }
                else if (a.Kind == "text")
                {
                    SendKeys.SendWait(EscapeForSendKeys(a.Arg));
                }
            }
            catch (Exception)
            {
            }
        }

        static string EscapeForSendKeys(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                if ("+^%~(){}[]".IndexOf(c) >= 0)
                {
                    sb.Append('{').Append(c).Append('}');
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }

    // Turns Game Mode on by itself when one of your games is running, and sets its colour.
    class GameWatcher
    {
        Timer timer;
        string active;
        Profile snapshot;

        public GameWatcher()
        {
            timer = new Timer();
            timer.Interval = 2000;
            timer.Tick += delegate(object sender, EventArgs e) { Check(); };
        }

        public void Start()
        {
            timer.Start();
            Apply();
        }

        public void Stop()
        {
            timer.Stop();
        }

        // Called when the Game Mode switches change.
        public void Apply()
        {
            AppState.GameBlocking = AppState.GameManual || active != null;
        }

        void Check()
        {
            GameEntry found = null;
            if (AppState.GameAuto)
            {
                foreach (GameEntry g in AppState.Games)
                {
                    try
                    {
                        if (g.Exe.Length > 0 && Process.GetProcessesByName(g.Exe).Length > 0)
                        {
                            found = g;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            string now = found == null ? null : found.Exe;
            if (now == active)
            {
                return;
            }
            active = now;
            Apply();
            string raw;
            if (found != null)
            {
                if (snapshot == null)
                {
                    snapshot = Profile.FromCurrent("snapshot");
                }
                Profile gp = found.Profile.Length > 0 ? ProfileStore.Load(found.Profile) : null;
                if (gp != null)
                {
                    ProfileApply.Apply(gp, false);
                }
                else
                {
                    KeyboardLight.SetAll(found.Colour, out raw);
                }
            }
            else
            {
                if (snapshot != null)
                {
                    ProfileApply.Apply(snapshot, false);
                    snapshot = null;
                }
                else
                {
                    KeyboardLight.SetAll(AppState.LightColor, out raw);
                }
            }
            AppState.RaiseSettingsChanged();
        }

        public string ActiveGame
        {
            get { return active; }
        }
    }
}
