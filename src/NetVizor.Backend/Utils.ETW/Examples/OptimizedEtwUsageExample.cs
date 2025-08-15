using Common.Logger;
using Utils.ETW.Core;
using Utils.ETW.Etw;
using Utils.ETW.Tools;

namespace Utils.ETW.Examples;

/// <summary>
/// 优化的ETW使用示例 - 解决高带宽下的数据统计问题
/// </summary>
public class OptimizedEtwUsageExample : IDisposable
{
    private readonly HighPerformanceEtwNetworkManager _etwManager;
    private readonly NetworkMonitoringComparison _comparison;
    private readonly Timer _reportTimer;

    public OptimizedEtwUsageExample()
    {
        // 创建高性能ETW管理器
        _etwManager = new HighPerformanceEtwNetworkManager();

        // 创建网络监控比对工具
        _comparison = new NetworkMonitoringComparison();

        // 订阅事件
        _etwManager.OnProcessTrafficUpdate += OnProcessTrafficUpdate;
        _etwManager.OnActiveConnectionsUpdate += OnActiveConnectionsUpdate;
        _etwManager.OnPerformanceStatsUpdate += OnPerformanceStatsUpdate;

        _comparison.OnComparisonReport += OnComparisonReport;

        // 定时输出性能报告
        _reportTimer = new Timer(OutputPerformanceReport, null, 10000, 10000);
    }

    public async Task StartMonitoringAsync()
    {
        Log.Info("=== 开始优化的ETW网络监控演示 ===");

        // 启动ETW监控
        _etwManager.StartMonitoring();

        // 启动数据比对
        _comparison.StartComparison();

        Log.Info("监控已启动，请执行网络活动（如Chrome网速测试）来验证性能...");

        // 模拟运行30秒
        await Task.Delay(30000);

        // 输出最终统计报告
        OutputFinalReport();
    }

    private void OnProcessTrafficUpdate(ProcessTrafficSnapshot snapshot)
    {
        // 只记录高流量进程
        if (snapshot.BytesPerSecond > 1024 * 1024) // 1MB/s
        {
            Log.Info($"[高流量进程] {snapshot.ProcessName} (PID:{snapshot.ProcessId}): " +
                     $"{FormatBytes(snapshot.BytesPerSecond)}/s, " +
                     $"连接数={snapshot.ConnectionCount}, " +
                     $"总流量={FormatBytes(snapshot.TotalBytes)}");
        }
    }

    private void OnActiveConnectionsUpdate(List<NetworkConnection> connections)
    {
        var topConnections = connections.Take(5);
        foreach (var conn in topConnections)
        {
            Log.Debug($"[活跃连接] {conn.ProcessName}: " +
                      $"{conn.SourceIp}:{conn.SourcePort} -> {conn.DestinationIp}:{conn.DestinationPort} " +
                      $"(发送:{FormatBytes(conn.BytesSent)}, 接收:{FormatBytes(conn.BytesReceived)})");
        }
    }

    private void OnPerformanceStatsUpdate(EtwPerformanceStats stats)
    {
        if (stats.DropRate > 5.0) // 丢包率超过5%时警告
        {
            Log.Warning($"[ETW性能警告] 事件丢失率过高: {stats.DropRate:F2}%, " +
                        $"TCP队列长度:{stats.TcpQueueLength}, UDP队列长度:{stats.UdpQueueLength}");
        }
    }

    private void OnComparisonReport(NetworkMonitoringReport report)
    {
        var global = report.GlobalComparison;

        Log.Info($"=== 网络监控数据比对报告 ({report.Timestamp:HH:mm:ss}) ===");
        Log.Info($"全局网速: 下载={FormatBytes(global.GlobalDownloadSpeed)}/s, " +
                 $"上传={FormatBytes(global.GlobalUploadSpeed)}/s, " +
                 $"总计={FormatBytes(global.GlobalTotalSpeed)}/s");
        Log.Info($"ETW统计: 总计={FormatBytes(global.EtwTotalSpeed)}/s");
        Log.Info($"数据差异: {FormatBytes(Math.Abs(global.SpeedDifference))}/s " +
                 $"({Math.Abs(global.SpeedDifferencePercentage):F1}%) - {global.Status}");

        if (Math.Abs(global.SpeedDifferencePercentage) > 20)
        {
            Log.Warning("⚠️ 数据差异较大，可能原因:");
            Log.Warning("  1. ETW事件丢失（缓冲区不足）");
            Log.Warning("  2. 事件处理延迟");
            Log.Warning("  3. 非TCP/IP流量（如VPN、隧道等）");
        }

        Log.Info($"ETW性能: 接收事件={report.EtwPerformanceStats.TotalEventsReceived}, " +
                 $"处理事件={report.EtwPerformanceStats.TotalEventsProcessed}, " +
                 $"丢弃事件={report.EtwPerformanceStats.EventsDropped}, " +
                 $"丢失率={report.EtwPerformanceStats.DropRate:F2}%");
    }

    private void OutputPerformanceReport(object? state)
    {
        try
        {
            var etwStats = _etwManager.GetPerformanceStats();
            var globalComparison = _comparison.GetGlobalComparison();

            // 简化的性能报告
            if (etwStats.TotalEventsReceived > 0)
            {
                Log.Info($"[性能报告] ETW事件处理效率: {etwStats.TotalEventsProcessed}/{etwStats.TotalEventsReceived} " +
                         $"({(etwStats.TotalEventsProcessed * 100.0 / etwStats.TotalEventsReceived):F1}%), " +
                         $"数据准确度: {(100 - Math.Abs(globalComparison.SpeedDifferencePercentage)):F1}%");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"输出性能报告异常: {ex.Message}");
        }
    }

    private void OutputFinalReport()
    {
        Log.Info("=== 最终监控报告 ===");

        var etwStats = _etwManager.GetPerformanceStats();
        var globalComparison = _comparison.GetGlobalComparison();
        var activeConnections = _etwManager.GetActiveConnections(10);

        Log.Info($"ETW总体统计:");
        Log.Info($"  - 总接收事件: {etwStats.TotalEventsReceived:N0}");
        Log.Info($"  - 总处理事件: {etwStats.TotalEventsProcessed:N0}");
        Log.Info($"  - 丢弃事件: {etwStats.EventsDropped:N0}");
        Log.Info($"  - 事件丢失率: {etwStats.DropRate:F2}%");
        Log.Info(
            $"  - 处理效率: {(etwStats.TotalEventsProcessed * 100.0 / Math.Max(etwStats.TotalEventsReceived, 1)):F2}%");

        Log.Info($"数据准确性分析:");
        Log.Info($"  - 最后全局网速: {FormatBytes(globalComparison.GlobalTotalSpeed)}/s");
        Log.Info($"  - 最后ETW统计: {FormatBytes(globalComparison.EtwTotalSpeed)}/s");
        Log.Info($"  - 数据差异: {Math.Abs(globalComparison.SpeedDifferencePercentage):F1}%");
        Log.Info($"  - 准确性评估: {GetAccuracyAssessment(globalComparison.SpeedDifferencePercentage)}");

        Log.Info($"活跃连接统计: {activeConnections.Count} 个");
        foreach (var conn in activeConnections.Take(5))
        {
            Log.Info($"  - {conn.ProcessName}: {conn.DestinationIp}:{conn.DestinationPort} " +
                     $"({FormatBytes(conn.BytesSent + conn.BytesReceived)})");
        }

        // 给出优化建议
        if (etwStats.DropRate > 5.0)
        {
            Log.Warning("🔧 优化建议: ETW事件丢失率过高");
            Log.Warning("  1. 增加ETW缓冲区大小");
            Log.Warning("  2. 提高事件处理线程优先级");
            Log.Warning("  3. 优化事件处理逻辑");
        }

        if (Math.Abs(globalComparison.SpeedDifferencePercentage) > 30)
        {
            Log.Warning("🔧 优化建议: 数据准确性需要改进");
            Log.Warning("  1. 检查是否有VPN或隧道流量");
            Log.Warning("  2. 考虑使用WinDivert等内核级监控");
            Log.Warning("  3. 增加对非TCP/UDP协议的支持");
        }
    }

    private static string GetAccuracyAssessment(double differencePercentage)
    {
        return Math.Abs(differencePercentage) switch
        {
            < 5 => "优秀 (差异<5%)",
            < 15 => "良好 (差异<15%)",
            < 30 => "一般 (差异<30%)",
            _ => "需要改进 (差异>30%)"
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F1} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} B";
    }

    public void Dispose()
    {
        _reportTimer?.Dispose();
        _comparison?.Dispose();
        _etwManager?.Dispose();
    }
}

/// <summary>
/// 快速测试程序入口点
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // 检查管理员权限
        if (!IsRunAsAdministrator())
        {
            Console.WriteLine("此程序需要管理员权限来监听ETW事件！");
            Console.WriteLine("请以管理员身份重新运行程序。");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("优化的ETW网络监控演示程序");
        Console.WriteLine("建议: 在另一个窗口中运行Chrome网速测试来验证监控准确性");
        Console.WriteLine("按任意键开始监控...");
        Console.ReadKey();

        using var example = new OptimizedEtwUsageExample();
        await example.StartMonitoringAsync();

        Console.WriteLine("监控演示完成，按任意键退出...");
        Console.ReadKey();
    }

    private static bool IsRunAsAdministrator()
    {
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}