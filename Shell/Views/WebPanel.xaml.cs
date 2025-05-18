using System.Windows.Controls;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Infrastructure.utils;
using WinDivertNet.WinDivertWrapper;

namespace Shell.Views;

public partial class WebPanel : UserControl
{
    public WebPanel()
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

        webView.CoreWebView2.WebMessageReceived += (sender, args) =>
        {
            string json = args.WebMessageAsJson;

            // 反序列化为 C# 对象
            var msg = JsonSerializer.Deserialize<JsMessage>(json);

            Console.WriteLine(json);
            Console.WriteLine(msg?.Channel);

            switch (msg?.Channel)
            {
                case "showMessage":
                    MessageBox.Show(msg.Payload?.Content ?? "无内容", msg.Payload?.Title ?? "提示");
                    InitWebView1();
                    break;
                case "GetNetInfo":
                    NetUtils.GetNetInfo();
                    break;
                case "GetProgramDiagnostics":
                    var programDiagnostics = SysInfoUtils.GetProgramDiagnostics();
                    Console.WriteLine(programDiagnostics);
                    break;
                case "InspectProcess":
                    SysInfoUtils.InspectProcess(23192);
                    break;
                case "GetAllTcpConnections":
                    NetUtils.GetAllTcpConnections();
                    break;
                case "PacketSnifferStart":
                    PacketSniffer.Start();
                    break;
                default:
                    Console.WriteLine("没有任何函数被触发...");
                    break;
            }
        };
    }

    private async void InitWebView1()
    {
        await webView.ExecuteScriptAsync("""
                                           window.externalFunctions.__BRIDGE_LISTEN__('showMessage','来自 C# 的问候');
                                         """);
    }
}

public class JsMessage
{
    [JsonPropertyName("channel")] public string Channel { get; set; }
    [JsonPropertyName("payload")] public JsData Payload { get; set; }

    public override string ToString()
    {
        return $"Channel:{Channel}, Payload:{Payload}";
    }
}

public class JsData
{
    [JsonPropertyName("title")] public string Title { get; set; }
    [JsonPropertyName("content")] public string Content { get; set; }

    public override string ToString()
    {
        return $"Title:{Title}, Content:{Content}";
    }
}
