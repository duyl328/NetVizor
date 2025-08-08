using System.Collections.Concurrent;
using System.Diagnostics;
using Utils.ETW.Etw;
using Utils.ETW.Models;

namespace Utils.ETW.Services;

/// <summary>
/// 网速Top榜管理器
/// 负责计算和维护各进程的网络速度统计
/// </summary>
public class NetworkTopListManager
{
    private static readonly Lazy<NetworkTopListManager> _instance = new(() => new NetworkTopListManager());
    public static NetworkTopListManager Instance => _instance.Value;

    /// <summary>
    /// 进程网络统计信息缓存
    /// </summary>
    private readonly ConcurrentDictionary<int, ProcessNetworkStats> _processStats = new();

    /// <summary>
    /// 上次统计时间戳
    /// </summary>
    private readonly ConcurrentDictionary<int, DateTime> _lastCalculationTime = new();

    /// <summary>
    /// 上次流量数据
    /// </summary>
    private readonly ConcurrentDictionary<int, (long TotalSent, long TotalReceived)> _lastTrafficData = new();

    /// <summary>
    /// 读写锁
    /// </summary>
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// 统计更新间隔 (秒)
    /// </summary>
    private const double UPDATE_INTERVAL_SECONDS = 1.0;

    private NetworkTopListManager()
    {
        // 初始化
    }

    /// <summary>
    /// 更新所有进程的网络统计
    /// </summary>
    public void UpdateAllProcessStats()
    {
        _lock.EnterWriteLock();
        try
        {
            var snapshot = GlobalNetworkMonitor.Instance.GetSnapshot();
            var currentTime = DateTime.Now;

            // 更新每个应用程序的统计信息
            foreach (var app in snapshot.Applications)
            {
                UpdateProcessStats(app, currentTime);
            }

            // 清理不活跃的进程统计
            CleanupInactiveProcesses(currentTime);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 更新单个进程的统计信息
    /// </summary>
    private void UpdateProcessStats(ApplicationSnapshot app, DateTime currentTime)
    {
        var processId = app.ProcessId;

        // 获取当前的总流量
        var currentTotalSent = app.TotalBytesSent;
        var currentTotalReceived = app.TotalBytesReceived;

        // 检查是否有之前的数据用于计算速度
        if (_lastTrafficData.TryGetValue(processId, out var lastTraffic) &&
            _lastCalculationTime.TryGetValue(processId, out var lastTime))
        {
            var timeDiff = (currentTime - lastTime).TotalSeconds;

            if (timeDiff >= UPDATE_INTERVAL_SECONDS)
            {
                // 计算速度
                var uploadSpeed = Math.Max(0, (currentTotalSent - lastTraffic.TotalSent) / timeDiff);
                var downloadSpeed = Math.Max(0, (currentTotalReceived - lastTraffic.TotalReceived) / timeDiff);

                // 更新或创建统计信息
                _processStats.AddOrUpdate(processId,
                    new ProcessNetworkStats
                    {
                        ProcessId = processId,
                        ProcessName = GetProcessName(processId),
                        ApplicationInfo = GetApplicationInfo(app),
                        CurrentUploadSpeed = uploadSpeed,
                        CurrentDownloadSpeed = downloadSpeed,
                        LastUpdateTime = currentTime,
                        ActiveConnectionCount = app.ActiveConnections
                    },
                    (key, existing) =>
                    {
                        existing.CurrentUploadSpeed = uploadSpeed;
                        existing.CurrentDownloadSpeed = downloadSpeed;
                        existing.LastUpdateTime = currentTime;
                        existing.ActiveConnectionCount = app.ActiveConnections;
                        existing.ApplicationInfo = GetApplicationInfo(app);
                        return existing;
                    });

                // 更新缓存数据
                _lastTrafficData[processId] = (currentTotalSent, currentTotalReceived);
                _lastCalculationTime[processId] = currentTime;
            }
        }
        else
        {
            // 首次记录，初始化数据
            _lastTrafficData[processId] = (currentTotalSent, currentTotalReceived);
            _lastCalculationTime[processId] = currentTime;

            // 创建初始统计信息（速度为0）
            _processStats[processId] = new ProcessNetworkStats
            {
                ProcessId = processId,
                ProcessName = GetProcessName(processId),
                ApplicationInfo = GetApplicationInfo(app),
                CurrentUploadSpeed = 0,
                CurrentDownloadSpeed = 0,
                LastUpdateTime = currentTime,
                ActiveConnectionCount = app.ActiveConnections
            };
        }
    }

    /// <summary>
    /// 获取应用程序信息
    /// </summary>
    private ApplicationInfo? GetApplicationInfo(ApplicationSnapshot app)
    {
        if (app.ProgramInfo != null)
        {
            return new ApplicationInfo
            {
                ProcessId = app.ProcessId,
                ProgramInfo = app.ProgramInfo,
                FirstSeenTime = app.FirstSeenTime,
                LastUpdateTime = app.LastActiveTime
            };
        }

        return null;
    }

    /// <summary>
    /// 获取进程名称
    /// </summary>
    private string GetProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return $"Process_{processId}";
        }
    }

    /// <summary>
    /// 清理不活跃的进程统计
    /// </summary>
    private void CleanupInactiveProcesses(DateTime currentTime)
    {
        var inactiveThreshold = TimeSpan.FromMinutes(5); // 5分钟无活动的进程将被清理

        var processesToRemove = _processStats
            .Where(kvp => currentTime - kvp.Value.LastUpdateTime > inactiveThreshold)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var processId in processesToRemove)
        {
            _processStats.TryRemove(processId, out _);
            _lastCalculationTime.TryRemove(processId, out _);
            _lastTrafficData.TryRemove(processId, out _);
        }
    }

    /// <summary>
    /// 获取网速Top榜
    /// </summary>
    /// <param name="count">返回的数量，最多5个</param>
    /// <returns>Top榜信息</returns>
    public NetworkTopListInfo GetTopList(int count = 5)
    {
        count = Math.Min(count, 5); // 限制最多5个

        _lock.EnterReadLock();
        try
        {
            // 先获取有网络活动的进程
            var activeProcesses = _processStats.Values
                .Where(p => p.TotalSpeed > 0)
                .OrderByDescending(p => p.TotalSpeed)
                .Take(count)
                .ToList();

            var topProcesses = new List<ProcessNetworkStats>(activeProcesses);

            // 如果有网络活动的进程不够，用无网络活动的进程填充
            if (topProcesses.Count < count)
            {
                var inactiveProcesses = _processStats.Values
                    .Where(p => p.TotalSpeed == 0)
                    .OrderBy(p => p.ProcessName) // 按进程名称首字母排序
                    .Take(count - topProcesses.Count)
                    .ToList();

                topProcesses.AddRange(inactiveProcesses);
            }

            // 如果还是不够，创建占位符进程
            while (topProcesses.Count < count)
            {
                topProcesses.Add(new ProcessNetworkStats
                {
                    ProcessId = -1,
                    ProcessName = "---",
                    CurrentUploadSpeed = 0,
                    CurrentDownloadSpeed = 0,
                    LastUpdateTime = DateTime.Now,
                    ActiveConnectionCount = 0
                });
            }

            return new NetworkTopListInfo
            {
                TopProcesses = topProcesses,
                UpdateTime = DateTime.Now
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 获取指定进程的统计信息
    /// </summary>
    public ProcessNetworkStats? GetProcessStats(int processId)
    {
        _lock.EnterReadLock();
        try
        {
            _processStats.TryGetValue(processId, out var stats);
            return stats;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 重置所有统计数据
    /// </summary>
    public void Reset()
    {
        _lock.EnterWriteLock();
        try
        {
            _processStats.Clear();
            _lastCalculationTime.Clear();
            _lastTrafficData.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}