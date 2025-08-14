using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Shell.Views;
using Common.Logger;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace Shell.Utils;

public class TrayIconManager : IDisposable
{
    private NotifyIcon _notifyIcon;
    private Window _mainWindow;
    private ContextMenu _contextMenu;
    private DispatcherTimer _clickTimer;
    private bool _isDoubleClick = false;
    private DispatcherTimer _menuCloseTimer;
    private bool _lastLeftButtonState = false;
    private bool _lastRightButtonState = false;

    // Win32 API 用于获取鼠标位置和显示器信息
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    // 添加获取鼠标按键状态的API
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;

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
        _contextMenu.Closed += (s, e) => { };
        _contextMenu.LostFocus += (s, e) =>
        {
            // 暂时禁用自动关闭来调试问题
            // if (_contextMenu != null)
            //     _contextMenu.IsOpen = false;
        };

        // 添加更多事件监听来调试
        _contextMenu.Opened += (s, e) => { };

        _contextMenu.MouseEnter += (s, e) => { };

        _contextMenu.MouseLeave += (s, e) => { };

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
            if (_contextMenu != null)
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
            if (_contextMenu != null)
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
            if (_contextMenu != null)
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
            if (_contextMenu != null)
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
            if (_contextMenu != null)
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
            if (_contextMenu != null)
            {
                _contextMenu.IsOpen = false;
            }
        };
        _contextMenu.Items.Add(exitItem);
    }

    private Icon LoadIcon()
    {
        try
        {
            // 尝试多种路径来加载图标
            string[] possiblePaths = {
                "Assets/Icons/app.ico",
                "./Assets/Icons/app.ico",
                "Shell/Assets/Icons/app.ico",
                "./Shell/Assets/Icons/app.ico",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "app.ico"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Shell", "Assets", "Icons", "app.ico")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    Log.Info($"成功加载托盘图标: {path}");
                    return new Icon(path);
                }
            }

            // 如果找不到图标文件，记录警告并使用系统默认图标
            Log.Warning("未找到app.ico文件，使用系统默认图标");
            return SystemIcons.Application;
        }
        catch (Exception ex)
        {
            Log.Warning($"加载托盘图标失败: {ex.Message}，使用系统默认图标");
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

        // 简单设置菜单位置
        _contextMenu.PlacementTarget = null;
        _contextMenu.Placement = PlacementMode.MousePoint;
        _contextMenu.StaysOpen = true; // 保持打开，我们手动控制关闭

        // 禁用默认的关闭行为
        _contextMenu.IsOpen = true;

        // 强制设置，确保菜单不会因为失去焦点而关闭
        _contextMenu.UpdateLayout();


        // 启动定时器来检测点击外部
        StartMenuCloseTimer();
    }

    private void StartMenuCloseTimer()
    {
        if (_menuCloseTimer != null)
        {
            _menuCloseTimer.Stop();
        }

        _menuCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50) // 改为每50ms检查一次，提高响应速度
        };


        _menuCloseTimer.Tick += (s, e) =>
        {
            if (!_contextMenu.IsOpen)
            {
                _menuCloseTimer.Stop();
                return;
            }

            // 检查ESC键是否被按下
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Escape))
            {
                if (_contextMenu != null)
                    _contextMenu.IsOpen = false;
                _menuCloseTimer?.Stop();
                return;
            }

            // 使用Win32 API检测鼠标按键状态变化
            bool currentLeftState = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
            bool currentRightState = (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;

            // 检测按键从未按下到按下的状态变化（按下事件）
            bool leftButtonPressed = currentLeftState && !_lastLeftButtonState;
            bool rightButtonPressed = currentRightState && !_lastRightButtonState;

            _lastLeftButtonState = currentLeftState;
            _lastRightButtonState = currentRightState;

            if (leftButtonPressed || rightButtonPressed)
            {
                // 如果鼠标不在菜单区域内且有按键按下，则关闭菜单
                bool isMouseOver = IsMouseOverContextMenu();

                if (!isMouseOver)
                {
                    if (_contextMenu != null)
                        _contextMenu.IsOpen = false;
                    _menuCloseTimer?.Stop();
                }
            }
        };

        _menuCloseTimer.Start();

        // 重置按键状态，避免误检测
        _lastLeftButtonState = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
        _lastRightButtonState = (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;
    }

    private bool IsMouseOverContextMenu()
    {
        try
        {
            if (_contextMenu == null || !_contextMenu.IsOpen)
            {
                return false;
            }

            // 获取屏幕上的鼠标位置
            GetCursorPos(out POINT screenPoint);
            var screenPosition = new System.Windows.Point(screenPoint.X, screenPoint.Y);

            // 获取菜单的屏幕位置和大小
            var contextMenuElement = _contextMenu as FrameworkElement;
            if (contextMenuElement == null)
            {
                return true; // 改为返回true，避免意外关闭
            }


            var source = PresentationSource.FromVisual(contextMenuElement);
            if (source == null)
            {
                return true; // 改为返回true，避免意外关闭
            }

            // 获取菜单的屏幕边界
            var menuTopLeft = contextMenuElement.PointToScreen(new System.Windows.Point(0, 0));
            var menuBottomRight =
                contextMenuElement.PointToScreen(new System.Windows.Point(contextMenuElement.ActualWidth,
                    contextMenuElement.ActualHeight));


            // 添加一些边距，让鼠标检测区域稍微大一点，避免意外关闭
            var margin = 5;
            var menuBounds = new Rect(
                menuTopLeft.X - margin,
                menuTopLeft.Y - margin,
                menuBottomRight.X - menuTopLeft.X + (margin * 2),
                menuBottomRight.Y - menuTopLeft.Y + (margin * 2)
            );


            bool contains = menuBounds.Contains(screenPosition);

            return contains;
        }
        catch (Exception ex)
        {
            // 如果检测失败，假设鼠标在菜单上，避免意外关闭
            return true;
        }
    }

    private void UpdateContextMenuStates()
    {
        if (_mainWindow != null && _mainWindow is Shell.Views.NetView netView)
        {
            // 获取各个菜单项并更新状态
            foreach (var item in _contextMenu.Items.OfType<MenuItem>())
            {
                switch (item.Header?.ToString())
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
        if (_mainWindow != null && _mainWindow is Shell.Views.NetView netView)
        {
            var workArea = SystemParameters.WorkArea;
            netView.Left = workArea.Right - netView.Width - 20;
            netView.Top = workArea.Top + 20;
        }
    }

    private void ToggleDetailedInfo()
    {
        if (_mainWindow != null && _mainWindow is Shell.Views.NetView netView)
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
            else if (_mainWindow != null)
            {
                // 如果不是NetView类型，直接关闭应用程序
                System.Windows.Application.Current?.Shutdown();
            }
            else
            {
                // _mainWindow为null时，直接关闭应用程序
                System.Windows.Application.Current?.Shutdown();
            }
        }
    }

    public void Dispose()
    {
        try
        {
            _clickTimer?.Stop();
            _menuCloseTimer?.Stop();

            if (_contextMenu != null)
            {
                _contextMenu.IsOpen = false;
                _contextMenu = null;
            }

            _notifyIcon?.Dispose();
            _mainWindow = null;
        }
        catch (Exception ex)
        {
            // 记录异常但不抛出，避免在Dispose中出现异常
            System.Diagnostics.Debug.WriteLine($"Error in TrayIconManager.Dispose: {ex.Message}");
        }
    }
}