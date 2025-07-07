using System.Net;
using Infrastructure.Models;
using Utils.ETW.Models;

namespace Utils.ETW.Etw;

/// <summary>
/// 应用程序信息
/// </summary>
public class ApplicationInfo
{
    public int ProcessId { get; set; }
    public ProgramInfo ProgramInfo { get; set; }
    public DateTime FirstSeenTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
}

/// <summary>
/// 进程网络信息
/// </summary>
public class ProcessNetworkInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; }
    public DateTime FirstSeenTime { get; set; }
    public DateTime LastActiveTime { get; set; }
    public List<string> ActiveConnections { get; set; } = new List<string>();
}

/// <summary>
/// 进程信息
/// </summary>
public class ProcessType
{
    /// <summary>
    /// 进程名称
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 进程ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// 启动时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 是否退出
    /// </summary>
    public bool HasExited { get; set; }

    /// <summary>
    /// 退出时间
    /// </summary>
    public DateTime? ExitTime { get; set; }

    /// <summary>
    /// 退出代码
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    /// 占用内存
    /// </summary>
    public long UseMemory { get; set; }

    /// <summary>
    /// 线程数
    /// </summary>
    public int ThreadCount { get; set; }

    /// <summary>
    /// 主模块路径
    /// </summary>
    public string? MainModulePath { get; set; }

    /// <summary>
    /// 启动文件名
    /// </summary>
    public string? MainModuleName { get; set; }

    /// <summary>
    /// 上行总量
    /// </summary>
    public long TotalUploaded { get; set; }

    /// <summary>
    /// 下行总量
    /// </summary>
    public long TotalDownloaded { get; set; }

    /// <summary>
    /// 上行速度
    /// </summary>
    public double UploadSpeed { get; set; }

    /// <summary>
    /// 下行速度
    /// </summary>
    public double DownloadSpeed { get; set; }

    /// <summary>
    /// 所有连接
    /// </summary>
    public List<ConnectionInfo> Connections { get; set; } = new List<ConnectionInfo>();
}

/// <summary>
/// 连接详细信息
/// </summary>
public class ConnectionInfo
{
    public string ConnectionKey { get; set; }
    public int ProcessId { get; set; }
    public ProtocolType Protocol { get; set; }

    /// <summary>
    /// 本地 终结点
    /// </summary>
    public IPEndPoint LocalEndpoint { get; set; }

    /// <summary>
    /// 目标 终结点
    /// </summary>
    public IPEndPoint RemoteEndpoint { get; set; }

    public ConnectionState State { get; set; }
    public TrafficDirection Direction { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime LastActiveTime { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public double CurrentSendSpeed { get; set; } // 字节/秒
    public double CurrentReceiveSpeed { get; set; } // 字节/秒
    public bool IsActive { get; set; }

    // 速率计算辅助字段
    public long LastBytesSent { get; set; }
    public long LastBytesReceived { get; set; }
    public DateTime LastSpeedCalculationTime { get; set; }

    /// <summary>
    /// 重置次数统计
    /// </summary>
    public int ResetCount { get; set; }

    /// <summary>
    /// 累积发送总量（跨重置）
    /// </summary>
    public long TotalBytesSentAccumulated { get; set; }

    /// <summary>
    /// 累积接收总量（跨重置）
    /// </summary>
    public long TotalBytesReceivedAccumulated { get; set; }

    /// <summary>
    /// 连接类型（持久/临时）
    /// </summary>
    public ConnectionType ConnectionType { get; set; }
}

public enum ConnectionType
{
    Unknown,
    Persistent, // 长连接
    Ephemeral, // 短连接
    Multicast, // 多播
    Loopback // 本地回环
}

/// <summary>
/// DNS解析信息
/// </summary>
public class DnsResolveInfo
{
    public string IpAddress { get; set; }
    public string DomainName { get; set; }
    public string QueryType { get; set; }
    public DateTime ResolveTime { get; set; }
    public DateTime LastUsedTime { get; set; }
    public List<string> AlternativeDomains { get; set; } = new List<string>();
}

/// <summary>
/// 端口流量信息
/// </summary>
public class PortTrafficInfo
{
    public int Port { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public long TotalBytes => BytesSent + BytesReceived;
    public DateTime LastUpdateTime { get; set; }
}

/// <summary>
/// IP流量信息
/// </summary>
public class IpTrafficInfo
{
    public string IpAddress { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public long TotalBytes => BytesSent + BytesReceived;
    public DateTime LastUpdateTime { get; set; }
}

/// <summary>
/// 网络监控数据快照
/// </summary>
public class NetworkMonitorSnapshot
{
    public DateTime SnapshotTime { get; set; }
    public List<ApplicationSnapshot> Applications { get; set; }
    public GlobalNetworkStats GlobalStats { get; set; }
}

/// <summary>
/// 应用程序快照
/// </summary>
public class ApplicationSnapshot
{
    public int ProcessId { get; set; }
    public ProgramInfo? ProgramInfo { get; set; }
    public DateTime FirstSeenTime { get; set; }
    public DateTime LastActiveTime { get; set; }
    public List<ConnectionSnapshot> Connections { get; set; }

    // 统计信息
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public long TotalBytesSent { get; set; }
    public long TotalBytesReceived { get; set; }
    public double TotalSendSpeed { get; set; }
    public double TotalReceiveSpeed { get; set; }
}

/// <summary>
/// 连接快照
/// </summary>
public class ConnectionSnapshot
{
    public string ConnectionKey { get; set; }
    public string Protocol { get; set; }
    public string LocalIp { get; set; }
    public int LocalPort { get; set; }
    public string RemoteIp { get; set; }
    public int RemotePort { get; set; }
    public string RemoteDomain { get; set; }
    public string State { get; set; }

    /// <summary>
    /// 方向
    /// </summary>
    public string Direction { get; set; }

    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public double CurrentSendSpeed { get; set; }
    public double CurrentReceiveSpeed { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// 全局网络统计
/// </summary>
public class GlobalNetworkStats
{
    /// <summary>
    /// 总软件数量
    /// </summary>
    public int TotalApplications { get; set; }

    /// <summary>
    /// 总连接数量
    /// </summary>
    public int TotalConnections { get; set; }

    /// <summary>
    /// 活跃连接数
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// 端口流量统计
    /// </summary>
    public List<PortTrafficInfo> PortTraffic { get; set; }

    /// <summary>
    /// IP 流量统计
    /// </summary>
    public List<IpTrafficInfo> IpTraffic { get; set; }
}

/// <summary>
/// 进程网络详情
/// </summary>
public class ProcessNetworkDetails
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; }
    public ApplicationInfo ApplicationInfo { get; set; }
    public List<ConnectionInfo> Connections { get; set; }
}