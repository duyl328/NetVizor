using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Infrastructure.utils;


public static class IconHelper
{
    [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    /// <summary>
    ///     获取 ICON
    /// </summary>
    /// <param name="exePath"></param>
    /// <returns></returns>
    public static string GetIconBase64(string? exePath)
    {
        if (exePath == null && !File.Exists(exePath)) return "";
        IntPtr hIcon = ExtractIcon(IntPtr.Zero, exePath, 0);
        if (hIcon == IntPtr.Zero) return "";

        using System.Drawing.Icon icon = Icon.FromHandle(hIcon);
        using Bitmap bmp = icon.ToBitmap();
        using MemoryStream ms = new MemoryStream();
        // 保存为 PNG 格式
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        byte[] bytes = ms.ToArray();
        return Convert.ToBase64String(bytes);
    }
}
