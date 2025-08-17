using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using Common.Logger;
using Shell.UserControls;
using Shell.Utils;

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

    private async void TrafficAnalysisWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Info("TrafficAnalysisWindow 加载完成");

            // 更新状态

            // 检查WebView2是否可用
            bool hasWebView2 = await WebView2Helper.IsWebView2RuntimeInstalled();

            if (hasWebView2)
            {
                // 创建WebPanel并添加到容器
                var webPanel = new WebPanel();
                WebContainer.Child = webPanel;
            }
            else
            {
                // 显示简化的错误界面
                var errorPanel = CreateErrorPanel();
                WebContainer.Child = errorPanel;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"TrafficAnalysisWindow加载时发生错误: {ex.Message}");

            // 显示错误界面
            var errorPanel = CreateErrorPanel(ex.Message);
            WebContainer.Child = errorPanel;
        }
    }

    private StackPanel CreateErrorPanel(string errorMessage = null)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        };

        panel.Children.Add(new TextBlock
        {
            Text = "⚠️ 流量分析功能不可用",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        });

        if (string.IsNullOrEmpty(errorMessage))
        {
            panel.Children.Add(new TextBlock
            {
                Text = "需要安装 Microsoft Edge WebView2 运行时才能使用此功能。",
                FontSize = 14,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            });

            var downloadButton = new System.Windows.Controls.Button
            {
                Content = "下载 WebView2 运行时",
                Padding = new Thickness(15, 8, 15, 8),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            downloadButton.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Log.Error($"无法打开下载链接: {ex.Message}");
                }
            };
            panel.Children.Add(downloadButton);
        }
        else
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"错误信息: {errorMessage}",
                FontSize = 12,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.Red,
                TextWrapping = TextWrapping.Wrap
            });
        }

        return panel;
    }

    private void TrafficAnalysisWindow_Closing(object sender, CancelEventArgs e)
    {
        // Don't actually close, just hide the window for better performance
        e.Cancel = true;
        this.Hide();
        Log.Info("TrafficAnalysisWindow 隐藏");
    }

    // Force close method for application shutdown
    public void ForceClose()
    {
        this.Closing -= TrafficAnalysisWindow_Closing; // Remove the cancel logic
        this.Close();
        _instance = null;
    }
}