using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Utils.ETW.Models;

namespace Utils.ETW.Core;


// ETW网络事件处理器
public class EtwNetworkCapture(string sessionName = "NetworkETWSession") : IDisposable
{
    private readonly TraceEventSession _session = new(sessionName);
    private readonly ETWTraceEventSource _source = new(sessionName, TraceEventSourceType.Session);

    /// <summary>
    /// 是否正在监听
    /// </summary>
    private bool _isCapturing = false;

    // 事件处理委托
    public delegate void NetworkEventHandler(NetworkEventData eventData);

    public delegate void TcpConnectionEventHandler(TcpConnectionEventData eventData);

    public delegate void UdpPacketEventHandler(UdpPacketEventData eventData);

    public delegate void DnsEventHandler(DnsEventData eventData);

    public delegate void HttpEventHandler(HttpEventData eventData);

    public delegate void NetworkInterfaceEventHandler(NetworkInterfaceEventData eventData);

    public delegate void NetworkErrorEventHandler(NetworkErrorEventData eventData);

    // 事件处理器
    
    /// <summary>
    /// 通用网络事件处理，所有的网络事件都会进入该函数
    /// </summary>
    public event NetworkEventHandler? OnNetworkEvent;
    public event TcpConnectionEventHandler? OnTcpConnectionEvent;
    public event UdpPacketEventHandler? OnUdpPacketEvent;
    public event DnsEventHandler? OnDnsEvent;
    public event HttpEventHandler? OnHttpEvent;
    public event NetworkInterfaceEventHandler? OnNetworkInterfaceEvent;
    public event NetworkErrorEventHandler? OnNetworkErrorEvent;

    /// <summary>
    /// 开始监听
    /// </summary>
    public void StartCapture()
    {
        if (_isCapturing) return;

        try
        {
            // 启用网络相关的ETW提供程序
            EnableNetworkProviders();

            // 设置事件处理器
            SetupEventHandlers();

            _isCapturing = true;

            // 开始处理事件（这是阻塞调用）
            Task.Run(() => { _source.Process(); });

            Console.WriteLine("ETW网络捕获已启动...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动ETW捕获失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 设置监听的 Providers
    /// </summary>
    private void EnableNetworkProviders()
    {
        // 启用内核网络事件
        _session.EnableKernelProvider(
            KernelTraceEventParser.Keywords.NetworkTCPIP
        );


        // 启用TCP/IP提供程序
        _session.EnableProvider("Microsoft-Windows-TCPIP",
            TraceEventLevel.Informational);

        // 启用Winsock AFD提供程序
        /*
         *哪个进程创建了 socket
        每个 socket 的本地/远程地址、端口
        Socket 错误、阻塞、超时
        发送/接收的字节数
        使用了什么协议（TCP/UDP）
         *
         */
        _session.EnableProvider("Microsoft-Windows-Winsock-AFD",
            TraceEventLevel.Informational);

        // 启用DNS客户端提供程序
        _session.EnableProvider("Microsoft-Windows-DNS-Client",
            TraceEventLevel.Informational);

        // 启用HTTP服务提供程序
        _session.EnableProvider("Microsoft-Windows-HttpService",
            TraceEventLevel.Informational);

        // 启用网络配置文件提供程序
        _session.EnableProvider("Microsoft-Windows-NetworkProfile",
            TraceEventLevel.Informational);

        // 监控 DNS 查询行为
        _session.EnableProvider("Microsoft-Windows-DNS-Client",
            TraceEventLevel.Informational);
    }

    /// <summary>
    /// 设置监听事件
    /// </summary>
    private void SetupEventHandlers()
    {
        var parser = new KernelTraceEventParser(_source);

        // TCP连接事件
        parser.TcpIpConnect += data =>
        {
            var tcpEvent = new TcpConnectionEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "TcpConnect",
                ProcessId = data.ProcessID,
                ThreadId = data.ThreadID,
                ProcessName = data.ProcessName,
                SourceIp = data.saddr,
                DestinationIp = data.daddr,
                SourcePort = data.sport,
                DestinationPort = data.dport,
                Protocol = "TCP",
                ConnectionState = "ESTABLISHED",
                Direction = TrafficDirection.Outbound
            };

            OnTcpConnectionEvent?.Invoke(tcpEvent);
            OnNetworkEvent?.Invoke(tcpEvent);
        };

        // TCP断开连接事件
        parser.TcpIpDisconnect += data =>
        {
            var tcpEvent = new TcpConnectionEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "TcpDisconnect",
                ProcessId = data.ProcessID,
                ThreadId = data.ThreadID,
                ProcessName = data.ProcessName,
                SourceIp = data.saddr,
                DestinationIp = data.daddr,
                SourcePort = data.sport,
                DestinationPort = data.dport,
                Protocol = "TCP",
                ConnectionState = "CLOSED",
                Direction = TrafficDirection.Outbound
            };

            OnTcpConnectionEvent?.Invoke(tcpEvent);
            OnNetworkEvent?.Invoke(tcpEvent);
        };

        // TCP数据发送事件
        parser.TcpIpSend += data =>
        {
            var tcpEvent = new TcpConnectionEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "TcpSend",
                ProcessId = data.ProcessID,
                ThreadId = data.ThreadID,
                ProcessName = data.ProcessName,
                SourceIp = data.saddr,
                DestinationIp = data.daddr,
                SourcePort = data.sport,
                DestinationPort = data.dport,
                Protocol = "TCP",
                DataLength = data.size,
                SequenceNumber = data.seqnum,
                Direction = TrafficDirection.Outbound
            };

            OnTcpConnectionEvent?.Invoke(tcpEvent);
            OnNetworkEvent?.Invoke(tcpEvent);
        };

        // TCP数据接收事件
        parser.TcpIpRecv += data =>
        {
            var tcpEvent = new TcpConnectionEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "TcpReceive",
                ProcessId = data.ProcessID,
                ThreadId = data.ThreadID,
                ProcessName = data.ProcessName,
                SourceIp = data.saddr,
                DestinationIp = data.daddr,
                SourcePort = data.sport,
                DestinationPort = data.dport,
                Protocol = "TCP",
                DataLength = data.size,
                SequenceNumber = data.seqnum,
                Direction = TrafficDirection.Inbound
            };

            OnTcpConnectionEvent?.Invoke(tcpEvent);
            OnNetworkEvent?.Invoke(tcpEvent);
        };

        // UDP发送事件
        parser.UdpIpSend += data =>
        {
            var udpEvent = new UdpPacketEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "UdpSend",
                ProcessId = data.ProcessID,
                ThreadId = data.ThreadID,
                ProcessName = data.ProcessName,
                SourceIp = data.saddr,
                DestinationIp = data.daddr,
                SourcePort = data.sport,
                DestinationPort = data.dport,
                Protocol = "UDP",
                DataLength = data.size,
                UdpLength = data.size,
                Direction = TrafficDirection.Outbound
            };

            OnUdpPacketEvent?.Invoke(udpEvent);
            OnNetworkEvent?.Invoke(udpEvent);
        };

        // UDP接收事件
        parser.UdpIpRecv += data =>
        {
            var udpEvent = new UdpPacketEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "UdpReceive",
                ProcessId = data.ProcessID,
                ThreadId = data.ThreadID,
                ProcessName = data.ProcessName,
                SourceIp = data.saddr,
                DestinationIp = data.daddr,
                SourcePort = data.sport,
                DestinationPort = data.dport,
                Protocol = "UDP",
                DataLength = data.size,
                UdpLength = data.size,
                Direction = TrafficDirection.Inbound
            };

            OnUdpPacketEvent?.Invoke(udpEvent);
            OnNetworkEvent?.Invoke(udpEvent);
        };

        // 通用事件处理器，用于其他网络事件
        _source.Dynamic.All += data =>
        {
            // 处理DNS、HTTP等其他网络事件
            ProcessDynamicEvent(data);
        };
    }

    /// <summary>
    /// 通用事件处理器，用于其他网络事件
    /// </summary>
    /// <param name="data"></param>
    private void ProcessDynamicEvent(TraceEvent data)
    {
        // 根据提供程序名称和事件ID处理不同类型的网络事件
        switch (data.ProviderName)
        {
            case "Microsoft-Windows-DNS-Client":
                ProcessDnsEvent(data);
                break;
            case "Microsoft-Windows-HttpService":
                ProcessHttpEvent(data);
                break;
            case "Microsoft-Windows-NetworkProfile":
                ProcessNetworkInterfaceEvent(data);
                break;
                // 负责 TCP/IP 协议栈事件，如连接、发送、接收、断开、重传等
                // 用于监控 TCP 会话生命周期，是连接追踪的核心
            case "Microsoft-Windows-TCPIP":
                // AFD（Ancillary Function Driver for WinSock）是 Winsock API 的核心内核模块
                // 常用于检测哪一个进程创建了 socket，socket 和 TCP 会话的关联
            case "Microsoft-Windows-Winsock-AFD":
                // 也叫 Kernel-Network
            case "MSNT_SystemTrace":
                break;
            default:
                // 处理其他网络事件
                ProcessGenericNetworkEvent(data);
                break;
        }
    }

    private void ProcessDnsEvent(TraceEvent data)
    {
        try
        {
            var dnsEvent = new DnsEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "DnsQuery",
                ProcessId = data.ProcessID,
                ThreadId = data.ThreadID,
                ProcessName = data.ProcessName,
                Protocol = "DNS"
            };

            // 从事件数据中提取DNS特定信息
            if (data.PayloadNames.Contains("QueryName"))
                dnsEvent.QueryName = data.PayloadStringByName("QueryName");
            if (data.PayloadNames.Contains("QueryType"))
                dnsEvent.QueryType = data.PayloadStringByName("QueryType");

            OnDnsEvent?.Invoke(dnsEvent);
            OnNetworkEvent?.Invoke(dnsEvent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理DNS事件时出错: {ex.Message}");
        }
    }

    private void ProcessHttpEvent(TraceEvent data)
    {
        try
        {
            var httpEvent = new HttpEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "HttpRequest",
                ProcessId = data.ProcessID,
                ThreadId = data.ThreadID,
                ProcessName = data.ProcessName,
                Protocol = "HTTP"
            };

            // 从事件数据中提取HTTP特定信息
            if (data.PayloadNames.Contains("Url"))
                httpEvent.Url = data.PayloadStringByName("Url");
            if (data.PayloadNames.Contains("Method"))
                httpEvent.HttpMethod = data.PayloadStringByName("Method");

            OnHttpEvent?.Invoke(httpEvent);
            OnNetworkEvent?.Invoke(httpEvent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理HTTP事件时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 网络配置发生变化
    /// </summary>
    /// <param name="data"></param>
    private void ProcessNetworkInterfaceEvent(TraceEvent data)
    {
        try
        {
            var interfaceEvent = new NetworkInterfaceEventData
            {
                Timestamp = data.TimeStamp,
                EventType = "NetworkInterface",
                ProcessId = data.ProcessID,
                ThreadId = data.ThreadID,
                ProcessName = data.ProcessName
            };

            // 从事件数据中提取网络接口信息
            if (data.PayloadNames.Contains("InterfaceName"))
                interfaceEvent.InterfaceName = data.PayloadStringByName("InterfaceName");

            OnNetworkInterfaceEvent?.Invoke(interfaceEvent);
            OnNetworkEvent?.Invoke(interfaceEvent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理网络接口事件时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理其他网络事件
    /// </summary>
    /// <param name="data"></param>
    private void ProcessGenericNetworkEvent(TraceEvent data)
    {
        try
        {
            var networkEvent = new NetworkEventData
            {
                Timestamp = data.TimeStamp,
                EventType = data.EventName,
                ProcessId = data.ProcessID,
                ThreadId = data.ThreadID,
                ProcessName = data.ProcessName
            };
            Console.WriteLine($"data.PayloadName :=> {data.ProviderName}");

            // 提取通用网络信息
            foreach (var payloadName in data.PayloadNames)
            {
                try
                {
                    var value = data.PayloadByName(payloadName);
                    networkEvent.AdditionalProperties[payloadName] = value;
                }
                catch
                {
                    // 忽略无法获取的字段
                }
            }

            OnNetworkEvent?.Invoke(networkEvent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理通用网络事件时出错: {ex.Message}");
        }
    }

    public void StopCapture()
    {
        if (!_isCapturing) return;

        try
        {
            _isCapturing = false;
            _session.Stop();
            _source.Dispose();
            Console.WriteLine("ETW网络捕获已停止");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"停止ETW捕获时出错: {ex.Message}");
        }
    }

    public void Dispose()
    {
        StopCapture();
        _session.Dispose();
        _source.Dispose();
    }
}
