using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Shell.Views;

namespace Shell.Utils;

public class TrayIconManager : IDisposable
{
    private NotifyIcon _notifyIcon;
    private Window _mainWindow;
    private ContextMenu _contextMenu;
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
        InitializeContextMenu();
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

    private void InitializeContextMenu()
    {
        _contextMenu = new ContextMenu();

        // 添加失去焦点时关闭事件
        _contextMenu.Closed += (s, e) =>
        {
            /* 菜单关闭 */
        };
        _contextMenu.LostFocus += (s, e) => _contextMenu.IsOpen = false;

        // 展示主窗口（可勾选）
        var showMainWindowItem = new MenuItem
        {
            Header = "展示主窗口",
            IsCheckable = true,
            Icon = new TextBlock { Text = "", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons") }
        };
        showMainWindowItem.Click += (s, e) =>
        {
            ToggleMainWindow();
            _contextMenu.IsOpen = false;
        };
        _contextMenu.Items.Add(showMainWindowItem);

        // 分隔线
        _contextMenu.Items.Add(new Separator());

        // 重置主窗口位置
        var resetPositionItem = new MenuItem
        {
            Header = "重置主窗口位置",
            Icon = new TextBlock { Text = "", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons") }
        };
        resetPositionItem.Click += (s, e) =>
        {
            ResetNetViewPosition();
            _contextMenu.IsOpen = false;
        };
        _contextMenu.Items.Add(resetPositionItem);

        // 切换监控网络
        var networkSelectionItem = new MenuItem
        {
            Header = "切换监控网络",
            Icon = new TextBlock { Text = "", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons") }
        };
        _contextMenu.Items.Add(networkSelectionItem);

        // 显示详细信息
        var detailedInfoItem = new MenuItem
        {
            Header = "显示详细信息",
            IsCheckable = true,
            Icon = new TextBlock { Text = "", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons") }
        };
        detailedInfoItem.Click += (s, e) =>
        {
            ToggleDetailedInfo();
            _contextMenu.IsOpen = false;
        };
        _contextMenu.Items.Add(detailedInfoItem);

        // 分隔线
        _contextMenu.Items.Add(new Separator());

        // 流量统计与分析
        var trafficAnalysisItem = new MenuItem
        {
            Header = "流量统计与分析",
            Icon = new TextBlock { Text = "", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons") }
        };
        trafficAnalysisItem.Click += (s, e) =>
        {
            OpenTrafficAnalysis();
            _contextMenu.IsOpen = false;
        };
        _contextMenu.Items.Add(trafficAnalysisItem);

        // 设置
        var settingsItem = new MenuItem
        {
            Header = "设置",
            Icon = new TextBlock { Text = "", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons") }
        };
        settingsItem.Click += (s, e) =>
        {
            OpenSettings();
            _contextMenu.IsOpen = false;
        };
        _contextMenu.Items.Add(settingsItem);

        // 分隔线
        _contextMenu.Items.Add(new Separator());

        // 退出程序
        var exitItem = new MenuItem
        {
            Header = "退出程序",
            Icon = new TextBlock
            {
                Text = "", FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"),
                Foreground = new SolidColorBrush(Colors.Red)
            }
        };
        exitItem.Click += (s, e) =>
        {
            ExitApplication();
            _contextMenu.IsOpen = false;
        };
        _contextMenu.Items.Add(exitItem);
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
            ShowContextMenu();
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

    private void ShowContextMenu()
    {
        // 更新菜单状态
        UpdateContextMenuStates();

        // 直接设置菜单位置为鼠标位置，不使用临时窗口
        _contextMenu.PlacementTarget = null;
        _contextMenu.Placement = PlacementMode.MousePoint;
        _contextMenu.StaysOpen = false;

        // 使用 PreviewMouseDown 事件来处理点击外部关闭
        _contextMenu.PreviewMouseDown += OnContextMenuPreviewMouseDown;

        // 当菜单关闭时，清理事件
        RoutedEventHandler closedHandler = null;
        closedHandler = (s, e) =>
        {
            _contextMenu.PreviewMouseDown -= OnContextMenuPreviewMouseDown;
            _contextMenu.Closed -= closedHandler;
        };
        _contextMenu.Closed += closedHandler;

        _contextMenu.IsOpen = true;
    }

    private void OnContextMenuPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // 如果点击的是菜单项，让事件继续处理
        // 这个方法主要是为了确保菜单行为正常
    }

    private void UpdateContextMenuStates()
    {
        if (_mainWindow is Shell.Views.NetView netView)
        {
            // 获取各个菜单项并更新状态
            foreach (var item in _contextMenu.Items.OfType<MenuItem>())
            {
                switch (item.Header.ToString())
                {
                    case "展示主窗口":
                        item.IsChecked = netView.IsVisible;
                        break;
                    case "显示详细信息":
                        item.IsChecked = GetNetViewMenuItemState(netView, "DetailedInfoMenuItem");
                        break;
                }
            }
        }
    }

    private bool GetNetViewMenuItemState(Shell.Views.NetView netView, string menuItemName)
    {
        var contextMenu = netView.ContextMenu;
        if (contextMenu?.Items != null)
        {
            foreach (var item in contextMenu.Items)
            {
                if (item is MenuItem menuItem && menuItem.Name == menuItemName)
                {
                    return menuItem.IsChecked;
                }
            }
        }

        return false;
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

    private void ShowNetMonitor()
    {
        if (_mainWindow != null)
        {
            if (_mainWindow.IsVisible)
            {
                _mainWindow.Hide();
            }
            else
            {
                _mainWindow.Show();
                _mainWindow.Activate();
            }
        }
    }

    private void ResetNetViewPosition()
    {
        if (_mainWindow is Shell.Views.NetView netView)
        {
            var workArea = SystemParameters.WorkArea;
            netView.Left = workArea.Right - netView.Width - 20;
            netView.Top = workArea.Top + 20;
        }
    }

    private void ToggleDetailedInfo()
    {
        if (_mainWindow is Shell.Views.NetView netView)
        {
            var contextMenu = netView.ContextMenu;
            if (contextMenu?.Items != null)
            {
                foreach (var item in contextMenu.Items)
                {
                    if (item is MenuItem menuItem && menuItem.Name == "DetailedInfoMenuItem")
                    {
                        var routedEventArgs = new RoutedEventArgs(MenuItem.ClickEvent);
                        menuItem.RaiseEvent(routedEventArgs);
                        break;
                    }
                }
            }
        }
    }

    private void OpenTrafficAnalysis()
    {
        try
        {
            TrafficAnalysisWindow.ShowWindow();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"无法打开流量分析窗口: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenSettings()
    {
        try
        {
            SettingsWindow.ShowWindow();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"无法打开设置窗口: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ToggleMainWindow()
    {
        if (_mainWindow != null)
        {
            if (_mainWindow.IsVisible)
            {
                _mainWindow.Hide();
            }
            else
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
                _mainWindow.Focus();
            }
        }
    }

    private void ExitApplication()
    {
        var result = System.Windows.MessageBox.Show("确定要退出程序吗？", "确认退出",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            if (_mainWindow is Shell.Views.NetView netView)
            {
                netView.ForceClose();
            }
        }
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
        _contextMenu = null;
        _clickTimer?.Stop();
    }
}