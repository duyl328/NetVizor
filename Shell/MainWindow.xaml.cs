using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Shell;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitWebView();
    }

    private async void InitWebView()
    {
        // 初始化 WebView2
        await webView.EnsureCoreWebView2Async();

        // 加载前端页面：可以是本地文件或服务器地址
        // 示例 1：加载本地 HTML
        // string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "frontend", "index.html");
        // webView.CoreWebView2.Navigate($"file:///{htmlPath.Replace("\\", "/")}");

        // 示例 2：加载远程网页
        webView.CoreWebView2.Navigate("http://localhost:3000");
        
        webView.CoreWebView2.WebMessageReceived += (s, e) =>
        {
            string message = e.TryGetWebMessageAsString();
            MessageBox.Show($"收到消息: {message}");
        };
    }
}
