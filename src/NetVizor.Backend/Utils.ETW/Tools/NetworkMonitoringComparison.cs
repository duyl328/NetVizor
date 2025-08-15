using Common.Logger;
using Common.Utils;
using Utils.ETW.Core;
using Utils.ETW.Etw;
using System.Diagnostics;

namespace Utils.ETW.Tools;

/// <summary>
/// 网络监控数据比对工具 - 用于验证ETW和全局监控的数据一致性
/// </summary>
public class NetworkMonitoringComparison : IDisposable
{
    private readonly HighPerformanceEtwNetworkManager _etwManager;
    private readonly Timer _comparisonTimer;
    private readonly object _lockObject = new();

    // 统计数据
    private Dictionary<int, ProcessComparison> _processComparisons = new();
    private GlobalTrafficComparison _globalComparison = new();

    // 配置
    private readonly TimeSpan _comparisonInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _reportInterval = TimeSpan.FromSeconds(30);

    public event Action<NetworkMonitoringReport>? OnComparisonReport;

    public NetworkMonitoringComparison()
    {
        _etwManager = new HighPerformanceEtwNetworkManager();

        // 订阅ETW事件
        _etwManager.OnProcessTrafficUpdate += OnEtwProcessTrafficUpdate;

        // 定期比对数据
        _comparisonTimer = new Timer(PerformComparison, null,
            _comparisonInterval, _comparisonInterval);

        Log.Info("网络监控数据比对工具已初始化");
    }

    public void StartComparison()
    {
        _etwManager.StartMonitoring();
        Log.Info("网络监控数据比对已开始");
    }

    public void StopComparison()
    {
        _etwManager.StopMonitoring();
        Log.Info("网络监控数据比对已停止");
    }

    private void OnEtwProcessTrafficUpdate(ProcessTrafficSnapshot etwSnapshot)
    {
        lock (_lockObject)
        {
            if (!_processComparisons.ContainsKey(etwSnapshot.ProcessId))
            {
                _processComparisons[etwSnapshot.ProcessId] = new ProcessComparison
                {
                    ProcessId = etwSnapshot.ProcessId,
                    ProcessName = etwSnapshot.ProcessName
                };
            }

            var comparison = _processComparisons[etwSnapshot.ProcessId];
            comparison.EtwTotalBytes = etwSnapshot.TotalBytes;
            comparison.EtwBytesPerSecond = etwSnapshot.BytesPerSecond;
            comparison.EtwConnectionCount = etwSnapshot.ConnectionCount;
            comparison.LastEtwUpdate = etwSnapshot.LastUpdate;
        }
    }

    private void PerformComparison(object? state)
    {
        try
        {
            // 获取全局网络统计
            var globalSpeed = BasicNetworkMonitor.CalculateTotalSpeed();

            lock (_lockObject)
            {
                // 更新全局比对数据
                _globalComparison.GlobalDownloadSpeed = (long)globalSpeed.DownloadSpeed;
                _globalComparison.GlobalUploadSpeed = (long)globalSpeed.UploadSpeed;
                _globalComparison.GlobalTotalSpeed = (long)(globalSpeed.DownloadSpeed + globalSpeed.UploadSpeed);

                // 计算ETW总流量
                var etwTotalSpeed = _processComparisons.Values.Sum(p => p.EtwBytesPerSecond);
                _globalComparison.EtwTotalSpeed = etwTotalSpeed;

                // 计算差异
                _globalComparison.SpeedDifference =
                    _globalComparison.GlobalTotalSpeed - _globalComparison.EtwTotalSpeed;
                _globalComparison.SpeedDifferencePercentage = _globalComparison.GlobalTotalSpeed > 0
                    ? ((double)_globalComparison.SpeedDifference * 100.0) / _globalComparison.GlobalTotalSpeed
                    : 0;

                _globalComparison.LastUpdate = DateTime.Now;

                // 如果差异超过阈值，输出警告
                if (Math.Abs(_globalComparison.SpeedDifferencePercentage) > 20) // 20%阈值
                {
                    Log.Warning($"网络监控数据差异过大: " +
                                $"全局={FormatBytes(_globalComparison.GlobalTotalSpeed)}/s, " +
                                $"ETW={FormatBytes(_globalComparison.EtwTotalSpeed)}/s, " +
                                $"差异={_globalComparison.SpeedDifferencePercentage:F1}%");
                }
            }

            // 定期输出详细报告
            var now = DateTime.Now;
            if (now.Second % 30 == 0) // 每30秒输出一次
            {
                GenerateAndSendReport();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"网络监控数据比对异常: {ex.Message}");
        }
    }

    private void GenerateAndSendReport()
    {
        lock (_lockObject)
        {
            var report = new NetworkMonitoringReport
            {
                Timestamp = DateTime.Now,
                GlobalComparison = _globalComparison,
                ProcessComparisons = _processComparisons.Values.ToList(),
                EtwPerformanceStats = _etwManager.GetPerformanceStats(),
                ActiveConnections = _etwManager.GetActiveConnections(20)
            };

            // 计算汇总统计
            report.Summary = new ComparisonSummary
            {
                TotalProcessesMonitored = _processComparisons.Count,
                TotalActiveConnections = report.ActiveConnections.Count,
                AverageSpeedDifference = _processComparisons.Values
                    .Where(p => p.GlobalBytesPerSecond > 0)
                    .Select(p => Math.Abs(p.SpeedDifferencePercentage))
                    .DefaultIfEmpty(0)
                    .Average(),
                HighDifferenceProcesses = _processComparisons.Values
                    .Where(p => Math.Abs(p.SpeedDifferencePercentage) > 50)
                    .Count()
            };

            OnComparisonReport?.Invoke(report);

            // 输出简化日志
            Log.Info($"网络监控比对报告: " +
                     $"全局速度差异={_globalComparison.SpeedDifferencePercentage:F1}%, " +
                     $"监控进程={report.Summary.TotalProcessesMonitored}, " +
                     $"活跃连接={report.Summary.TotalActiveConnections}, " +
                     $"ETW丢包率={report.EtwPerformanceStats.DropRate:F2}%");
        }
    }

    /// <summary>
    /// 获取特定进程的详细比对信息
    /// </summary>
    public ProcessComparison? GetProcessComparison(int processId)
    {
        lock (_lockObject)
        {
            return _processComparisons.GetValueOrDefault(processId);
        }
    }

    /// <summary>
    /// 获取当前全局比对信息
    /// </summary>
    public GlobalTrafficComparison GetGlobalComparison()
    {
        lock (_lockObject)
        {
            return _globalComparison;
        }
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
        StopComparison();
        _comparisonTimer?.Dispose();
        _etwManager?.Dispose();
    }
}

#region 数据模型

public class ProcessComparison
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = "";

    // ETW统计
    public long EtwTotalBytes { get; set; }
    public long EtwBytesPerSecond { get; set; }
    public int EtwConnectionCount { get; set; }
    public DateTime LastEtwUpdate { get; set; }

    // 全局网络监控无法直接提供进程级别数据
    // 这里主要用于存储推算的数据
    public long GlobalBytesPerSecond { get; set; }

    // 比对结果
    public long SpeedDifference => GlobalBytesPerSecond - EtwBytesPerSecond;

    public double SpeedDifferencePercentage => GlobalBytesPerSecond > 0
        ? (SpeedDifference * 100.0) / GlobalBytesPerSecond
        : 0;
}

public class GlobalTrafficComparison
{
    public long GlobalDownloadSpeed { get; set; }
    public long GlobalUploadSpeed { get; set; }
    public long GlobalTotalSpeed { get; set; }

    public long EtwTotalSpeed { get; set; }

    public long SpeedDifference { get; set; }
    public double SpeedDifferencePercentage { get; set; }

    public DateTime LastUpdate { get; set; }

    public string Status => Math.Abs(SpeedDifferencePercentage) switch
    {
        < 10 => "正常",
        < 30 => "轻微差异",
        < 50 => "显著差异",
        _ => "严重差异"
    };
}

public class NetworkMonitoringReport
{
    public DateTime Timestamp { get; set; }
    public GlobalTrafficComparison GlobalComparison { get; set; } = new();
    public List<ProcessComparison> ProcessComparisons { get; set; } = new();
    public EtwPerformanceStats EtwPerformanceStats { get; set; } = new();
    public List<NetworkConnection> ActiveConnections { get; set; } = new();
    public ComparisonSummary Summary { get; set; } = new();
}

public class ComparisonSummary
{
    public int TotalProcessesMonitored { get; set; }
    public int TotalActiveConnections { get; set; }
    public double AverageSpeedDifference { get; set; }
    public int HighDifferenceProcesses { get; set; }
}

#endregion