using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logger;
using Utils.ETW.Core;

namespace Utils.ETW.Core;

/// <summary>
/// 高精度网络监控器 - 基于TCP连接表的精确流量统计
/// 解决ETW在突发高流量下的统计不准确问题
/// 这是替代ETW方案的核心实现
/// </summary>
public class HighPrecisionNetworkMonitor : IDisposable
{
    private readonly Timer _monitorTimer;

    private readonly ConcurrentDictionary<string, TcpConnectionStatsCollector.TcpConnectionStats>
        _previousStats = new();

    private readonly ConcurrentDictionary<uint, ProcessNetworkStats> _processStats = new();
    private readonly object _lockObject = new();

    // 配置参数
    private readonly int _updateIntervalMs;
    private readonly bool _enableDetailedStats;
    private bool _isRunning = false;

    // 性能统计
    private long _totalConnections = 0;
    private long _activeConnections = 0;
    private long _monitoringCycles = 0;
    private DateTime _lastUpdate = DateTime.Now;

    // 事件
    public event Action<ProcessNetworkStats>? OnProcessStatsUpdated;
    public event Action<List<ProcessNetworkStats>>? OnAllProcessStatsUpdated;
    public event Action<MonitoringPerformanceStats>? OnPerformanceStatsUpdated;

    public HighPrecisionNetworkMonitor(int updateIntervalMs = 1000, bool enableDetailedStats = false)
    {
        _updateIntervalMs = updateIntervalMs;
        _enableDetailedStats = enableDetailedStats;

        _monitorTimer = new Timer(MonitoringTick, null, Timeout.Infinite, Timeout.Infinite);

        Log.Info($"高精度网络监控器已创建 - 更新间隔: {updateIntervalMs}ms, 详细统计: {enableDetailedStats}");
    }

    /// <summary>
    /// 启动监控
    /// </summary>
    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _lastUpdate = DateTime.Now;

        // 启动定时器
        _monitorTimer.Change(0, _updateIntervalMs);

        Log.Info("高精度网络监控已启动");
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _monitorTimer.Change(Timeout.Infinite, Timeout.Infinite);

        Log.Info("高精度网络监控已停止");
    }

    /// <summary>
    /// 监控主循环
    /// </summary>
    private void MonitoringTick(object? state)
    {
        if (!_isRunning) return;

        try
        {
            var startTime = DateTime.Now;

            // 1. 获取所有已建立的TCP连接
            var connections = TcpConnectionEnumerator.GetTcpConnections(onlyEstablished: true);
            _totalConnections = connections.Count;

            // 2. 获取统计信息并计算速率
            var currentStats = ProcessConnections(connections);

            // 3. 按进程汇总统计
            var processStats = AggregateStatsByProcess(currentStats);

            // 4. 更新进程统计并触发事件
            UpdateProcessStats(processStats);

            // 5. 更新性能统计
            var processingTime = (DateTime.Now - startTime).TotalMilliseconds;
            UpdatePerformanceStats(processingTime);

            _monitoringCycles++;
            _lastUpdate = DateTime.Now;
        }
        catch (Exception ex)
        {
            Log.Error($"网络监控周期执行异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理连接列表，获取统计信息并计算速率
    /// </summary>
    private List<TcpConnectionStatsCollector.TcpConnectionStats> ProcessConnections(
        List<TcpConnectionEnumerator.TcpConnectionInfo> connections)
    {
        var currentStatsList = new List<TcpConnectionStatsCollector.TcpConnectionStats>();
        _activeConnections = 0;

        foreach (var connection in connections)
        {
            var connectionKey = connection.ConnectionKey;

            // 尝试获取连接统计（如果支持详细统计）
            TcpConnectionStatsCollector.TcpConnectionStats? currentStats = null;

            if (_enableDetailedStats)
            {
                currentStats = TcpConnectionStatsCollector.GetConnectionStats(connection);
            }

            // 如果详细统计不可用，创建基础统计记录
            if (currentStats == null || !currentStats.StatsAvailable)
            {
                currentStats = new TcpConnectionStatsCollector.TcpConnectionStats
                {
                    ConnectionKey = connectionKey,
                    ProcessId = connection.ProcessId,
                    LocalEndpoint = $"{connection.LocalAddress}:{connection.LocalPort}",
                    RemoteEndpoint = $"{connection.RemoteAddress}:{connection.RemotePort}",
                    LastUpdate = DateTime.Now,
                    StatsAvailable = false,
                    ErrorMessage = "详细统计不可用，使用基础监控"
                };
            }

            // 如果有上一次的统计数据，计算速率
            if (_previousStats.TryGetValue(connectionKey, out var previousStats))
            {
                currentStats = TcpConnectionStatsCollector.CalculateSpeed(previousStats, currentStats);

                // 只有当有速率数据时才计为活跃连接
                if (currentStats.BytesInPerSecond > 0 || currentStats.BytesOutPerSecond > 0)
                {
                    _activeConnections++;
                }
            }

            // 更新缓存
            _previousStats.AddOrUpdate(connectionKey, currentStats, (key, oldValue) => currentStats);
            currentStatsList.Add(currentStats);
        }

        // 清理已断开连接的统计数据
        CleanupDisconnectedConnections(connections);

        return currentStatsList;
    }

    /// <summary>
    /// 按进程汇总网络统计
    /// </summary>
    private Dictionary<uint, ProcessNetworkStats> AggregateStatsByProcess(
        List<TcpConnectionStatsCollector.TcpConnectionStats> connectionStats)
    {
        var processStatsDict = new Dictionary<uint, ProcessNetworkStats>();

        var groupedByProcess = connectionStats.GroupBy(s => s.ProcessId);

        foreach (var group in groupedByProcess)
        {
            var processId = group.Key;
            var connections = group.ToList();

            var processStats = new ProcessNetworkStats
            {
                ProcessId = processId,
                ProcessName = GetProcessName(processId),
                ConnectionCount = connections.Count,
                LastUpdate = DateTime.Now
            };

            // 汇总所有连接的流量
            foreach (var conn in connections)
            {
                processStats.TotalBytesIn += conn.TotalBytesIn;
                processStats.TotalBytesOut += conn.TotalBytesOut;
                processStats.BytesInPerSecond += conn.BytesInPerSecond;
                processStats.BytesOutPerSecond += conn.BytesOutPerSecond;

                if (conn.StatsAvailable)
                {
                    processStats.DetailedStatsAvailable = true;
                }
            }

            processStats.TotalBytesPerSecond = processStats.BytesInPerSecond + processStats.BytesOutPerSecond;
            processStatsDict[processId] = processStats;
        }

        return processStatsDict;
    }

    /// <summary>
    /// 更新进程统计并触发事件
    /// </summary>
    private void UpdateProcessStats(Dictionary<uint, ProcessNetworkStats> newStats)
    {
        var updatedProcesses = new List<ProcessNetworkStats>();

        foreach (var kvp in newStats)
        {
            var processId = kvp.Key;
            var newProcessStats = kvp.Value;

            // 更新进程统计缓存
            _processStats.AddOrUpdate(processId, newProcessStats, (key, oldValue) => newProcessStats);

            // 只有活跃进程才触发更新事件（有实际流量）
            if (newProcessStats.TotalBytesPerSecond > 0)
            {
                updatedProcesses.Add(newProcessStats);
                OnProcessStatsUpdated?.Invoke(newProcessStats);
            }
        }

        // 触发批量更新事件
        if (updatedProcesses.Count > 0)
        {
            OnAllProcessStatsUpdated?.Invoke(updatedProcesses);
        }
    }

    /// <summary>
    /// 清理已断开连接的统计数据
    /// </summary>
    private void CleanupDisconnectedConnections(List<TcpConnectionEnumerator.TcpConnectionInfo> activeConnections)
    {
        var activeConnectionKeys = activeConnections.Select(c => c.ConnectionKey).ToHashSet();
        var keysToRemove = _previousStats.Keys.Where(key => !activeConnectionKeys.Contains(key)).ToList();

        foreach (var key in keysToRemove)
        {
            _previousStats.TryRemove(key, out _);
        }

        if (keysToRemove.Count > 0)
        {
            Log.Debug($"清理了 {keysToRemove.Count} 个已断开连接的统计记录");
        }
    }

    /// <summary>
    /// 更新性能统计
    /// </summary>
    private void UpdatePerformanceStats(double processingTimeMs)
    {
        var perfStats = new MonitoringPerformanceStats
        {
            TotalConnections = _totalConnections,
            ActiveConnections = _activeConnections,
            MonitoringCycles = _monitoringCycles,
            LastProcessingTimeMs = processingTimeMs,
            LastUpdate = _lastUpdate,
            UpdateIntervalMs = _updateIntervalMs,
            CachedConnectionStats = _previousStats.Count
        };

        OnPerformanceStatsUpdated?.Invoke(perfStats);
    }

    /// <summary>
    /// 获取进程名称（带缓存）
    /// </summary>
    private static readonly ConcurrentDictionary<uint, string> ProcessNameCache = new();

    private static string GetProcessName(uint processId)
    {
        return ProcessNameCache.GetOrAdd(processId, pid =>
        {
            try
            {
                using var process = Process.GetProcessById((int)pid);
                return process.ProcessName;
            }
            catch
            {
                return $"PID_{pid}";
            }
        });
    }

    /// <summary>
    /// 获取指定进程的网络统计
    /// </summary>
    public ProcessNetworkStats? GetProcessStats(uint processId)
    {
        return _processStats.GetValueOrDefault(processId);
    }

    /// <summary>
    /// 获取所有活跃进程的网络统计
    /// </summary>
    public List<ProcessNetworkStats> GetAllActiveProcessStats()
    {
        return _processStats.Values
            .Where(stats => stats.TotalBytesPerSecond > 0)
            .OrderByDescending(stats => stats.TotalBytesPerSecond)
            .ToList();
    }

    /// <summary>
    /// 获取网络监控性能统计
    /// </summary>
    public MonitoringPerformanceStats GetPerformanceStats()
    {
        return new MonitoringPerformanceStats
        {
            TotalConnections = _totalConnections,
            ActiveConnections = _activeConnections,
            MonitoringCycles = _monitoringCycles,
            LastUpdate = _lastUpdate,
            UpdateIntervalMs = _updateIntervalMs,
            CachedConnectionStats = _previousStats.Count
        };
    }

    public void Dispose()
    {
        Stop();
        _monitorTimer?.Dispose();
        _previousStats.Clear();
        _processStats.Clear();
    }

    #region 数据模型

    /// <summary>
    /// 进程网络统计信息
    /// </summary>
    public class ProcessNetworkStats
    {
        public uint ProcessId { get; set; }
        public string ProcessName { get; set; } = "";
        public int ConnectionCount { get; set; }
        public ulong TotalBytesIn { get; set; }
        public ulong TotalBytesOut { get; set; }
        public double BytesInPerSecond { get; set; }
        public double BytesOutPerSecond { get; set; }
        public double TotalBytesPerSecond { get; set; }
        public bool DetailedStatsAvailable { get; set; }
        public DateTime LastUpdate { get; set; }

        public string FormattedDownloadSpeed => FormatSpeed(BytesInPerSecond);
        public string FormattedUploadSpeed => FormatSpeed(BytesOutPerSecond);
        public string FormattedTotalSpeed => FormatSpeed(TotalBytesPerSecond);

        private static string FormatSpeed(double bytesPerSec)
        {
            if (bytesPerSec >= 1024 * 1024 * 1024)
                return $"{bytesPerSec / (1024.0 * 1024 * 1024):F1} GB/s";
            if (bytesPerSec >= 1024 * 1024)
                return $"{bytesPerSec / (1024.0 * 1024):F1} MB/s";
            if (bytesPerSec >= 1024)
                return $"{bytesPerSec / 1024.0:F1} KB/s";
            return $"{bytesPerSec:F0} B/s";
        }

        public override string ToString()
        {
            return $"{ProcessName} (PID:{ProcessId}): {FormattedTotalSpeed} " +
                   $"[↓{FormattedDownloadSpeed} ↑{FormattedUploadSpeed}] {ConnectionCount}连接";
        }
    }

    /// <summary>
    /// 监控性能统计信息
    /// </summary>
    public class MonitoringPerformanceStats
    {
        public long TotalConnections { get; set; }
        public long ActiveConnections { get; set; }
        public long MonitoringCycles { get; set; }
        public double LastProcessingTimeMs { get; set; }
        public DateTime LastUpdate { get; set; }
        public int UpdateIntervalMs { get; set; }
        public int CachedConnectionStats { get; set; }

        public double EfficiencyPercentage => TotalConnections > 0
            ? (ActiveConnections * 100.0) / TotalConnections
            : 0;

        public override string ToString()
        {
            return $"监控效率: {ActiveConnections}/{TotalConnections} ({EfficiencyPercentage:F1}%), " +
                   $"处理耗时: {LastProcessingTimeMs:F1}ms, 周期: {MonitoringCycles}";
        }
    }

    #endregion
}