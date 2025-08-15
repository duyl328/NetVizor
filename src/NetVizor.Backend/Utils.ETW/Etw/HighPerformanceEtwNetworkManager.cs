using Common.Logger;
using Utils.ETW.Core;
using Utils.ETW.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Utils.ETW.Etw;

/// <summary>
/// 高性能ETW网络管理器 - 解决高带宽下的数据统计问题
/// </summary>
public class HighPerformanceEtwNetworkManager : IDisposable
{
    private readonly OptimizedETWNetworkCapture _networkCapture;
    private bool _isMonitoring = false;

    // 使用高性能数据结构存储流量统计
    private readonly ConcurrentDictionary<int, ProcessTrafficStats> _processStats = new();
    private readonly ConcurrentDictionary<string, ConnectionTrafficStats> _connectionStats = new();

    // 定时器用于定期汇总和清理数据
    private readonly Timer _aggregationTimer;
    private readonly Timer _cleanupTimer;

    // 性能监控
    private readonly Stopwatch _performanceWatch = Stopwatch.StartNew();
    private long _lastStatsTime = 0;

    public event Action<ProcessTrafficSnapshot>? OnProcessTrafficUpdate;
    public event Action<List<NetworkConnection>>? OnActiveConnectionsUpdate;
    public event Action<EtwPerformanceStats>? OnPerformanceStatsUpdate;

    public HighPerformanceEtwNetworkManager()
    {
        _networkCapture = new OptimizedETWNetworkCapture();

        // 订阅批量事件处理（性能更好）
        _networkCapture.OnBatchTcpEvents += HandleTcpBatch;
        _networkCapture.OnBatchUdpEvents += HandleUdpBatch;

        // 每500ms汇总一次流量统计
        _aggregationTimer = new Timer(AggregateTrafficStats, null, 500, 500);

        // 每30秒清理一次过期数据
        _cleanupTimer = new Timer(CleanupExpiredData, null, 30000, 30000);

        Log.Info("高性能ETW网络管理器已创建");
    }

    public void StartMonitoring()
    {
        if (_isMonitoring) return;

        try
        {
            _networkCapture.StartCapture();
            _isMonitoring = true;
            _lastStatsTime = _performanceWatch.ElapsedMilliseconds;

            Log.Info("高性能ETW网络监控已启动");
        }
        catch (Exception ex)
        {
            Log.Error($"启动高性能ETW监控失败: {ex.Message}");
            throw;
        }
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;

        _isMonitoring = false;
        _networkCapture.StopCapture();

        Log.Info("高性能ETW网络监控已停止");
    }

    /// <summary>
    /// 批量处理TCP事件 - 大幅提升性能
    /// </summary>
    private void HandleTcpBatch(List<TcpConnectionEventData> tcpEvents)
    {
        var processUpdates = new Dictionary<int, long>();
        var connectionUpdates = new Dictionary<string, (long sent, long received)>();

        // 批量累积统计，减少锁竞争
        foreach (var tcpEvent in tcpEvents)
        {
            // 更新进程统计
            if (!processUpdates.ContainsKey(tcpEvent.ProcessId))
                processUpdates[tcpEvent.ProcessId] = 0;

            if (tcpEvent.EventType == "TcpSend")
                processUpdates[tcpEvent.ProcessId] += tcpEvent.DataLength;
            else if (tcpEvent.EventType == "TcpReceive")
                processUpdates[tcpEvent.ProcessId] += tcpEvent.DataLength;

            // 更新连接统计
            var connectionKey =
                $"{tcpEvent.ProcessId}_{tcpEvent.SourceIp}:{tcpEvent.SourcePort}_{tcpEvent.DestinationIp}:{tcpEvent.DestinationPort}";
            if (!connectionUpdates.ContainsKey(connectionKey))
                connectionUpdates[connectionKey] = (0, 0);

            var current = connectionUpdates[connectionKey];
            if (tcpEvent.EventType == "TcpSend")
                connectionUpdates[connectionKey] = (current.sent + tcpEvent.DataLength, current.received);
            else if (tcpEvent.EventType == "TcpReceive")
                connectionUpdates[connectionKey] = (current.sent, current.received + tcpEvent.DataLength);
        }

        // 批量更新统计数据
        UpdateProcessStats(processUpdates);
        UpdateConnectionStats(connectionUpdates, tcpEvents);
    }

    /// <summary>
    /// 批量处理UDP事件
    /// </summary>
    private void HandleUdpBatch(List<UdpPacketEventData> udpEvents)
    {
        var processUpdates = new Dictionary<int, long>();

        foreach (var udpEvent in udpEvents)
        {
            if (!processUpdates.ContainsKey(udpEvent.ProcessId))
                processUpdates[udpEvent.ProcessId] = 0;

            processUpdates[udpEvent.ProcessId] += udpEvent.DataLength;
        }

        UpdateProcessStats(processUpdates);
    }

    /// <summary>
    /// 批量更新进程统计
    /// </summary>
    private void UpdateProcessStats(Dictionary<int, long> processUpdates)
    {
        foreach (var update in processUpdates)
        {
            _processStats.AddOrUpdate(update.Key,
                new ProcessTrafficStats
                {
                    ProcessId = update.Key,
                    TotalBytes = update.Value,
                    LastUpdate = DateTime.Now
                },
                (key, existing) =>
                {
                    existing.TotalBytes += update.Value;
                    existing.LastUpdate = DateTime.Now;
                    return existing;
                });
        }
    }

    /// <summary>
    /// 批量更新连接统计
    /// </summary>
    private void UpdateConnectionStats(Dictionary<string, (long sent, long received)> connectionUpdates,
        List<TcpConnectionEventData> events)
    {
        foreach (var update in connectionUpdates)
        {
            var sampleEvent = events.FirstOrDefault();
            if (sampleEvent != null)
            {
                _connectionStats.AddOrUpdate(update.Key,
                    new ConnectionTrafficStats
                    {
                        ProcessId = sampleEvent.ProcessId,
                        SourceIp = sampleEvent.SourceIp?.ToString() ?? "",
                        SourcePort = sampleEvent.SourcePort,
                        DestinationIp = sampleEvent.DestinationIp?.ToString() ?? "",
                        DestinationPort = sampleEvent.DestinationPort,
                        BytesSent = update.Value.sent,
                        BytesReceived = update.Value.received,
                        LastUpdate = DateTime.Now
                    },
                    (key, existing) =>
                    {
                        existing.BytesSent += update.Value.sent;
                        existing.BytesReceived += update.Value.received;
                        existing.LastUpdate = DateTime.Now;
                        return existing;
                    });
            }
        }
    }

    /// <summary>
    /// 定期汇总流量统计并通知订阅者
    /// </summary>
    private void AggregateTrafficStats(object? state)
    {
        try
        {
            var currentTime = _performanceWatch.ElapsedMilliseconds;
            var timeDelta = (currentTime - _lastStatsTime) / 1000.0; // 转换为秒

            if (timeDelta < 0.1) return; // 避免除零和过于频繁的计算

            // 计算进程流量快照
            var processSnapshots = new List<ProcessTrafficSnapshot>();
            foreach (var processStats in _processStats.Values)
            {
                processSnapshots.Add(new ProcessTrafficSnapshot
                {
                    ProcessId = processStats.ProcessId,
                    ProcessName = GetProcessName(processStats.ProcessId),
                    TotalBytes = processStats.TotalBytes,
                    BytesPerSecond = (long)(processStats.TotalBytes / timeDelta),
                    ConnectionCount = _connectionStats.Values.Count(c => c.ProcessId == processStats.ProcessId),
                    LastUpdate = processStats.LastUpdate
                });
            }

            // 触发进程流量更新事件
            foreach (var snapshot in processSnapshots.Take(50)) // 只发送前50个最活跃的进程
            {
                OnProcessTrafficUpdate?.Invoke(snapshot);
            }

            // 触发性能统计事件
            OnPerformanceStatsUpdate?.Invoke(_networkCapture.GetPerformanceStats());

            _lastStatsTime = currentTime;
        }
        catch (Exception ex)
        {
            Log.Error($"汇总流量统计异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理过期数据
    /// </summary>
    private void CleanupExpiredData(object? state)
    {
        try
        {
            var cutoffTime = DateTime.Now.AddMinutes(-5);
            var expiredProcesses = new List<int>();
            var expiredConnections = new List<string>();

            // 清理过期进程统计
            foreach (var kvp in _processStats)
            {
                if (kvp.Value.LastUpdate < cutoffTime)
                    expiredProcesses.Add(kvp.Key);
            }

            foreach (var pid in expiredProcesses)
            {
                _processStats.TryRemove(pid, out _);
            }

            // 清理过期连接统计
            foreach (var kvp in _connectionStats)
            {
                if (kvp.Value.LastUpdate < cutoffTime)
                    expiredConnections.Add(kvp.Key);
            }

            foreach (var connKey in expiredConnections)
            {
                _connectionStats.TryRemove(connKey, out _);
            }

            if (expiredProcesses.Count > 0 || expiredConnections.Count > 0)
            {
                Log.Info($"清理过期数据: 进程={expiredProcesses.Count}, 连接={expiredConnections.Count}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"清理过期数据异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取进程名称（带缓存）
    /// </summary>
    private static readonly ConcurrentDictionary<int, string> ProcessNameCache = new();

    private string GetProcessName(int processId)
    {
        return ProcessNameCache.GetOrAdd(processId, pid =>
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                return process.ProcessName;
            }
            catch
            {
                return $"PID_{pid}";
            }
        });
    }

    /// <summary>
    /// 获取当前活跃连接
    /// </summary>
    public List<NetworkConnection> GetActiveConnections(int topN = 100)
    {
        return _connectionStats.Values
            .Where(c => c.LastUpdate > DateTime.Now.AddMinutes(-2))
            .OrderByDescending(c => c.BytesSent + c.BytesReceived)
            .Take(topN)
            .Select(c => new NetworkConnection
            {
                ProcessId = c.ProcessId,
                ProcessName = GetProcessName(c.ProcessId),
                SourceIp = c.SourceIp,
                SourcePort = c.SourcePort,
                DestinationIp = c.DestinationIp,
                DestinationPort = c.DestinationPort,
                BytesSent = c.BytesSent,
                BytesReceived = c.BytesReceived,
                IsActive = true
            })
            .ToList();
    }

    /// <summary>
    /// 获取ETW性能统计
    /// </summary>
    public EtwPerformanceStats GetPerformanceStats()
    {
        return _networkCapture.GetPerformanceStats();
    }

    public void Dispose()
    {
        StopMonitoring();
        _aggregationTimer?.Dispose();
        _cleanupTimer?.Dispose();
        _networkCapture?.Dispose();
    }
}

#region 数据模型

public class ProcessTrafficStats
{
    public int ProcessId { get; set; }
    public long TotalBytes { get; set; }
    public DateTime LastUpdate { get; set; }
}

public class ConnectionTrafficStats
{
    public int ProcessId { get; set; }
    public string SourceIp { get; set; } = "";
    public int SourcePort { get; set; }
    public string DestinationIp { get; set; } = "";
    public int DestinationPort { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public DateTime LastUpdate { get; set; }
}

public class ProcessTrafficSnapshot
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = "";
    public long TotalBytes { get; set; }
    public long BytesPerSecond { get; set; }
    public int ConnectionCount { get; set; }
    public DateTime LastUpdate { get; set; }
}

public class NetworkConnection
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = "";
    public string SourceIp { get; set; } = "";
    public int SourcePort { get; set; }
    public string DestinationIp { get; set; } = "";
    public int DestinationPort { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public bool IsActive { get; set; }
}

#endregion