using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;
using Common.Logger;

namespace Shell.Views;

public partial class TrafficAnalysisWindow : Window
{
    private static TrafficAnalysisWindow? _instance;
    private static bool _hasRequestedElevation = false;

    // Win32 API for checking admin rights
    [DllImport("shell32.dll")]
    static extern bool IsUserAnAdmin();

    public static TrafficAnalysisWindow Instance
    {
        get
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new TrafficAnalysisWindow();
            }

            return _instance;
        }
    }

    private TrafficAnalysisWindow()
    {
        InitializeComponent();
        this.Loaded += TrafficAnalysisWindow_Loaded;
        this.Closing += TrafficAnalysisWindow_Closing;

        Log.Info("TrafficAnalysisWindow 初始化");
    }

    public static void ShowWindow()
    {
        try
        {
            var window = Instance;

            // Check if we need admin rights and haven't requested yet
            if (!IsRunningAsAdmin() && !_hasRequestedElevation)
            {
                var result = System.Windows.MessageBox.Show(
                    "流量分析功能需要管理员权限以获取完整的网络统计信息。\n\n是否要以管理员身份重新启动应用程序？",
                    "需要管理员权限",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    RequestElevation();
                    return;
                }
                else
                {
                    _hasRequestedElevation = true; // User declined, don't ask again
                }
            }

            window.Show();
            window.Activate();
            window.Focus();
        }
        catch (Exception ex)
        {
            Log.Error4Ctx($"打开流量分析窗口时发生错误: {ex.Message}");
            System.Windows.MessageBox.Show($"无法打开流量分析窗口: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static bool IsRunningAsAdmin()
    {
        try
        {
            return IsUserAnAdmin();
        }
        catch
        {
            // Fallback method
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    private static void RequestElevation()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath,
                UseShellExecute = true,
                Verb = "runas", // Request elevation
                Arguments = "--traffic-analysis" // Flag to indicate this was started for traffic analysis
            };

            Process.Start(processInfo);

            // Close current application
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            Log.Warning($"请求管理员权限失败: {ex.Message}");
            System.Windows.MessageBox.Show("无法请求管理员权限。请手动以管理员身份运行程序。", "权限请求失败",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            _hasRequestedElevation = true; // Don't ask again in this session
        }
    }

    private void TrafficAnalysisWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Log.Info("TrafficAnalysisWindow 加载完成");

        // Update status based on admin rights
        UpdateStatus();

        // Initialize WebPanel if needed
        InitializeWebPanel();
    }

    private void UpdateStatus()
    {
        if (IsRunningAsAdmin())
        {
            StatusText.Text = "管理员模式 - 完整功能可用";
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
        }
        else
        {
            StatusText.Text = "普通模式 - 功能受限";
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
        }
    }

    private void InitializeWebPanel()
    {
        try
        {
            // WebPanel should automatically load the Vue3 application
            // The WebView2 control will handle the web content
            Log.Info("WebPanel 初始化完成");
        }
        catch (Exception ex)
        {
            Log.Error4Ctx($"WebPanel 初始化失败: {ex.Message}");
            StatusText.Text = "初始化失败";
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
        }
    }

    private void TrafficAnalysisWindow_Closing(object sender, CancelEventArgs e)
    {
        // Don't actually close, just hide the window for better performance
        e.Cancel = true;
        this.Hide();
        Log.Info("TrafficAnalysisWindow 隐藏");
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Refresh the WebPanel content
            if (WebPanel != null)
            {
                // The WebPanel should handle its own refresh logic
                Log.Info("刷新流量分析数据");
                StatusText.Text = "正在刷新...";

                // Use a timer to reset status after refresh
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (s, args) =>
                {
                    UpdateStatus();
                    timer.Stop();
                };
                timer.Start();
            }
        }
        catch (Exception ex)
        {
            Log.Error4Ctx($"刷新失败: {ex.Message}");
            System.Windows.MessageBox.Show($"刷新失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }

    // Force close method for application shutdown
    public void ForceClose()
    {
        this.Closing -= TrafficAnalysisWindow_Closing; // Remove the cancel logic
        this.Close();
        _instance = null;
    }
}
