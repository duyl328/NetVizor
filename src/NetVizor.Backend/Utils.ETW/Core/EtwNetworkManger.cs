using System.Collections.Concurrent;
using Utils.ETW.EtwTracker;
using Utils.ETW.Models;

namespace Utils.ETW.Core;

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


    private CancellationTokenSource _udpCleanupTokenSource = new();

    /// <summary>
    /// 设置信息处理器
    /// </summary>
    public void SetupEtwHandlers()
    {
        // UDP
        // udpTracker.SetupEtwHandlers(networkCapture);
        // StartUdpSessionCleanupLoop();

        // TCP
        // _tcpTracker.SetupEtwHandlers(networkCapture);
        
        Console.WriteLine("进入了！！！！！！！！！！");
        // DNS
        dnsTracker.SetupEtwHandlers(networkCapture);

        // httpTracker.SetupEtwHandlers(networkCapture);
        // networkProfileTracker.SetupEtwHandlers(networkCapture);
        // networkTracker.SetupEtwHandlers(networkCapture);
    }

    /// <summary>
    /// 开始定时循环清理
    /// </summary>
    public void StartUdpSessionCleanupLoop()
    {
        Task.Run(async () =>
        {
            while (!_udpCleanupTokenSource.Token.IsCancellationRequested)
            {
                udpTracker.CleanupExpiredUdpSessions();
                // 每10秒清理一次
                await Task.Delay(TimeSpan.FromSeconds(10), _udpCleanupTokenSource.Token);
            }
        }, _udpCleanupTokenSource.Token);
    }

    /// <summary>
    /// 停止清理
    /// </summary>
    public void StopUdpSessionCleanupLoop()
    {
        _udpCleanupTokenSource.Cancel();
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