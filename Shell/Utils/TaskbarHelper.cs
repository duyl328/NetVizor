namespace Shell.Utils;

using System;
using System.Runtime.InteropServices;

public static class TaskbarHelper
{
    [DllImport("shell32.dll", SetLastError = true)]
    static extern UInt32 SHAppBarMessage(UInt32 dwMessage, ref APPBARDATA pData);

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public UInt32 cbSize;
        public IntPtr hWnd;
        public UInt32 uCallbackMessage;
        public UInt32 uEdge;
        public RECT rc;
        public Int32 lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left, top, right, bottom;
    }

    public static RECT GetTaskbarPosition()
    {
        APPBARDATA data = new APPBARDATA();
        data.cbSize = (UInt32)Marshal.SizeOf(typeof(APPBARDATA));
        SHAppBarMessage(0x00000005, ref data); // ABM_GETTASKBARPOS
        return data.rc;
    }
}
