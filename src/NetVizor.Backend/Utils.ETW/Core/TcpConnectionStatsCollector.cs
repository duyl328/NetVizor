using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Utils.ETW.Native;
using Common.Logger;

namespace Utils.ETW.Core;

/// <summary>
/// TCP连接统计信息收集器
/// 负责获取每个TCP连接的详细流量统计数据
/// </summary>
public class TcpConnectionStatsCollector
{
    /// <summary>
    /// TCP连接的流量统计数据
    /// </summary>
    public class TcpConnectionStats
    {
        public string ConnectionKey { get; set; } = "";
        public uint ProcessId { get; set; }
        public string LocalEndpoint { get; set; } = "";
        public string RemoteEndpoint { get; set; } = "";

        // 原始累积数据
        public ulong TotalBytesIn { get; set; } // 总接收字节数
        public ulong TotalBytesOut { get; set; } // 总发送字节数
        public ulong TotalSegmentsIn { get; set; } // 总接收段数
        public ulong TotalSegmentsOut { get; set; } // 总发送段数

        // 时间戳
        public DateTime LastUpdate { get; set; }

        // 计算出的速率（需要通过两次采样计算）
        public double BytesInPerSecond { get; set; } // 接收速率 bytes/s
        public double BytesOutPerSecond { get; set; } // 发送速率 bytes/s

        // 统计是否可用
        public bool StatsAvailable { get; set; }
        public string ErrorMessage { get; set; } = "";

        public override string ToString()
        {
            return $"PID:{ProcessId} {LocalEndpoint}->{RemoteEndpoint} " +
                   $"In:{FormatBytes(TotalBytesIn)} Out:{FormatBytes(TotalBytesOut)} " +
                   $"Speed:↓{FormatSpeed(BytesInPerSecond)} ↑{FormatSpeed(BytesOutPerSecond)}";
        }

        private static string FormatBytes(ulong bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024 * 1024):F1}GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024):F1}MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1}KB";
            return $"{bytes}B";
        }

        private static string FormatSpeed(double bytesPerSec)
        {
            if (bytesPerSec >= 1024 * 1024 * 1024)
                return $"{bytesPerSec / (1024.0 * 1024 * 1024):F1}GB/s";
            if (bytesPerSec >= 1024 * 1024)
                return $"{bytesPerSec / (1024.0 * 1024):F1}MB/s";
            if (bytesPerSec >= 1024)
                return $"{bytesPerSec / 1024.0:F1}KB/s";
            return $"{bytesPerSec:F0}B/s";
        }
    }

    /// <summary>
    /// 尝试启用TCP连接的统计信息收集
    /// 注意：这个功能需要管理员权限，且不是所有连接都支持
    /// </summary>
    /// <param name="connection">TCP连接信息</param>
    /// <returns>是否成功启用</returns>
    public static bool EnableConnectionStats(TcpConnectionEnumerator.TcpConnectionInfo connection)
    {
        try
        {
            var tcpRow = connection.GetTcpRow();

            // 创建启用统计收集的设置
            var enableStats = new TcpConnectionAPI.TCP_ESTATS_DATA_RW_v0
            {
                EnableCollection = TcpConnectionAPI.TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled
            };

            IntPtr settingsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<TcpConnectionAPI.TCP_ESTATS_DATA_RW_v0>());
            try
            {
                Marshal.StructureToPtr(enableStats, settingsPtr, false);

                int result = TcpConnectionAPI.SetPerTcpConnectionEStats(
                    ref tcpRow,
                    TcpConnectionAPI.TCP_ESTATS_TYPE.TcpConnectionEstatsData,
                    settingsPtr,
                    0, // RwVersion
                    (uint)Marshal.SizeOf<TcpConnectionAPI.TCP_ESTATS_DATA_RW_v0>(),
                    0); // Offset

                if (result == TcpConnectionAPI.NO_ERROR)
                {
                    Log.Debug($"成功启用连接统计: {connection.ConnectionKey}");
                    return true;
                }
                else
                {
                    Log.Debug($"启用连接统计失败: {connection.ConnectionKey}, 错误代码: {result}");
                    return false;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(settingsPtr);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"启用连接统计异常: {connection.ConnectionKey}, {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取TCP连接的统计信息
    /// </summary>
    /// <param name="connection">TCP连接信息</param>
    /// <returns>统计数据，如果获取失败则返回null</returns>
    public static TcpConnectionStats? GetConnectionStats(TcpConnectionEnumerator.TcpConnectionInfo connection)
    {
        try
        {
            var tcpRow = connection.GetTcpRow();
            var stats = new TcpConnectionStats
            {
                ConnectionKey = connection.ConnectionKey,
                ProcessId = connection.ProcessId,
                LocalEndpoint = $"{connection.LocalAddress}:{connection.LocalPort}",
                RemoteEndpoint = $"{connection.RemoteAddress}:{connection.RemotePort}",
                LastUpdate = DateTime.Now
            };

            // 分配内存用于接收统计数据
            IntPtr dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<TcpConnectionAPI.TCP_ESTATS_DATA_ROD_v0>());
            try
            {
                int result = TcpConnectionAPI.GetPerTcpConnectionEStats(
                    ref tcpRow,
                    TcpConnectionAPI.TCP_ESTATS_TYPE.TcpConnectionEstatsData,
                    IntPtr.Zero, // Rw - 不需要
                    0, // RwVersion
                    0, // RwSize
                    IntPtr.Zero, // Ros - 不需要
                    0, // RosVersion  
                    0, // RosSize
                    dataPtr, // Rod - 我们要的数据
                    0, // RodVersion
                    (uint)Marshal.SizeOf<TcpConnectionAPI.TCP_ESTATS_DATA_ROD_v0>()); // RodSize

                if (result == TcpConnectionAPI.NO_ERROR)
                {
                    var data = Marshal.PtrToStructure<TcpConnectionAPI.TCP_ESTATS_DATA_ROD_v0>(dataPtr);

                    stats.TotalBytesIn = data.DataBytesIn;
                    stats.TotalBytesOut = data.DataBytesOut;
                    stats.TotalSegmentsIn = data.DataSegsIn;
                    stats.TotalSegmentsOut = data.DataSegsOut;
                    stats.StatsAvailable = true;

                    Log.Debug($"获取连接统计成功: {stats}");
                    return stats;
                }
                else
                {
                    // 很多连接可能不支持详细统计，这是正常的
                    stats.ErrorMessage = $"获取统计失败，错误代码: {result}";
                    stats.StatsAvailable = false;

                    Log.Debug($"连接统计不可用: {connection.ConnectionKey}, 错误: {result}");
                    return stats;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(dataPtr);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"获取连接统计异常: {connection.ConnectionKey}, {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 批量获取多个连接的统计信息
    /// </summary>
    /// <param name="connections">连接列表</param>
    /// <returns>统计信息列表</returns>
    public static List<TcpConnectionStats> GetConnectionStatsBatch(
        List<TcpConnectionEnumerator.TcpConnectionInfo> connections)
    {
        var statsList = new List<TcpConnectionStats>();
        int successCount = 0;
        int failureCount = 0;

        foreach (var connection in connections)
        {
            var stats = GetConnectionStats(connection);
            if (stats != null)
            {
                statsList.Add(stats);
                if (stats.StatsAvailable)
                    successCount++;
                else
                    failureCount++;
            }
            else
            {
                failureCount++;
            }
        }

        Log.Info($"批量获取连接统计完成: 总计={connections.Count}, 成功={successCount}, 失败={failureCount}");
        return statsList;
    }

    /// <summary>
    /// 尝试为所有连接启用统计收集
    /// 注意：这可能会失败很多次，因为不是所有连接都支持详细统计
    /// </summary>
    /// <param name="connections">连接列表</param>
    /// <returns>成功启用统计的连接数量</returns>
    public static int EnableStatsForAllConnections(List<TcpConnectionEnumerator.TcpConnectionInfo> connections)
    {
        int enabledCount = 0;

        foreach (var connection in connections)
        {
            if (EnableConnectionStats(connection))
            {
                enabledCount++;
            }
        }

        Log.Info($"尝试启用统计收集: 总连接={connections.Count}, 成功启用={enabledCount}");
        return enabledCount;
    }

    /// <summary>
    /// 计算两次采样之间的速率
    /// </summary>
    /// <param name="previousStats">上一次的统计数据</param>
    /// <param name="currentStats">当前的统计数据</param>
    /// <returns>包含速率信息的当前统计数据</returns>
    public static TcpConnectionStats CalculateSpeed(TcpConnectionStats previousStats, TcpConnectionStats currentStats)
    {
        if (previousStats == null || currentStats == null)
        {
            return currentStats;
        }

        var timeDiff = (currentStats.LastUpdate - previousStats.LastUpdate).TotalSeconds;
        if (timeDiff <= 0 || timeDiff > 60) // 避免异常的时间差
        {
            return currentStats;
        }

        // 计算字节差值
        var bytesInDiff = currentStats.TotalBytesIn >= previousStats.TotalBytesIn
            ? currentStats.TotalBytesIn - previousStats.TotalBytesIn
            : 0; // 处理连接重置的情况

        var bytesOutDiff = currentStats.TotalBytesOut >= previousStats.TotalBytesOut
            ? currentStats.TotalBytesOut - previousStats.TotalBytesOut
            : 0;

        // 计算速率
        currentStats.BytesInPerSecond = bytesInDiff / timeDiff;
        currentStats.BytesOutPerSecond = bytesOutDiff / timeDiff;

        return currentStats;
    }
}