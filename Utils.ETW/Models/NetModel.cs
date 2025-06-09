using System.Net;

namespace Utils.ETW.Models;

// 网络事件数据结构定义
public class NetworkEventData
{
    /// <summary>
    /// 记录产生时间
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 事件类型
    /// </summary>
    public string EventType { get; set; }

    /// <summary>
    /// 进程 ID 
    /// </summary>
    public int ProcessId { get; set; }

    public int ThreadId { get; set; }
    public string ProcessName { get; set; }
    public IPAddress SourceIp { get; set; }
    public IPAddress DestinationIp { get; set; }

    /// <summary>
    /// 源端口
    /// </summary>
    public int SourcePort { get; set; }

    /// <summary>
    /// 目标端口
    /// </summary>
    public int DestinationPort { get; set; }

    public string Protocol { get; set; }
    public int DataLength { get; set; }
    public TrafficDirection Direction { get; set; } // Inbound/Outbound
    public Dictionary<string, object> AdditionalProperties { get; set; }

    public NetworkEventData()
    {
        AdditionalProperties = new Dictionary<string, object>();
    }
}

public class TcpConnectionSession : NetworkEventData
{
    public long ConnectionId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 发送的字节数
    /// </summary>
    public long BytesSent { get; set; }

    public long BytesReceived { get; set; }

    /// <summary>
    /// 持续时间
    /// </summary>
    public TimeSpan Duration => (EndTime ?? DateTime.Now) - StartTime;

    public List<string> Events { get; set; } = new List<string>();
}

// TCP连接事件数据
public class TcpConnectionEventData : NetworkEventData
{
    /// <summary>
    /// 连接状态
    /// </summary>
    public string ConnectionState { get; set; } // ESTABLISHED, CLOSED, LISTEN等
    public long ConnectionId { get; set; }
    /// <summary>
    /// 顺序
    /// </summary>
    public int SequenceNumber { get; set; }
    public int AcknowledgmentNumber { get; set; }
    public int WindowSize { get; set; }
    public string TcpFlags { get; set; }
}

// UDP数据包事件数据
public class UdpPacketEventData : NetworkEventData
{
    public int UdpLength { get; set; }
    public int Checksum { get; set; }
}

// DNS查询事件数据
public class DnsEventData : NetworkEventData
{
    public string QueryName { get; set; }
    public string QueryType { get; set; }
    public string ResponseCode { get; set; }
    public List<string> ResolvedAddresses { get; set; }
    public int QueryId { get; set; }

    public DnsEventData()
    {
        ResolvedAddresses = new List<string>();
    }
}

// HTTP事件数据
public class HttpEventData : NetworkEventData
{
    public string HttpMethod { get; set; }
    public string Url { get; set; }
    public int StatusCode { get; set; }
    public string UserAgent { get; set; }
    public string Host { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public long ContentLength { get; set; }

    public HttpEventData()
    {
        Headers = new Dictionary<string, string>();
    }
}

// 网络接口事件数据
public class NetworkInterfaceEventData : NetworkEventData
{
    public string InterfaceName { get; set; }
    public string InterfaceType { get; set; }
    public string MacAddress { get; set; }
    public string InterfaceState { get; set; } // Up/Down
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public long PacketsSent { get; set; }
    public long PacketsReceived { get; set; }
}

// 网络错误事件数据
public class NetworkErrorEventData : NetworkEventData
{
    public int ErrorCode { get; set; }
    public string ErrorDescription { get; set; }
    public string ErrorCategory { get; set; }
}
