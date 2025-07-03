using System.Windows.Controls;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Application.Utils;
using Common;
using Common.Logger;
using Common.Net;
using Common.Net.WebSocketConn;
using Common.Utils;
using Infrastructure.utils;
using Utils.ETW;
using Utils.ETW.Core;
using Utils.ETW.Etw;

namespace Shell.Views;

public partial class WebPanel : UserControl
{
    public WebPanel()
    {
        InitializeComponent();

        // Class1.Main2();

        InitWebView();

        // 注册管理 WebSocket 事件
        // WebSocketManger();

        // ETW 信息获取
        // 检查管理员权限
        NewMethod();
    }

    private static async Task NewMethod()
    {
        if (!SysHelper.IsAdministrator())
        {
            Console.WriteLine("此程序需要管理员权限才能运行ETW监控！");
            Console.WriteLine("请以管理员身份重新运行程序。");
            Console.ReadKey();
            return;
        }

        var example = new NetworkMonitorUsageExample();
        example.RunExample();

        // 创建监听实例
        // var etwNetworkManger = new EtwNetworkManger();
        // // 设置 ETW 监听
        // etwNetworkManger.SetupEtwHandlers();
        // etwNetworkManger.StartCapture();
    }

    private void WebSocketManger()
    {
        var webSocketManager = WebSocketManager.Instance;

        // 获取网络状态
        webSocketManager.RegisterHandler("etwNetworkManger", async (cmd, socket) =>
        {
            Console.WriteLine("开始测试");

            // 创建监听实例
            // var etwNetworkManger = new EtwNetworkManger();

            // 设置 ETW 监听
            // etwNetworkManger.Initialize();
        });
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
            var msg = JsonHelper.FromJson<JsMessage>(json);

            Log.Information(json);
            Log.Information(msg?.Channel);

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
                    Log.Information(programDiagnostics);
                    break;
                case "InspectProcess":
                    SysInfoUtils.InspectProcess(23192);
                    break;
                case "GetAllTcpConnections":
                    NetUtils.GetAllTcpConnections();
                    break;
                // case "PacketSnifferStart":
                //     PacketSniffer.Start();
                //     break;
                case "GetWebSocketPath":
                    WebSocketPath();
                    break;
                case "CloseWebSocket":
                    CloseWebSocket();
                    break;
                case "OpenWebSocket":
                    OpenWebSocket();
                    break;
                default:
                    Log.Information("没有任何函数被触发...");
                    break;
            }
        };
    }

    private void OpenWebSocket()
    {
        int port = SysHelper.GetAvailablePort();
        AppConfig.Instance.WebSocketPort = port;
        AppConfig.Instance.WebSocketPath = $"ws://127.0.0.1:{port}";
        WebSocketManager.Instance.Start(AppConfig.Instance.WebSocketPath);
    }

    private void CloseWebSocket()
    {
        WebSocketManager.Instance.Stop();
    }

    private async void InitWebView1()
    {
        await webView.ExecuteScriptAsync("""
                                           window.externalFunctions.__BRIDGE_LISTEN__('showMessage','来自 C# 的问候');
                                         """);
    }

    private async void WebSocketPath()
    {
        var instanceWebSocketPath = AppConfig.Instance.WebSocketPath;
        Log.Information($"instanceWebSocketPath -> :{instanceWebSocketPath}");
        string result = await webView.ExecuteScriptAsync($"""
                                                            window.externalFunctions.__BRIDGE_LISTEN__('ListenWebSocketPath','{instanceWebSocketPath}');
                                                          """);
        string result1 = await webView.ExecuteScriptAsync($"""
                                                             window.externalFunctions.__BRIDGE_LISTEN__('GetWebSocketPath','{instanceWebSocketPath}');
                                                           """);
    }
}

public class JsMessage
{
public string Channel { get; set; }
public JsData Payload { get; set; }

    public override string ToString()
    {
        return $"Channel:{Channel}, Payload:{Payload}";
    }
}

public class JsData
{
public string Title { get; set; }
public string Content { get; set; }

    public override string ToString()
    {
        return $"Title:{Title}, Content:{Content}";
    }
}