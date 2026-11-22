using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Voltrix
{
    // Info about one HID interface that Windows lists.
    class HidInfo
    {
        public string Path;
        public ushort Vid;
        public ushort Pid;
        public ushort Version;
        public ushort UsagePage;
        public ushort Usage;
        public int InputLen;
        public int OutputLen;
        public int FeatureLen;
        public string Product;
        public string Maker;

        public string Label()
        {
            return "VID " + Vid.ToString("X4") + " PID " + Pid.ToString("X4")
                + " usage page 0x" + UsagePage.ToString("X4") + " usage 0x" + Usage.ToString("X2")
                + " in/out/feat " + InputLen + "/" + OutputLen + "/" + FeatureLen;
        }
    }

    // Raw Windows HID calls (setupapi, hid.dll, kernel32). No extra libraries needed.
    static class HidNative
    {
        const uint DIGCF_PRESENT = 0x2;
        const uint DIGCF_DEVICEINTERFACE = 0x10;
        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const uint FILE_SHARE_READ = 1;
        const uint FILE_SHARE_WRITE = 2;
        const uint OPEN_EXISTING = 3;
        const uint FILE_FLAG_OVERLAPPED = 0x40000000;

        [StructLayout(LayoutKind.Sequential)]
        struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct OVERLAPPED
        {
            public IntPtr Internal;
            public IntPtr InternalHigh;
            public int Offset;
            public int OffsetHigh;
            public IntPtr hEvent;
        }

        [DllImport("hid.dll")]
        static extern void HidD_GetHidGuid(out Guid guid);

        [DllImport("hid.dll", SetLastError = true)]
        static extern bool HidD_GetAttributes(IntPtr h, ref HIDD_ATTRIBUTES attr);

        [DllImport("hid.dll", SetLastError = true)]
        static extern bool HidD_GetPreparsedData(IntPtr h, out IntPtr data);

        [DllImport("hid.dll", SetLastError = true)]
        static extern bool HidD_FreePreparsedData(IntPtr data);

        [DllImport("hid.dll")]
        static extern int HidP_GetCaps(IntPtr data, ref HIDP_CAPS caps);

        [DllImport("hid.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool HidD_GetProductString(IntPtr h, StringBuilder sb, int len);

        [DllImport("hid.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool HidD_GetManufacturerString(IntPtr h, StringBuilder sb, int len);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern IntPtr SetupDiGetClassDevs(ref Guid guid, IntPtr enumerator, IntPtr hwnd, uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr info, ref Guid guid, uint index, ref SP_DEVICE_INTERFACE_DATA data);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref SP_DEVICE_INTERFACE_DATA data, IntPtr detail, int size, out int required, IntPtr devInfo);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr h);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteFile(IntPtr h, IntPtr buf, uint len, out uint written, ref OVERLAPPED ov);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadFile(IntPtr h, IntPtr buf, uint len, out uint read, ref OVERLAPPED ov);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetOverlappedResult(IntPtr h, ref OVERLAPPED ov, out uint count, bool wait);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CancelIo(IntPtr h);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateEvent(IntPtr security, bool manual, bool initial, string name);

        [DllImport("kernel32.dll")]
        static extern uint WaitForSingleObject(IntPtr h, uint ms);

        static readonly IntPtr InvalidHandle = new IntPtr(-1);

        // Lists every HID interface Windows knows about. Only reads descriptions.
        public static List<HidInfo> ListAll()
        {
            List<HidInfo> list = new List<HidInfo>();
            Guid hid;
            HidD_GetHidGuid(out hid);
            IntPtr set = SetupDiGetClassDevs(ref hid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (set == InvalidHandle || set == IntPtr.Zero)
            {
                return list;
            }
            try
            {
                for (uint i = 0; ; i++)
                {
                    SP_DEVICE_INTERFACE_DATA did = new SP_DEVICE_INTERFACE_DATA();
                    did.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));
                    if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref hid, i, ref did))
                    {
                        break;
                    }
                    int needed;
                    SetupDiGetDeviceInterfaceDetail(set, ref did, IntPtr.Zero, 0, out needed, IntPtr.Zero);
                    if (needed <= 0)
                    {
                        continue;
                    }
                    IntPtr detail = Marshal.AllocHGlobal(needed);
                    try
                    {
                        // cbSize of the detail struct: 8 on 64-bit, 6 on 32-bit (Unicode).
                        Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                        if (!SetupDiGetDeviceInterfaceDetail(set, ref did, detail, needed, out needed, IntPtr.Zero))
                        {
                            continue;
                        }
                        string path = Marshal.PtrToStringUni(new IntPtr(detail.ToInt64() + 4));
                        HidInfo info = Describe(path);
                        if (info != null)
                        {
                            list.Add(info);
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(detail);
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(set);
            }
            return list;
        }

        static HidInfo Describe(string path)
        {
            // Access 0 lets us read descriptions even when Windows owns the device.
            IntPtr h = CreateFile(path, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (h == InvalidHandle || h == IntPtr.Zero)
            {
                return null;
            }
            try
            {
                HIDD_ATTRIBUTES a = new HIDD_ATTRIBUTES();
                a.Size = Marshal.SizeOf(typeof(HIDD_ATTRIBUTES));
                if (!HidD_GetAttributes(h, ref a))
                {
                    return null;
                }
                HidInfo info = new HidInfo();
                info.Path = path;
                info.Vid = a.VendorID;
                info.Pid = a.ProductID;
                info.Version = a.VersionNumber;
                IntPtr pp;
                if (HidD_GetPreparsedData(h, out pp))
                {
                    HIDP_CAPS caps = new HIDP_CAPS();
                    caps.Reserved = new ushort[17];
                    HidP_GetCaps(pp, ref caps);
                    info.UsagePage = caps.UsagePage;
                    info.Usage = caps.Usage;
                    info.InputLen = caps.InputReportByteLength;
                    info.OutputLen = caps.OutputReportByteLength;
                    info.FeatureLen = caps.FeatureReportByteLength;
                    HidD_FreePreparsedData(pp);
                }
                StringBuilder sb = new StringBuilder(256);
                info.Product = HidD_GetProductString(h, sb, 512) ? sb.ToString() : "";
                sb = new StringBuilder(256);
                info.Maker = HidD_GetManufacturerString(h, sb, 512) ? sb.ToString() : "";
                return info;
            }
            finally
            {
                CloseHandle(h);
            }
        }

        public static IntPtr OpenReadWrite(string path)
        {
            IntPtr h = CreateFile(path, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);
            if (h == InvalidHandle)
            {
                return IntPtr.Zero;
            }
            return h;
        }

        public static void Close(IntPtr h)
        {
            if (h != IntPtr.Zero)
            {
                CloseHandle(h);
            }
        }

        // Writes one output report. The first byte is the report ID.
        public static bool Write(IntPtr h, byte[] data)
        {
            IntPtr buf = Marshal.AllocHGlobal(data.Length);
            IntPtr evt = CreateEvent(IntPtr.Zero, true, false, null);
            try
            {
                Marshal.Copy(data, 0, buf, data.Length);
                OVERLAPPED ov = new OVERLAPPED();
                ov.hEvent = evt;
                uint written;
                bool ok = WriteFile(h, buf, (uint)data.Length, out written, ref ov);
                if (!ok)
                {
                    if (Marshal.GetLastWin32Error() != 997)
                    {
                        return false;
                    }
                    if (WaitForSingleObject(evt, 1000) != 0)
                    {
                        CancelIo(h);
                        return false;
                    }
                    if (!GetOverlappedResult(h, ref ov, out written, false))
                    {
                        return false;
                    }
                }
                return true;
            }
            finally
            {
                CloseHandle(evt);
                Marshal.FreeHGlobal(buf);
            }
        }

        // Reads one input report, waiting up to timeoutMs. Returns null on timeout.
        public static byte[] Read(IntPtr h, int length, int timeoutMs)
        {
            IntPtr buf = Marshal.AllocHGlobal(length);
            IntPtr evt = CreateEvent(IntPtr.Zero, true, false, null);
            try
            {
                OVERLAPPED ov = new OVERLAPPED();
                ov.hEvent = evt;
                uint got;
                bool ok = ReadFile(h, buf, (uint)length, out got, ref ov);
                if (!ok)
                {
                    if (Marshal.GetLastWin32Error() != 997)
                    {
                        return null;
                    }
                    if (WaitForSingleObject(evt, (uint)timeoutMs) != 0)
                    {
                        CancelIo(h);
                        // Wait for the cancel to finish before freeing the buffer.
                        GetOverlappedResult(h, ref ov, out got, true);
                        return null;
                    }
                    if (!GetOverlappedResult(h, ref ov, out got, false))
                    {
                        return null;
                    }
                }
                byte[] result = new byte[got];
                Marshal.Copy(buf, result, 0, (int)got);
                return result;
            }
            finally
            {
                CloseHandle(evt);
                Marshal.FreeHGlobal(buf);
            }
        }
    }
}
