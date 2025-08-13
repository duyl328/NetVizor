using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using Application;
using Common;
using Common.Logger;
using Common.Net.HttpConn;
using Common.Net.Models;
using Common.Net.WebSocketConn;
using Common.Utils;
using Data;
using Data.Models;
using Data.Core;
using Data.Repositories;
using Shell.Views;
using Utils.ETW.Etw;
using Utils.Firewall;
using Dapper;
using Shell.Services;
using Shell.Utils;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Shell;
// 引用 Windows Forms 命名空间

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private ServerStartupManager? _serverManager;
    private GlobalExceptionHandler? _exceptionHandler;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // 初始化异常处理
        _exceptionHandler = GlobalExceptionHandler.Instance;
        _exceptionHandler.Initialize();

        // 初始化日志
        var configModelLogging = AppConfig.Instance.ConfigModel.Logging;
        Log.Initialize(configModelLogging);
        base.OnStartup(e);

        // 初始化数据库和数据收集服务
        try
        {
            Log.Information("正在初始化数据库和网络监控服务...");
            await DatabaseManager.InitializeAsync();
            Log.Information("数据库和网络监控服务初始化完成");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据库初始化失败");
            MessageBox.Show($"数据库初始化失败: {ex.Message}", "错误", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
            return;
        }

        // 启动所有服务
        try
        {
            _serverManager = new ServerStartupManager();
            await _serverManager.StartAllServicesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "服务启动失败");
            MessageBox.Show($"服务启动失败: {ex.Message}", "错误", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }

        // 创建 NetView 窗口
        try
        {
            Log.Information("创建 NetView 窗口...");
            var netView = new NetView();
            netView.Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "创建 NetView 窗口失败");
            throw;
        }

        // 设置应用程序在关闭最后一个窗口时不自动退出
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 检测 WebView2
        bool hasRuntime = await WebView2Helper.IsWebView2RuntimeInstalled();
        if (!hasRuntime)
        {
            // 非阻塞提示
            MessageBox.Show(
                "检测到您未安装 WebView2 运行时，统计页面等功能将不可用。\n\n点击“确定”访问下载页面。"
            );

            // 跳转官方下载
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                UseShellExecute = true
            });
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serverManager?.Stop();
        base.OnExit(e);
    }

    // 辅助方法：获取所有网卡的小时数据
    private static async Task<List<GlobalNetworkAggregatedBase>> GetAllNetworkCardsHourlyDataAsync(int hours)
    {
        var result = new List<GlobalNetworkAggregatedBase>();
        var startTime = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeSeconds();

        // 使用现有的方法来获取所有网卡的小时数据
        // 先获取所有存在的网卡GUID
        try
        {
            var allHourlyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync("", hours);
            var networkGuids = allHourlyData.Select(x => "").Distinct(); // 这里需要从实际数据中提取

            foreach (var guid in networkGuids)
            {
                if (!string.IsNullOrEmpty(guid))
                    result.Add(new GlobalNetworkHourly { NetworkCardGuid = guid });
            }
        }
        catch
        {
            // 如果出错，返回空列表
        }

        return result;
    }

    // 辅助方法：获取所有网卡的日数据  
    private static async Task<List<GlobalNetworkAggregatedBase>> GetAllNetworkCardsDailyDataAsync(int days)
    {
        var result = new List<GlobalNetworkAggregatedBase>();

        try
        {
            var allDailyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync("", days);
            var networkGuids = allDailyData.Select(x => "").Distinct(); // 这里需要从实际数据中提取

            foreach (var guid in networkGuids)
            {
                if (!string.IsNullOrEmpty(guid))
                    result.Add(new GlobalNetworkDaily { NetworkCardGuid = guid });
            }
        }
        catch
        {
            // 如果出错，返回空列表
        }

        return result;
    }

    // 辅助方法：检查指定时间范围内是否有网络数据
    private static async Task<bool> HasNetworkDataInTimeRangeAsync(string dataType, long startTime)
    {
        try
        {
            // 使用现有方法检查数据
            switch (dataType)
            {
                case "hourly":
                    var hourlyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync("", 1);
                    return hourlyData.Any();
                case "daily":
                    var dailyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync("", 1);
                    return dailyData.Any();
                default:
                    var networkData = await DatabaseManager.Instance.Networks.GetGlobalNetworkHistoryAsync("", 1);
                    return networkData.Any();
            }
        }
        catch
        {
            return false;
        }
    }

    // 辅助方法：获取流量趋势数据
    private static async Task<List<object>> GetTrafficTrendsAsync(string timeRange, string interfaceId, long startTime,
        long endTime)
    {
        var points = new List<object>();

        try
        {
            if (timeRange == "hour")
            {
                // 从 GlobalNetwork 表查询
                if (interfaceId == "all")
                {
                    // 获取所有网卡数据并聚合
                    var allData =
                        await DatabaseManager.Instance.Networks
                            .GetGlobalNetworkByTimeRangeAsync("", startTime, endTime);
                    var groupedData = allData.GroupBy(d => d.Timestep).Select(g => new
                    {
                        timestamp = g.Key.ToString(),
                        uploadSpeed = g.Sum(x => x.Upload),
                        downloadSpeed = g.Sum(x => x.Download)
                    });

                    points.AddRange(groupedData);
                }
                else
                {
                    var data = await DatabaseManager.Instance.Networks.GetGlobalNetworkByTimeRangeAsync(interfaceId,
                        startTime, endTime);
                    points.AddRange(data.Select(d => new
                    {
                        timestamp = d.Timestep.ToString(),
                        uploadSpeed = d.Upload,
                        downloadSpeed = d.Download
                    }));
                }
            }
            else if (timeRange == "day")
            {
                // 从小时数据聚合
                if (interfaceId == "all")
                {
                    var allData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync("", 24);
                    var groupedData = allData.GroupBy(d => d.HourTimestamp).Select(g => new
                    {
                        timestamp = g.Key.ToString(),
                        uploadSpeed = g.Sum(x => x.AvgUpload),
                        downloadSpeed = g.Sum(x => x.AvgDownload)
                    });

                    points.AddRange(groupedData);
                }
                else
                {
                    var data = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync(interfaceId, 24);
                    points.AddRange(data.Select(d => new
                    {
                        timestamp = d.HourTimestamp.ToString(),
                        uploadSpeed = d.AvgUpload,
                        downloadSpeed = d.AvgDownload
                    }));
                }
            }
            else
            {
                // 从日数据聚合
                int days = timeRange == "week" ? 7 : 30;
                if (interfaceId == "all")
                {
                    var allData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync("", days);
                    var groupedData = allData.GroupBy(d => d.DayTimestamp).Select(g => new
                    {
                        timestamp = g.Key.ToString(),
                        uploadSpeed = g.Sum(x => x.AvgUpload),
                        downloadSpeed = g.Sum(x => x.AvgDownload)
                    });

                    points.AddRange(groupedData);
                }
                else
                {
                    var data = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync(interfaceId, days);
                    points.AddRange(data.Select(d => new
                    {
                        timestamp = d.DayTimestamp.ToString(),
                        uploadSpeed = d.AvgUpload,
                        downloadSpeed = d.AvgDownload
                    }));
                }
            }
        }
        catch
        {
            // 如果出错，返回空数据
        }

        return points;
    }

    /// <summary>
    /// 根据端口号获取服务名称
    /// </summary>
    /// <param name="port">端口号</param>
    /// <returns>服务名称</returns>
    private static string GetPortServiceName(int port)
    {
        return port switch
        {
            21 => "FTP",
            22 => "SSH",
            23 => "Telnet",
            25 => "SMTP",
            53 => "DNS",
            80 => "HTTP",
            110 => "POP3",
            123 => "NTP",
            143 => "IMAP",
            443 => "HTTPS",
            993 => "IMAPS",
            995 => "POP3S",
            1433 => "SQL Server",
            3306 => "MySQL",
            3389 => "RDP",
            5432 => "PostgreSQL",
            6379 => "Redis",
            8080 => "HTTP-Alt",
            8443 => "HTTPS-Alt",
            _ when port >= 1024 && port <= 5000 => "User Port",
            _ when port > 5000 && port < 32768 => "Service Port",
            _ when port >= 32768 => "Dynamic Port",
            _ => "Unknown"
        };
    }

// 判断是否为静态资源
    private bool IsStaticResource(string path)
    {
        string[] staticExtensions =
        {
            ".js", ".css", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot",
            ".map"
        };
        string extension = Path.GetExtension(path).ToLower();
        return staticExtensions.Contains(extension);
    }

// 获取正确的 MIME 类型
    private string GetContentType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".mjs" => "application/javascript", // ES6 模块
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".eot" => "application/vnd.ms-fontobject",
            ".map" => "application/json", // source map 文件
            _ => "application/octet-stream"
        };
    }
}

/// <summary>
/// 特定应用详情订阅的请求体模型
/// </summary>
public class SubscriptionAppInfo : SubscriptionInfo
{
    /// <summary>
    /// 订阅的应用程序路径
    /// </summary>
    public string ApplicationPath { get; set; }
}
