using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Shell.UserControls;

namespace Shell.Utils;

public class TrayIconManager : IDisposable
{
    private NotifyIcon _notifyIcon;
    private Window _mainWindow;
    private TrayMenuWindow _trayMenu;
    private DispatcherTimer _clickTimer;
    private bool _isDoubleClick = false;

    // Win32 API 用于获取鼠标位置和显示器信息
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    // 添加DPI相关API
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, DpiType dpiType, out uint dpiX, out uint dpiY);

    public enum DpiType
    {
        Effective = 0,
        Angular = 1,
        Raw = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    public TrayIconManager(Window mainWindow)
    {
        _mainWindow = mainWindow;
        InitializeTrayIcon();
        InitializeClickTimer();
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Visible = true,
            Text = "你的应用程序"
        };

        _notifyIcon.MouseClick += OnTrayIconClick;
        _notifyIcon.MouseDoubleClick += OnTrayIconDoubleClick;
    }

    private void InitializeClickTimer()
    {
        _clickTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _clickTimer.Tick += OnClickTimerTick;
    }

    private Icon LoadIcon()
    {
        try
        {
            return new Icon("Assets/Icons/app.ico");
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    private void OnTrayIconClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            try
            {
                ShowContextMenu();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }
        else if (e.Button == MouseButtons.Left)
        {
            _isDoubleClick = false;
            _clickTimer.Start();
        }
    }

    private void OnTrayIconDoubleClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDoubleClick = true;
            _clickTimer.Stop();
            ShowMainWindow();
        }
    }

    private void OnClickTimerTick(object sender, EventArgs e)
    {
        _clickTimer.Stop();
        if (!_isDoubleClick)
        {
            // 单击逻辑（可选）
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            _mainWindow.Focus();
        }
    }

    private void ShowContextMenu()
    {
        Console.WriteLine("=== TrayIconManager ShowContextMenu ===");

        if (_trayMenu == null)
        {
            _trayMenu = new TrayMenuWindow();
            _trayMenu.OnShowMainWindow += () => ShowMainWindow();
            _trayMenu.OnExitApplication += () => System.Windows.Application.Current.Shutdown();
            _trayMenu.OnHideToTray += () => _mainWindow?.Hide();
            Console.WriteLine("创建新的TrayMenuWindow");
        }

        // 使用Win32 API获取鼠标位置
        if (GetCursorPos(out POINT mousePos))
        {
            Console.WriteLine($"Win32 API获取鼠标物理位置: X={mousePos.X}, Y={mousePos.Y}");

            // 获取鼠标所在显示器的工作区域和DPI信息
            var (workArea, dpiScaleX, dpiScaleY) = GetMouseMonitorInfoWithDpi(mousePos);
            Console.WriteLine(
                $"显示器工作区域: Left={workArea.Left}, Top={workArea.Top}, Right={workArea.Right}, Bottom={workArea.Bottom}");
            Console.WriteLine($"DPI缩放比例: ScaleX={dpiScaleX:F2}, ScaleY={dpiScaleY:F2}");

            // 将物理坐标转换为WPF逻辑坐标
            var logicalMouseX = mousePos.X / dpiScaleX;
            var logicalMouseY = mousePos.Y / dpiScaleY;

            // 将工作区域也转换为逻辑坐标
            var logicalWorkArea = new RECT
            {
                Left = (int)(workArea.Left / dpiScaleX),
                Top = (int)(workArea.Top / dpiScaleY),
                Right = (int)(workArea.Right / dpiScaleX),
                Bottom = (int)(workArea.Bottom / dpiScaleY)
            };

            Console.WriteLine($"逻辑鼠标位置: X={logicalMouseX:F1}, Y={logicalMouseY:F1}");
            Console.WriteLine(
                $"逻辑工作区域: Left={logicalWorkArea.Left}, Top={logicalWorkArea.Top}, Right={logicalWorkArea.Right}, Bottom={logicalWorkArea.Bottom}");

            _trayMenu.ShowAtWithWorkArea((int)logicalMouseX, (int)logicalMouseY, logicalWorkArea);
        }
        else
        {
            Console.WriteLine("获取鼠标位置失败，使用默认位置");
            // 如果获取鼠标位置失败，使用托盘图标的估计位置
            var workArea = SystemParameters.WorkArea;
            var defaultX = (int)workArea.Right - 50;
            var defaultY = (int)workArea.Bottom - 50;
            Console.WriteLine($"使用默认位置: X={defaultX}, Y={defaultY}");
            _trayMenu.ShowAt(defaultX, defaultY);
        }

        Console.WriteLine("=== ShowContextMenu End ===\n");
    }

    private (RECT workArea, double dpiScaleX, double dpiScaleY) GetMouseMonitorInfoWithDpi(POINT mousePos)
    {
        // 获取鼠标所在的显示器
        IntPtr hMonitor = MonitorFromPoint(mousePos, MONITOR_DEFAULTTONEAREST);

        // 获取显示器信息
        MONITORINFO monitorInfo = new MONITORINFO();
        monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);

        RECT workArea;
        double dpiScaleX = 1.0;
        double dpiScaleY = 1.0;

        if (GetMonitorInfo(hMonitor, ref monitorInfo))
        {
            workArea = monitorInfo.rcWork;

            // 获取该显示器的DPI
            try
            {
                if (GetDpiForMonitor(hMonitor, DpiType.Effective, out uint dpiX, out uint dpiY) == 0)
                {
                    dpiScaleX = dpiX / 96.0; // 96 DPI是标准DPI
                    dpiScaleY = dpiY / 96.0;
                    Console.WriteLine($"显示器DPI: X={dpiX}, Y={dpiY}");
                }
                else
                {
                    // 如果GetDpiForMonitor失败，尝试使用系统DPI
                    var systemDpi = GetDpiForSystem();
                    dpiScaleX = dpiScaleY = systemDpi / 96.0;
                    Console.WriteLine($"使用系统DPI: {systemDpi}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取DPI失败: {ex.Message}，使用默认缩放");
                // 作为后备方案，使用WPF的DPI信息
                var source = PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow);
                if (source?.CompositionTarget != null)
                {
                    var matrix = source.CompositionTarget.TransformFromDevice;
                    dpiScaleX = 1.0 / matrix.M11;
                    dpiScaleY = 1.0 / matrix.M22;
                }
            }
        }
        else
        {
            // 如果获取失败，返回主显示器的工作区域
            var wpfWorkArea = SystemParameters.WorkArea;
            workArea = new RECT
            {
                Left = (int)wpfWorkArea.Left,
                Top = (int)wpfWorkArea.Top,
                Right = (int)wpfWorkArea.Right,
                Bottom = (int)wpfWorkArea.Bottom
            };

            // 使用WPF的DPI信息作为后备
            var source = PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow);
            if (source?.CompositionTarget != null)
            {
                var matrix = source.CompositionTarget.TransformFromDevice;
                dpiScaleX = 1.0 / matrix.M11;
                dpiScaleY = 1.0 / matrix.M22;
            }
        }

        return (workArea, dpiScaleX, dpiScaleY);
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
        _trayMenu?.Close();
        _clickTimer?.Stop();
    }
}