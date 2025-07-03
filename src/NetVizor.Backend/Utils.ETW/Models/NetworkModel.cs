using System.Net;

namespace Utils.ETW.Models;

/// <summary>
/// 网络连接模型
/// </summary>
public class NetworkModel
{
    // RemoteCountry	地理位置归属地，供安全分析

    /// <summary>
    /// 进程 ID 
    /// </summary>
    public required int ProcessId { get; set; }

    /// <summary>
    /// 线程 ID
    /// </summary>
    public required int ThreadId { get; set; }

    /// <summary>
    /// 进程名称
    /// </summary>
    public required string ProcessName { get; set; }

    /// <summary>
    /// 源地址
    /// </summary>
    public required IPAddress SourceIp { get; set; }

    public required IPAddress DestinationIp { get; set; }

    /// <summary>
    /// 最新活动时间
    /// </summary>
    public required DateTime LastSeenTime { get; set; }

    /// <summary>
    /// 记录产生时间
    /// </summary>
    public required DateTime RecordTime { get; set; }

    /// <summary>
    /// 发送的数据量
    /// </summary>
    public long BytesSent { get; set; }

    public long BytesReceived { get; set; }

    /// <summary>
    /// 是否是增量数据
    /// </summary>
    public bool IsIncrementalData { get; set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public required ConnectionState State { get; set; } // 如 ESTABLISHED, CLOSED, TIME_WAIT, 或 UDP/Unknown

    /// <summary>
    /// 源端口
    /// </summary>
    public required int SourcePort { get; set; }

    /// <summary>
    /// 目标端口
    /// </summary>
    public required int DestinationPort { get; set; }

    /// <summary>
    /// 连接类型
    /// </summary>
    public required ProtocolType ConnectType { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 进站或是出站
    /// </summary>
    public TrafficDirection Direction { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否是完整会话【如果在软件打开前会话已经创建则不认为是完整会话】
    /// </summary>
    public required bool IsPartialConnection { get; set; }


    /// <summary>
    /// 会话持续时间
    /// </summary>
    public TimeSpan Duration => (EndTime ?? DateTime.Now) - (StartTime ?? RecordTime);

    // 近 1 分钟平均网速   
    // 近 1 分钟流量统计   
    // 近 10 分钟流量统计   

    public string GetKey()
    {
        return GetKey(this);
    }

    /// <summary>
    /// 获取唯一 ID
    /// </summary>
    /// <returns></returns>
    public static string GetKey(NetworkModel networkModel)
    {
        return GetKey(networkModel.SourceIp,
            networkModel.SourcePort,
            networkModel.DestinationIp,
            networkModel.DestinationPort,
            networkModel.ProcessId,
            networkModel.ConnectType);
    }

    /// <summary>
    /// 获取唯一 ID
    /// </summary>
    /// <param name="sourceIp">源 Ip</param>
    /// <param name="sourcePort">源端口</param>
    /// <param name="destinationIp">目标 Ip</param>
    /// <param name="destinationPort">目标端口</param>
    /// <param name="processId">进程 ID</param>
    /// <param name="connectType">连接类型</param>
    /// <param name="recordTime">记录时间</param>
    /// <returns></returns>
    public static string GetKey(
        IPAddress sourceIp,
        int sourcePort,
        IPAddress destinationIp,
        int destinationPort,
        int processId,
        ProtocolType connectType)
    {
        var key =
            $"{sourceIp}:{sourcePort}-{destinationIp}:" +
            $"{destinationPort}-{processId}-{connectType}";
        return key;
    }
}

public enum ConnectionState
{
    /// <summary>
    /// 正在尝试连接
    /// </summary>
    Connecting,
    Connected, // 当前连接活跃（发送/接收中）
    Disconnected, // 已断开或未活跃
    Listening, // 监听状态（如本地服务器）
    Unknown // 无法判断状态
}

public enum ProtocolType
{
    TCP,
    UDP,
    ICMP, // 可选
    Unknown
}

/// <summary>
/// 进出站类型
/// </summary>
public enum TrafficDirection
{
    Inbound,
    Outbound,
    // Unknown
}