// // TaskbarEmbedSample.cs
//
// using System;
// using System.Collections.Generic;
// using System.Drawing;
// using System.Runtime.InteropServices;
// using System.Text;
// using System.Windows.Forms;
// using Microsoft.Win32; // for system events
//
// namespace TaskbarEmbedSample
// {
//     internal static class Program
//     {
//         [STAThread]
//         private static void Main()
//         {
//             Application.EnableVisualStyles();
//             Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
//             Application.SetCompatibleTextRenderingDefault(false);
//             Application.Run(new TaskbarEmbedForm());
//         }
//     }
//
//     /// <summary>
//     /// Small child window that is re‑parented into the task‑bar notification area (like TrafficMonitor).
//     /// Only shows a label that updates once per second, but you can replace the logic with real
//     /// network‑speed code.
//     /// </summary>
//     public class TaskbarEmbedForm : Form
//     {
//         private readonly Label _label;
//         private readonly Timer _timer;
//
//         public TaskbarEmbedForm()
//         {
//             FormBorderStyle = FormBorderStyle.None;
//             ShowInTaskbar = false; // do not show in Alt‑Tab or task‑bar button list
//             BackColor = Color.FromArgb(60, 180, 240); // fully transparent sample (see TransparencyKey)
//             TransparencyKey = Color.Magenta;
//             TopMost = false; // child of task‑bar – no need to stay on top
//
//             _label = new Label
//             {
//                 AutoSize = false,
//                 Dock = DockStyle.Fill,
//                 TextAlign = ContentAlignment.MiddleCenter,
//                 ForeColor = Color.White,
//                 Font = new Font("Segoe UI", 9, FontStyle.Bold),
//             };
//             Controls.Add(_label);
//
//             _timer = new Timer { Interval = 1000 };
//             _timer.Tick += (_, __) => UpdateDisplay();
//             _timer.Start();
//
//             Load += (_, __) => EmbedIntoTaskbar();
//             // Re‑embed when DPI / layout changes (monitor attach, task‑bar moved etc.)
//             SystemEvents.DisplaySettingsChanged += (_, __) => EmbedIntoTaskbar();
//             SystemEvents.UserPreferenceChanged += (_, __) => EmbedIntoTaskbar();
//         }
//
//         // Dummy payload – replace with real throughput metrics.
//         private void UpdateDisplay() => _label.Text = DateTime.Now.ToLongTimeString();
//
//         #region Win32 interop & constants
//
//         private const int GWL_STYLE = -16;
//         private const int GWL_EXSTYLE = -20;
//         private const int WS_CHILD = 0x40000000;
//         private const int WS_POPUP = unchecked((int)0x80000000);
//         private const int WS_EX_APPWINDOW = 0x00040000;
//         private const uint SWP_NOZORDER = 0x0004;
//         private const uint SWP_SHOWWINDOW = 0x0040;
//
//         [DllImport("user32.dll", SetLastError = true)]
//         private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);
//
//         [DllImport("user32.dll", SetLastError = true)]
//         private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter,
//             string lpszClass, string? lpszWindow);
//
//         [DllImport("user32.dll", SetLastError = true)]
//         private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
//
//         [DllImport("user32.dll", SetLastError = true)]
//         private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
//
//         [StructLayout(LayoutKind.Sequential)]
//         private struct RECT
//         {
//             public int left, top, right, bottom;
//         }
//
//         [DllImport("user32.dll", SetLastError = true)]
//         private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
//
//         [DllImport("user32.dll", SetLastError = true)]
//         private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
//             int X, int Y, int cx, int cy, uint uFlags);
//
//         [DllImport("user32.dll")]
//         private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
//
//         [DllImport("user32.dll", SetLastError = true)]
//         public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
//
//         public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
//
//         [DllImport("user32.dll", SetLastError = true)]
//         public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
//
//         [DllImport("user32.dll", SetLastError = true)]
//         public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
//
//         [StructLayout(LayoutKind.Sequential)]
//         public struct POINT
//         {
//             public int x;
//             public int y;
//         }
//
//         [DllImport("user32.dll", SetLastError = true)]
//         private static extern int GetWindowTextLength(IntPtr hWnd);
//
//         [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
//         private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
//
//         #endregion
//
//         /// <summary>
//         /// 对齐 ↑ 折叠按钮
//         /// </summary>
//         // private void EmbedIntoTaskbar()
//         // {
//         //     IntPtr hTaskbar = FindWindow("Shell_TrayWnd", null);
//         //     if (hTaskbar == IntPtr.Zero) return;
//         //
//         //     IntPtr hTray = FindWindowEx(hTaskbar, IntPtr.Zero, "TrayNotifyWnd", null);
//         //     if (hTray == IntPtr.Zero) hTray = hTaskbar;
//         //
//         //     // 将窗口变成 TrayNotifyWnd 的子窗口
//         //     SetParent(Handle, hTray);
//         //
//         //     int style = GetWindowLong(Handle, GWL_STYLE);
//         //     style |= WS_CHILD;
//         //     style &= ~WS_POPUP;
//         //     SetWindowLong(Handle, GWL_STYLE, style);
//         //
//         //     int ex = GetWindowLong(Handle, GWL_EXSTYLE);
//         //     ex &= ~WS_EX_APPWINDOW;
//         //     SetWindowLong(Handle, GWL_EXSTYLE, ex);
//         //
//         //     // 获取 TrayNotifyWnd 区域
//         //     if (!GetWindowRect(hTray, out RECT trayRect)) return;
//         //     Rectangle trayBounds = new Rectangle(trayRect.left, trayRect.top, trayRect.right - trayRect.left, trayRect.bottom - trayRect.top);
//         //
//         //     // 尝试查找 "↑" 折叠按钮的位置
//         //     IntPtr arrowButton = IntPtr.Zero;
//         //     EnumChildWindows(hTray, (hwnd, lParam) =>
//         //     {
//         //         StringBuilder className = new StringBuilder(256);
//         //         GetClassName(hwnd, className, className.Capacity);
//         //         if (className.ToString() == "Button")
//         //         {
//         //             arrowButton = hwnd;
//         //             return false; // 找到第一个按钮就停止
//         //         }
//         //         return true;
//         //     }, IntPtr.Zero);
//         //
//         //     int x = 8; // 默认偏移
//         //     // if (arrowButton != IntPtr.Zero && GetWindowRect(arrowButton, out RECT btnRect))
//         //     // {
//         //     //     // 将屏幕坐标转换为 TrayNotifyWnd 客户区坐标
//         //     //     POINT pt = new POINT { x = btnRect.left, y = btnRect.top };
//         //     //     ScreenToClient(hTray, ref pt);
//         //     //
//         //     //     x = pt.x - 124; // ←←← 向左偏移我们窗口宽度 (可调)
//         //     // }
//         //     
//         //     // int width = 120;
//         //     // if (arrowButton != IntPtr.Zero && GetWindowRect(arrowButton, out RECT btnRect))
//         //     // {
//         //     //     POINT pt = new POINT { x = btnRect.left, y = btnRect.top };
//         //     //     ScreenToClient(hTray, ref pt);
//         //     //
//         //     //     width = 120;
//         //     //     x = pt.x - width; // ←←← 正确方式：窗口右对齐按钮左
//         //     // }
//         //
//         //     int width = 120;
//         //
//         //     if (arrowButton != IntPtr.Zero)
//         //         Console.WriteLine($"arrowButton HWND: {arrowButton}");
//         //     else
//         //         Console.WriteLine("arrowButton is IntPtr.Zero");
//         //
//         //     var windowRect = GetWindowRect(arrowButton, out RECT btnRect);
//         //     bool btnRectGot = windowRect;
//         //     Console.WriteLine($"GetWindowRect on arrowButton success: {btnRectGot}");
//         //
//         //     
//         //     if (arrowButton != IntPtr.Zero &&
//         //         windowRect &&
//         //         GetWindowRect(hTray, out RECT trayRectFull))
//         //     {
//         //         // 屏幕坐标变换：窗口右边对齐按钮左边
//         //         x = (btnRect.left - trayRectFull.left) - width;
//         //         Console.WriteLine($"按钮屏幕位置: {btnRect.left}, 托盘屏幕位置: {trayRectFull.left}");
//         //     }
//         //
//         //     if (hTray != IntPtr.Zero)
//         //     {
//         //         DumpAllChildWindows(hTray);
//         //     }
//         //     int height = trayBounds.Height - 4;
//         //     int y = 2;
//         //
//         //     Console.WriteLine($"计算出的 x = {x}");
//         //
//         //     
//         //     SetWindowPos(Handle, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_SHOWWINDOW);
//         // }
//         private void EmbedIntoTaskbar()
//         {
//             IntPtr hTaskbar = FindWindow("Shell_TrayWnd", null);
//             if (hTaskbar == IntPtr.Zero) return;
//
//             IntPtr hTray = FindWindowEx(hTaskbar, IntPtr.Zero, "TrayNotifyWnd", null);
//             if (hTray == IntPtr.Zero) hTray = hTaskbar;
//
//             // 将窗口变成 TrayNotifyWnd 的子窗口
//             SetParent(Handle, hTray);
//
//             int style = GetWindowLong(Handle, GWL_STYLE);
//             style |= WS_CHILD;
//             style &= ~WS_POPUP;
//             SetWindowLong(Handle, GWL_STYLE, style);
//
//             int ex = GetWindowLong(Handle, GWL_EXSTYLE);
//             ex &= ~WS_EX_APPWINDOW;
//             SetWindowLong(Handle, GWL_EXSTYLE, ex);
//
//             // 获取 TrayNotifyWnd 区域
//             if (!GetWindowRect(hTray, out RECT trayRect)) return;
//             Rectangle trayBounds = new Rectangle(
//                 trayRect.left,
//                 trayRect.top,
//                 trayRect.right - trayRect.left,
//                 trayRect.bottom - trayRect.top
//             );
//
//             // 尝试查找 "↑" 折叠按钮的位置
//             IntPtr arrowButton = IntPtr.Zero;
//             EnumChildWindows(hTray, (hwnd, lParam) =>
//             {
//                 StringBuilder className = new StringBuilder(256);
//                 GetClassName(hwnd, className, className.Capacity);
//                 if (className.ToString() == "Button")
//                 {
//                     // 可以进一步验证是否是折叠按钮
//                     // 通常折叠按钮的文本是空的或特定字符
//                     arrowButton = hwnd;
//                     return false; // 找到第一个按钮就停止
//                 }
//
//                 return true;
//             }, IntPtr.Zero);
//
//             // 设置默认位置和尺寸
//             int x = 8;
//             int width = 120;
//             int height = trayBounds.Height - 4;
//             int y = 2;
//
//             // 如果找到了折叠按钮，计算正确的位置
//             if (arrowButton != IntPtr.Zero && GetWindowRect(arrowButton, out RECT btnRect))
//             {
//                 // 关键：将按钮的屏幕坐标转换为相对于 TrayNotifyWnd 客户区的坐标
//                 POINT btnLeftTop = new POINT { x = btnRect.left, y = btnRect.top };
//                 ScreenToClient(hTray, ref btnLeftTop);
//
//                 // 计算窗口位置：窗口右边缘对齐按钮左边缘
//                 // 所以窗口左边缘 = 按钮左边缘 - 窗口宽度
//                 x = btnLeftTop.x - width;
//
//                 // 如果计算出的位置太靠左（可能是负值），则使用默认值
//                 if (x < 0) x = 8;
//
//                 Console.WriteLine($"按钮客户区坐标: {btnLeftTop.x}, 窗口位置: {x}");
//             }
//
//             // 设置窗口位置
//             SetWindowPos(Handle, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_SHOWWINDOW);
//         }
//
//         private bool DumpAllChildWindows(IntPtr hwndParent, int indent = 0)
//         {
//             return EnumChildWindows(hwndParent, (hwnd, lParam) =>
//             {
//                 StringBuilder className = new StringBuilder(256);
//                 GetClassName(hwnd, className, className.Capacity);
//
//                 int textLength = GetWindowTextLength(hwnd);
//                 StringBuilder windowText = new StringBuilder(textLength + 1);
//                 GetWindowText(hwnd, windowText, windowText.Capacity);
//
//                 Console.WriteLine(
//                     $"{new string(' ', indent * 2)}HWND: {hwnd}, Class: {className}, Text: '{windowText}'");
//
//                 // 递归打印子窗口
//                 DumpAllChildWindows(hwnd, indent + 1);
//
//                 return true; // 返回true继续枚举兄弟窗口
//             }, IntPtr.Zero);
//         }
//
//         /// <summary>
//         /// 寻找按钮窗口
//         /// </summary>
//         /// <param name="hTray"></param>
//         private void DumpTrayNotifyWndChildren(IntPtr hTray)
//         {
//             Console.WriteLine("枚举 TrayNotifyWnd 子窗口:");
//
//             EnumChildWindows(hTray, (hwnd, lParam) =>
//             {
//                 StringBuilder className = new StringBuilder(256);
//                 GetClassName(hwnd, className, className.Capacity);
//
//                 int length = GetWindowTextLength(hwnd);
//                 StringBuilder windowText = new StringBuilder(length + 1);
//                 GetWindowText(hwnd, windowText, windowText.Capacity);
//
//                 Console.WriteLine($"HWND: {hwnd}, Class: {className}, Text: '{windowText}'");
//                 return true; // 继续枚举
//             }, IntPtr.Zero);
//         }
//     }
// }
// // 我在模仿TrafficMonitor进行实现任务栏信息的展示，现在已经能在任务栏展示的，但是现在我展示的窗口左侧在向上的折叠箭头的左侧，但是TrafficMonitor展示的是，窗口的最右侧和向上箭头的最左侧对齐的，我也想和他那样去实现，他是如何做的，我的实现哪里有问题嘛？我应该如何调整？
//
