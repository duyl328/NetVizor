using System.Collections.Concurrent;
using Utils.ETW.EtwTracker;
using Utils.ETW.Models;

namespace Utils.ETW.Core;

public class DnsTracker : INetTracker
{
    public void SetupEtwHandlers(EtwNetworkCapture networkCapture)
    {
        networkCapture.OnDnsEvent += dnsEvent =>
        {
            Console.WriteLine($"[DNS] 查询: {dnsEvent.QueryName} ({dnsEvent.QueryType})");
        };
    }
}

public class HttpTracker : INetTracker
{
    public void SetupEtwHandlers(EtwNetworkCapture networkCapture)
    {
        networkCapture.OnHttpEvent += httpEvent =>
        {
            Console.WriteLine($"[HTTP] {httpEvent.HttpMethod} {httpEvent.Url}");
        };
    }
}

public class NetworkProfileTracker : INetTracker
{
    public void SetupEtwHandlers(EtwNetworkCapture networkCapture)
    {
        networkCapture.OnNetworkInterfaceEvent += interfaceEvent =>
        {
            Console.WriteLine($"[Interface] {interfaceEvent.InterfaceName}: {interfaceEvent.InterfaceState}");
        };
    }
}

public class NetworkTracker : INetTracker
{
    /// <summary>
    /// 捕获的事件量
    /// </summary>
    private List<NetworkEventData> capturedEvents = new List<NetworkEventData>();

    public void SetupEtwHandlers(EtwNetworkCapture networkCapture)
    {
        networkCapture.OnNetworkEvent += networkEvent => { capturedEvents.Add(networkEvent); };
    }
}


public class EtwNetworkManger
{
    private UdpTracker udpTracker = new UdpTracker();
    private DnsTracker dnsTracker = new DnsTracker();
    private HttpTracker httpTracker = new HttpTracker();
    private NetworkProfileTracker networkProfileTracker = new NetworkProfileTracker();
    private NetworkTracker networkTracker = new NetworkTracker();
    private readonly TcpTracker _tcpTracker = new TcpTracker();

    private EtwNetworkCapture networkCapture = new EtwNetworkCapture();

    /// <summary>
    /// 设置信息处理器
    /// </summary>
    public void SetupEtwHandlers()
    {
        udpTracker.SetupEtwHandlers(networkCapture);
        // dnsTracker.SetupEtwHandlers(networkCapture);
        // httpTracker.SetupEtwHandlers(networkCapture);
        // networkProfileTracker.SetupEtwHandlers(networkCapture);
        // networkTracker.SetupEtwHandlers(networkCapture);
        
        // _tcpTracker.SetupEtwHandlers(networkCapture);
    }

    public void StartCapture()
    {
        networkCapture.StartCapture();
    }
    public void Dispose()
    {
        networkCapture.Dispose();
    }
}
