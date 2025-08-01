using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Shell.Utils;

namespace Shell.Views;

public partial class NetView : Window
{
    private TrayIconManager _trayIconManager;
    private bool _isClickThrough = false;
    private bool _isPositionLocked = false;
    private bool _snapToScreen = false;
    private bool _showDetailedInfo = false;

    // Double-click detection
    private DispatcherTimer _doubleClickTimer;
    private bool _isDoubleClick = false;
    private const int DoubleClickTimeMs = 500; // 500ms for double-click detection

    // Win32 API for click-through functionality
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // Win32 API to disable window snapping
    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int SPI_SETDRAGFULLWINDOWS = 0x0025;

    public NetView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        InitializeTrayIcon();
        this.Loaded += NetView_Loaded;

        // Prevent window snapping by setting ResizeMode appropriately
        this.ResizeMode = ResizeMode.NoResize;

        // Initialize double-click timer
        InitializeDoubleClickTimer();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 重置位置
        ResetToDefaultPosition();
    }

    private void NetView_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize default position if needed
        if (this.Left == 0 && this.Top == 0)
        {
            ResetToDefaultPosition();
        }

        // 设置窗口为工具窗口，隐藏在Alt+Tab中
        HideFromAltTab();

        // Disable automatic window arrangement features
        DisableWindowSnapping();

        // Populate network interfaces in context menu
        PopulateNetworkMenu();
    }

    private void PopulateNetworkMenu()
    {
        if (NetworkSelectionMenuItem != null)
        {
            // Get the NetworkMonitorView and its ViewModel
            var networkMonitorView = FindChild<UserControls.NetworkMonitorView>(this);
            if (networkMonitorView?.DataContext is Shell.ViewModel.NetworkMonitorViewModel viewModel)
            {
                // Clear existing items
                NetworkSelectionMenuItem.Items.Clear();

                // Add network interfaces from ViewModel
                foreach (var netInterface in viewModel.NetworkInterfaces)
                {
                    var menuItem = new System.Windows.Controls.MenuItem();

                    // Create header with status indicator
                    var headerText = netInterface.Name;
                    if (netInterface.IsConnected)
                    {
                        headerText += " [已连接]";
                    }
                    else
                    {
                        headerText += " [未连接]";
                    }

                    menuItem.Header = headerText;
                    menuItem.IsCheckable = true;
                    menuItem.IsChecked = netInterface == viewModel.SelectedInterface;
                    menuItem.Tag = netInterface;

                    // Set different styles for connected/disconnected
                    if (netInterface.IsConnected)
                    {
                        menuItem.Foreground =
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                    }
                    else
                    {
                        menuItem.Foreground =
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                    }

                    menuItem.Click += (s, e) =>
                    {
                        if (s is System.Windows.Controls.MenuItem clickedItem &&
                            clickedItem.Tag is Shell.ViewModel.NetworkInterfaceItem selectedInterface)
                        {
                            // Update selection in ViewModel
                            viewModel.SelectedInterface = selectedInterface;

                            // Update check states
                            foreach (System.Windows.Controls.MenuItem item in NetworkSelectionMenuItem.Items)
                            {
                                item.IsChecked = item == clickedItem;
                            }
                        }
                    };

                    NetworkSelectionMenuItem.Items.Add(menuItem);
                }
            }
        }
    }

    private T FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;

        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;

            var childResult = FindChild<T>(child);
            if (childResult != null)
                return childResult;
        }

        return null;
    }

    private void DisableWindowSnapping()
    {
        // Additional protection against window snapping
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            // Set window style to prevent automatic sizing
            var style = GetWindowLong(hwnd, -16); // GWL_STYLE
            // Remove maximize and minimize capabilities that can trigger snapping
            SetWindowLong(hwnd, -16, style & ~0x10000 & ~0x20000); // Remove WS_MAXIMIZEBOX and WS_MINIMIZEBOX
        }
    }

    private void HideFromAltTab()
    {
        // 设置窗口为工具窗口，这样它就不会出现在Alt+Tab中
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);
        }
    }

    private void InitializeTrayIcon()
    {
        _trayIconManager = new TrayIconManager(this);
    }

    private void InitializeDoubleClickTimer()
    {
        _doubleClickTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(DoubleClickTimeMs)
        };
        _doubleClickTimer.Tick += DoubleClickTimer_Tick;
    }

    private void DoubleClickTimer_Tick(object sender, EventArgs e)
    {
        _doubleClickTimer.Stop();
        if (!_isDoubleClick)
        {
            // Single click - no action needed for now
        }

        _isDoubleClick = false;
    }

    private void HandleDoubleClick()
    {
        try
        {
            var settings = Shell.Models.NetViewSettings.Instance;
            switch (settings.DoubleClickAction)
            {
                case Shell.Models.DoubleClickAction.TrafficAnalysis:
                    TrafficAnalysisWindow.ShowWindow();
                    break;
                case Shell.Models.DoubleClickAction.Settings:
                    SettingsWindow.ShowWindow();
                    break;
                case Shell.Models.DoubleClickAction.None:
                default:
                    // 无动作
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"执行双击动作失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // 阻止窗口关闭，而是隐藏到托盘
        e.Cancel = true;
        this.Hide();
    }

    protected override void OnClosed(EventArgs e)
    {
        _trayIconManager?.Dispose();
        base.OnClosed(e);
    }

    // 如果你想要真正关闭程序的方法
    public void ForceClose()
    {
        // Close TrafficAnalysisWindow if it exists
        try
        {
            if (TrafficAnalysisWindow.Instance != null)
            {
                TrafficAnalysisWindow.Instance.ForceClose();
            }
        }
        catch (Exception ex)
        {
            // Log but don't prevent shutdown
            System.Diagnostics.Debug.WriteLine($"Error closing TrafficAnalysisWindow: {ex.Message}");
        }

        // Close SettingsWindow if it exists
        try
        {
            if (SettingsWindow.Instance != null)
            {
                SettingsWindow.Instance.ForceClose();
            }
        }
        catch (Exception ex)
        {
            // Log but don't prevent shutdown
            System.Diagnostics.Debug.WriteLine($"Error closing SettingsWindow: {ex.Message}");
        }

        _trayIconManager?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    // Right-click menu event handlers
    private void ResetPosition_Click(object sender, RoutedEventArgs e)
    {
        ResetToDefaultPosition();
    }

    private void ResetToDefaultPosition()
    {
        var workArea = SystemParameters.WorkArea;
        this.Left = workArea.Right - this.Width - 50;
        this.Top = workArea.Top + 20;
    }

    private void LockPosition_Click(object sender, RoutedEventArgs e)
    {
        _isPositionLocked = !_isPositionLocked;
        LockPositionMenuItem.IsChecked = _isPositionLocked;

        // Don't change ResizeMode to maintain consistent window behavior
        // The dragging logic will check _isPositionLocked instead
    }

    private void ToggleClickThrough_Click(object sender, RoutedEventArgs e)
    {
        _isClickThrough = !_isClickThrough;
        ClickThroughMenuItem.IsChecked = _isClickThrough;
        SetClickThrough(_isClickThrough);
    }

    private void SetClickThrough(bool enabled)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (enabled)
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            }
            else
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
            }
        }
    }

    private void ToggleTopmost_Click(object sender, RoutedEventArgs e)
    {
        this.Topmost = !this.Topmost;
        TopmostMenuItem.IsChecked = this.Topmost;
    }

    private void ToggleSnapToScreen_Click(object sender, RoutedEventArgs e)
    {
        _snapToScreen = !_snapToScreen;
        SnapToScreenMenuItem.IsChecked = _snapToScreen;

        if (_snapToScreen)
        {
            SnapToScreen();
        }
    }

    private void SnapToScreen()
    {
        var workArea = SystemParameters.WorkArea;
        var left = Math.Max(workArea.Left, Math.Min(this.Left, workArea.Right - this.Width));
        var top = Math.Max(workArea.Top, Math.Min(this.Top, workArea.Bottom - this.Height));

        this.Left = left;
        this.Top = top;
    }

    private void ToggleDetailedInfo_Click(object sender, RoutedEventArgs e)
    {
        _showDetailedInfo = !_showDetailedInfo;
        DetailedInfoMenuItem.IsChecked = _showDetailedInfo;

        // The window will automatically resize based on content
        // No need to manually set height anymore due to SizeToContent="WidthAndHeight"

        // You can trigger a re-layout if needed
        this.InvalidateMeasure();
        this.UpdateLayout();
    }

    private void HideToTray_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }

    private void ExitApplication_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show("确定要退出程序吗？", "确认退出",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            ForceClose();
        }
    }

    private void OpenTrafficAnalysis_Click(object sender, RoutedEventArgs e)
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

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
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


    // Handle window dragging and double-click detection
    private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Handle double-click detection
        if (_doubleClickTimer.IsEnabled)
        {
            // This is a second click within the timeout period
            _doubleClickTimer.Stop();
            _isDoubleClick = true;
            HandleDoubleClick();
            return;
        }
        else
        {
            // Start timer for double-click detection
            _isDoubleClick = false;
            _doubleClickTimer.Start();
        }

        // Only allow dragging if position is not locked and click-through is disabled
        if (!_isPositionLocked && !_isClickThrough)
        {
            try
            {
                this.DragMove();
            }
            catch (InvalidOperationException)
            {
                // DragMove can throw exception if called when mouse is not pressed
                // This can happen in edge cases, so we catch and ignore
            }
        }
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);

        // Only check snap to screen if that option is enabled
        if (_snapToScreen && !_isPositionLocked)
        {
            SnapToScreen();
        }

        // Perform boundary check after drag is complete (with delay to avoid flicker)
        if (!_isPositionLocked)
        {
            CheckBoundariesDelayed();
        }
    }

    private System.Windows.Threading.DispatcherTimer? _boundaryCheckTimer;

    private void CheckBoundariesDelayed()
    {
        // Cancel previous timer if exists
        _boundaryCheckTimer?.Stop();

        // Create new timer for delayed boundary check
        _boundaryCheckTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150) // Delay to avoid flicker during drag
        };

        _boundaryCheckTimer.Tick += (s, e) =>
        {
            _boundaryCheckTimer.Stop();
            SnapBackToScreen();
        };

        _boundaryCheckTimer.Start();
    }

    private void SnapBackToScreen()
    {
        var workArea = SystemParameters.WorkArea;
        var originalLeft = this.Left;
        var originalTop = this.Top;

        // Calculate bounds with some tolerance (allow partial off-screen)
        var tolerance = 20; // Allow 20 pixels to be off-screen
        var minLeft = workArea.Left - (this.Width - tolerance);
        var maxLeft = workArea.Right - tolerance;
        var minTop = workArea.Top - (this.Height - tolerance);
        var maxTop = workArea.Bottom - tolerance;

        // Adjust position if outside bounds
        var newLeft = Math.Max(minLeft, Math.Min(this.Left, maxLeft));
        var newTop = Math.Max(minTop, Math.Min(this.Top, maxTop));

        // Only animate if position actually changed
        if (Math.Abs(newLeft - originalLeft) > 1 || Math.Abs(newTop - originalTop) > 1)
        {
            AnimateToPosition(newLeft, newTop);
        }
    }

    private void AnimateToPosition(double targetLeft, double targetTop)
    {
        var duration = TimeSpan.FromMilliseconds(200);

        var leftAnimation = new System.Windows.Media.Animation.DoubleAnimation(
            this.Left, targetLeft, duration)
        {
            EasingFunction = new System.Windows.Media.Animation.QuadraticEase()
        };

        var topAnimation = new System.Windows.Media.Animation.DoubleAnimation(
            this.Top, targetTop, duration)
        {
            EasingFunction = new System.Windows.Media.Animation.QuadraticEase()
        };

        this.BeginAnimation(Window.LeftProperty, leftAnimation);
        this.BeginAnimation(Window.TopProperty, topAnimation);
    }
}