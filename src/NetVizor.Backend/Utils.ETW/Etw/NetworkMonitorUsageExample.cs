using System.Runtime.CompilerServices;
using System.Text.Json;
using Common.Utils;
using Utils.ETW.Core;

namespace Utils.ETW.Etw;

/// <summary>
/// 网络监控系统使用示例
/// </summary>
public class NetworkMonitorUsageExample
{
    private EnhancedEtwNetworkManager _networkManager;
    private CancellationTokenSource _cancellationTokenSource;

    public void RunExample()
    {
        Console.WriteLine("启动网络监控系统...");

        // 初始化网络管理器
        _networkManager = new EnhancedEtwNetworkManager();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // 启动监控
            _networkManager.StartMonitoring();

            // 启动定期读取任务
            var readTask = Task.Run(() => ReadNetworkDataPeriodically(_cancellationTokenSource.Token));

            // 启动WebSocket推送任务（示例）
            // var websocketTask = Task.Run(() => SimulateWebSocketPush(_cancellationTokenSource.Token));

            // Console.WriteLine("网络监控已启动，按任意键停止...");
            // Console.ReadKey();
            //
            // // 停止监控
            // _cancellationTokenSource.Cancel();
            // await Task.WhenAll(readTask, websocketTask);
        }
        finally
        {
            // _networkManager?.Dispose();
            // Console.WriteLine("网络监控已停止！！！！！！！！！！！！！11！！！！");
        }
    }

    /// <summary>
    /// 定期读取网络数据
    /// </summary>
    private async Task ReadNetworkDataPeriodically(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 获取网络监控快照
                var snapshot = GlobalNetworkMonitor.Instance.GetSnapshot();

                var serialize = JsonHelper.ToJson(snapshot);
                Console.WriteLine("=================================================");
                Console.WriteLine(serialize);
                Console.WriteLine("=================================================");
                // 输出概要信息
                Console.WriteLine($"\n========== 网络监控快照 {snapshot.SnapshotTime:yyyy-MM-dd HH:mm:ss} ==========");
                Console.WriteLine($"活跃应用程序: {snapshot.GlobalStats.TotalApplications}");
                Console.WriteLine(
                    $"总连接数: {snapshot.GlobalStats.TotalConnections} (活跃: {snapshot.GlobalStats.ActiveConnections})");

                // 输出每个应用的详细信息
                // foreach (var app in snapshot.Applications.OrderByDescending(a =>
                //              a.TotalSendSpeed + a.TotalReceiveSpeed))
                // {
                //     Console.WriteLine($"\n应用程序: {app.ApplicationName} (PID: {app.ProcessId})");
                //     Console.WriteLine($"  路径: {app.ApplicationPath}");
                //     Console.WriteLine($"  版本: {app.Version}");
                //     Console.WriteLine($"  连接数: {app.ActiveConnections}/{app.TotalConnections}");
                //     Console.WriteLine(
                //         $"  总流量: 发送 {FormatBytes(app.TotalBytesSent)}, 接收 {FormatBytes(app.TotalBytesReceived)}");
                //     Console.WriteLine(
                //         $"  当前速度: ↑{FormatSpeed(app.TotalSendSpeed)} ↓{FormatSpeed(app.TotalReceiveSpeed)}");
                //
                //     // 显示前5个活跃连接
                //     var topConnections = app.Connections
                //         .Where(c => c.IsActive)
                //         .OrderByDescending(c => c.CurrentSendSpeed + c.CurrentReceiveSpeed)
                //         .Take(5);
                //
                //     foreach (var conn in topConnections)
                //     {
                //         Console.WriteLine(
                //             $"    → {conn.Protocol} {conn.LocalPort} → {conn.RemoteIp}:{conn.RemotePort} " +
                //             $"({conn.RemoteDomain ?? "未解析"}) " +
                //             $"[{conn.State}] " +
                //             $"↑{FormatSpeed(conn.CurrentSendSpeed)} ↓{FormatSpeed(conn.CurrentReceiveSpeed)}");
                //     }
                // }
                //
                // // 输出端口流量TOP 5
                // Console.WriteLine("\n端口流量 TOP 5:");
                // var topPorts = snapshot.GlobalStats.PortTraffic
                //     .OrderByDescending(p => p.TotalBytes)
                //     .Take(5);
                //
                // foreach (var port in topPorts)
                // {
                //     Console.WriteLine($"  端口 {port.Port}: {FormatBytes(port.TotalBytes)} " +
                //                       $"(发送: {FormatBytes(port.BytesSent)}, 接收: {FormatBytes(port.BytesReceived)})");
                // }
                //
                // // 输出IP流量TOP 5
                // Console.WriteLine("\nIP流量 TOP 5:");
                // var topIps = snapshot.GlobalStats.IpTraffic.Take(5);
                //
                // foreach (var ip in topIps)
                // {
                //     Console.WriteLine($"  {ip.IpAddress}: {FormatBytes(ip.TotalBytes)} " +
                //                       $"(发送: {FormatBytes(ip.BytesSent)}, 接收: {FormatBytes(ip.BytesReceived)})");
                // }

                await Task.Delay(5000, cancellationToken); // 每5秒更新一次
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取网络数据时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 模拟WebSocket推送
    /// </summary>
    // private async Task SimulateWebSocketPush(CancellationToken cancellationToken)
    // {
    //     while (!cancellationToken.IsCancellationRequested)
    //     {
    //         try
    //         {
    //             // 获取数据快照
    //             var snapshot = GlobalNetworkMonitor.Instance.GetSnapshot();
    //
    //             // 构建要推送的数据（根据前端需求定制）
    //             var pushData = new
    //             {
    //                 timestamp = snapshot.SnapshotTime,
    //                 applications = snapshot.Applications.Select(app => new
    //                 {
    //                     processId = app.ProcessId,
    //                     appName = app.ApplicationName,
    //                     appPath = app.ApplicationPath,
    //                     version = app.Version,
    //                     connections = app.Connections.Select(conn => new
    //                     {
    //                         protocol = conn.Protocol,
    //                         localPort = conn.LocalPort,
    //                         remoteIp = conn.RemoteIp,
    //                         remotePort = conn.RemotePort,
    //                         remoteDomain = conn.RemoteDomain,
    //                         state = conn.State,
    //                         bytesSent = conn.BytesSent,
    //                         bytesReceived = conn.BytesReceived,
    //                         sendSpeed = conn.CurrentSendSpeed,
    //                         receiveSpeed = conn.CurrentReceiveSpeed,
    //                         duration = conn.Duration.TotalSeconds
    //                     }),
    //                     stats = new
    //                     {
    //                         totalConnections = app.TotalConnections,
    //                         activeConnections = app.ActiveConnections,
    //                         totalBytesSent = app.TotalBytesSent,
    //                         totalBytesReceived = app.TotalBytesReceived,
    //                         totalSendSpeed = app.TotalSendSpeed,
    //                         totalReceiveSpeed = app.TotalReceiveSpeed
    //                     }
    //                 }),
    //                 globalStats = new
    //                 {
    //                     totalApps = snapshot.GlobalStats.TotalApplications,
    //                     totalConnections = snapshot.GlobalStats.TotalConnections,
    //                     activeConnections = snapshot.GlobalStats.ActiveConnections
    //                 }
    //             };
    //
    //             // 序列化为JSON
    //             var json = JsonHelper.ToJson(pushData);
    //
    //             // 这里应该通过WebSocket发送数据
    //             // await websocketClient.SendAsync(json);
    //             Console.WriteLine($"\n[WebSocket模拟] 推送数据大小: {json.Length} 字节");
    //             Console.WriteLine(json);
    //             await Task.Delay(1000, cancellationToken); // 每秒推送一次
    //         }
    //         catch (Exception ex)
    //         {
    //             Console.WriteLine($"WebSocket推送时出错: {ex.Message}");
    //         }
    //     }
    // }

    /// <summary>
    /// 格式化字节数
    /// </summary>
    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 格式化速度
    /// </summary>
    private string FormatSpeed(double bytesPerSecond)
    {
        return FormatBytes((long)bytesPerSecond) + "/s";
    }
}