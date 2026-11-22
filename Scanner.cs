using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Voltrix
{
    // What a scan found.
    class ScanResult
    {
        public string Report = "";
        public string Summary = "";
        public bool FoundLogitech;
        public bool GHubRunning;
        public string LinkPath = "";
        public int LinkLen;
        public bool Hidpp2;
    }

    // Read-only scanner. It lists devices and asks a Logitech device simple questions
    // (protocol version, name, firmware, feature list). It never changes anything.
    static class Scanner
    {
        public static ScanResult Scan()
        {
            ScanResult res = new ScanResult();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(AppInfo.Name + " " + AppInfo.Full + " scan report");
            sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            string running = RunningLogitechApps();
            res.GHubRunning = running.Length > 0;
            if (res.GHubRunning)
            {
                sb.AppendLine("WARNING: Logitech software is running: " + running);
                sb.AppendLine("Close it (tray icon > Quit) before using VOLTRIX Hub with the keyboard.");
                sb.AppendLine();
            }

            List<HidInfo> all;
            try
            {
                all = HidNative.ListAll();
            }
            catch (Exception ex)
            {
                res.Summary = "Scan failed: " + ex.Message;
                sb.AppendLine(res.Summary);
                res.Report = sb.ToString();
                return res;
            }

            List<HidInfo> logi = new List<HidInfo>();
            foreach (HidInfo h in all)
            {
                if (h.Vid == 0x046D)
                {
                    logi.Add(h);
                }
            }
            sb.AppendLine("HID interfaces on this PC: " + all.Count + ". Logitech ones: " + logi.Count + ".");
            sb.AppendLine();

            if (logi.Count == 0)
            {
                res.Summary = "No Logitech device found. Is the keyboard plugged in?";
                sb.AppendLine(res.Summary);
                res.Report = sb.ToString();
                return res;
            }
            res.FoundLogitech = true;

            string bestName = "";
            foreach (HidInfo h in logi)
            {
                sb.AppendLine("Interface: " + h.Label());
                sb.AppendLine("  Product: " + h.Product + "  Maker: " + h.Maker + "  Version: " + h.Version.ToString("X4"));
                bool vendorPage = h.UsagePage == 0xFF00 || h.UsagePage == 0xFF43;
                if (!vendorPage)
                {
                    continue;
                }
                bool longReport = h.OutputLen >= 20;
                bool shortReport = h.OutputLen >= 7 && h.OutputLen < 20;
                if (!longReport && !shortReport)
                {
                    sb.AppendLine("  Skipped: no HID++ sized report.");
                    continue;
                }
                IntPtr handle = HidNative.OpenReadWrite(h.Path);
                if (handle == IntPtr.Zero)
                {
                    sb.AppendLine("  Could not open (in use by another program?).");
                    continue;
                }
                try
                {
                    Hidpp link = new Hidpp(handle, longReport, h.InputLen);
                    string name = "";
                    try
                    {
                        name = Probe(link, sb, res);
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("  Probe error: " + ex.Message);
                    }
                    if (name.Length > 0 && bestName.Length == 0)
                    {
                        bestName = name;
                    }
                    if (link.Answered && res.LinkPath.Length == 0)
                    {
                        res.LinkPath = h.Path;
                        res.LinkLen = h.OutputLen;
                    }
                }
                finally
                {
                    HidNative.Close(handle);
                }
            }

            if (bestName.Length > 0)
            {
                res.Summary = "Found: " + bestName;
            }
            else
            {
                res.Summary = "Logitech device found (" + logi[0].Product + "), but it did not answer the HID++ questions. Send the report to Claude.";
            }
            sb.AppendLine();
            sb.AppendLine("Result: " + res.Summary);
            res.Report = sb.ToString();
            return res;
        }

        static string RunningLogitechApps()
        {
            string[] names = new string[] { "lghub", "lghub_agent", "lghub_updater", "LCore", "LogiOptions", "logioptionsplus_agent" };
            List<string> found = new List<string>();
            foreach (string n in names)
            {
                try
                {
                    if (Process.GetProcessesByName(n).Length > 0)
                    {
                        found.Add(n);
                    }
                }
                catch (Exception)
                {
                }
            }
            return string.Join(", ", found.ToArray());
        }

        // Asks the device simple read-only questions. Returns the device name if known.
        static string Probe(Hidpp link, StringBuilder sb, ScanResult res)
        {
            byte[] r = link.Ask(0x00, 0x01, new byte[] { 0x00, 0x00, 0x5A });
            if (r == null)
            {
                sb.AppendLine("  HID++ ping: no answer.");
                return "";
            }
            if (link.IsError(r))
            {
                sb.AppendLine("  HID++ ping: error reply (" + Hex(r, 7) + "). Probably an older HID++ 1.0 style device.");
                link.Answered = true;
                return "";
            }
            link.Answered = true;
            res.Hidpp2 = true;
            sb.AppendLine("  HID++ protocol " + r[4] + "." + r[5]);

            int idxSet = link.FeatureIndex(0x0001);
            int idxName = link.FeatureIndex(0x0005);
            int idxFw = link.FeatureIndex(0x0003);

            string name = "";
            if (idxName > 0)
            {
                name = link.ReadName(idxName);
                sb.AppendLine("  Name: " + name);
            }
            if (idxFw > 0)
            {
                r = link.Ask((byte)idxFw, 0x00, new byte[0]);
                if (r != null && !link.IsError(r))
                {
                    int n = r[4];
                    for (int i = 0; i < n && i < 6; i++)
                    {
                        byte[] f = link.Ask((byte)idxFw, 0x01, new byte[] { (byte)i });
                        if (f != null && !link.IsError(f))
                        {
                            string prefix = Encoding.ASCII.GetString(f, 5, 3);
                            sb.AppendLine("  Firmware part " + i + ": type " + (f[4] & 0x0F) + " " + prefix.Trim('\0') + " " + f[8].ToString("X2") + "." + f[9].ToString("X2") + " build " + ((f[10] << 8) | f[11]).ToString("X4"));
                        }
                    }
                }
            }
            if (idxSet > 0)
            {
                r = link.Ask((byte)idxSet, 0x00, new byte[0]);
                if (r != null && !link.IsError(r))
                {
                    int count = r[4];
                    sb.AppendLine("  Features (" + count + "):");
                    for (int i = 1; i <= count && i <= 60; i++)
                    {
                        byte[] f = link.Ask((byte)idxSet, 0x01, new byte[] { (byte)i });
                        if (f != null && !link.IsError(f))
                        {
                            int id = (f[4] << 8) | f[5];
                            sb.AppendLine("    index " + i + ": 0x" + id.ToString("X4") + " " + FeatureName(id));
                        }
                    }
                }
            }
            return name;
        }

        static string FeatureName(int id)
        {
            switch (id)
            {
                case 0x0000: return "Root";
                case 0x0001: return "Feature set";
                case 0x0003: return "Firmware version";
                case 0x0005: return "Device name";
                case 0x1000: return "Battery";
                case 0x1004: return "Battery (unified)";
                case 0x1B04: return "Reprogrammable keys";
                case 0x4521: return "Disable keys";
                case 0x4522: return "Game mode";
                case 0x8010: return "G-keys";
                case 0x8020: return "M-keys";
                case 0x8030: return "MR key";
                case 0x8040: return "Brightness control";
                case 0x8070: return "Colour LED effects";
                case 0x8071: return "RGB effects";
                case 0x8080: return "Per-key lighting";
                case 0x8081: return "Per-key lighting v2";
                case 0x8100: return "Onboard profiles";
                case 0x8110: return "Mouse button spy";
                default: return "";
            }
        }

        public static string Hex(byte[] d, int max)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < d.Length && i < max; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(d[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }

    // A tiny HID++ 2.0 helper for one open interface.
    class Hidpp
    {
        IntPtr handle;
        bool useLong;
        int inLen;
        public bool Answered;

        public Hidpp(IntPtr h, bool longReport, int inputLen)
        {
            handle = h;
            useLong = longReport;
            inLen = inputLen < 20 ? 20 : inputLen;
        }

        public bool IsError(byte[] r)
        {
            return r.Length > 2 && (r[2] == 0xFF || r[2] == 0x8F);
        }

        // Sends one question to feature index "feature", function "fn", and waits for the answer.
        public byte[] Ask(byte feature, int fn, byte[] args)
        {
            int size = useLong ? 20 : 7;
            byte[] pkt = new byte[size];
            pkt[0] = useLong ? (byte)0x11 : (byte)0x10;
            pkt[1] = 0xFF;
            pkt[2] = feature;
            byte sw = 0x0A;
            pkt[3] = (byte)((fn << 4) | sw);
            for (int i = 0; i < args.Length && 4 + i < size; i++)
            {
                pkt[4 + i] = args[i];
            }
            if (!HidNative.Write(handle, pkt))
            {
                return null;
            }
            for (int tries = 0; tries < 6; tries++)
            {
                byte[] r = HidNative.Read(handle, inLen, 400);
                if (r == null)
                {
                    return null;
                }
                if (r.Length > 3 && (r[0] == 0x10 || r[0] == 0x11 || r[0] == 0x12))
                {
                    if ((r[2] == feature && r[3] == pkt[3]) || r[2] == 0xFF || r[2] == 0x8F)
                    {
                        return r;
                    }
                }
            }
            return null;
        }

        // Finds the index of a feature in this device. 0 means not found.
        public int FeatureIndex(int featureId)
        {
            byte[] r = Ask(0x00, 0x00, new byte[] { (byte)(featureId >> 8), (byte)(featureId & 0xFF) });
            if (r == null || IsError(r) || r.Length < 5)
            {
                return 0;
            }
            return r[4];
        }

        public string ReadName(int idx)
        {
            byte[] r = Ask((byte)idx, 0x00, new byte[0]);
            if (r == null || IsError(r))
            {
                return "";
            }
            int len = r[4];
            StringBuilder sb = new StringBuilder();
            int offset = 0;
            while (offset < len && offset < 64)
            {
                byte[] c = Ask((byte)idx, 0x01, new byte[] { (byte)offset });
                if (c == null || IsError(c))
                {
                    break;
                }
                int take = 0;
                for (int i = 4; i < c.Length && offset + take < len; i++)
                {
                    if (c[i] == 0) break;
                    sb.Append((char)c[i]);
                    take++;
                }
                if (take == 0) break;
                offset += take;
            }
            return sb.ToString();
        }
    }
}
