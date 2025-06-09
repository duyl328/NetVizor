using System.Net;
using Utils.ETW.EtwTracker;
using Utils.ETW.Models;

namespace Utils.ETW.Core;

public class UdpTracker : INetTracker
{
    private Dictionary<int, int> portTraffic = new();
    private Dictionary<IPAddress, int> sourceTraffic = new();

    public void SetupEtwHandlers(EtwNetworkCapture networkCapture)
    {
        networkCapture.OnUdpPacketEvent += (udpEvent) =>
        {
            Console.WriteLine($"[UDP] {udpEvent.EventType}: {udpEvent.SourceIp}:{udpEvent.SourcePort} -> " +
                              $"{udpEvent.DestinationIp}:{udpEvent.DestinationPort} ({udpEvent.DataLength} bytes)");

            // UDP处理主要关注数据包本身，而不是连接状态
            UpdateTrafficStats(udpEvent);

            // 根据端口进行专门处理
            switch (udpEvent.DestinationPort)
            {
                case 53: // DNS
                    ProcessDnsPacket(udpEvent);
                    break;
                case 123: // NTP
                    ProcessNtpPacket(udpEvent);
                    break;
                case 67:
                case 68: // DHCP
                    ProcessDhcpPacket(udpEvent);
                    break;
                default:
                    ProcessGenericUdpPacket(udpEvent);
                    break;
            }
        };
    }

    private void UpdateTrafficStats(UdpPacketEventData udpEvent)
    {
        // 统计端口流量
        if (!portTraffic.ContainsKey(udpEvent.DestinationPort))
            portTraffic[udpEvent.DestinationPort] = 0;
        portTraffic[udpEvent.DestinationPort] += udpEvent.DataLength;

        // 统计源IP流量
        if (!sourceTraffic.ContainsKey(udpEvent.SourceIp))
            sourceTraffic[udpEvent.SourceIp] = 0;
        sourceTraffic[udpEvent.SourceIp] += udpEvent.DataLength;
    }

    private void ProcessDnsPacket(UdpPacketEventData udpEvent)
    {
        // 连接建立时间
        var lastSeenTime = DateTime.Now;

        var networkModel = new NetworkModel
        {
            ConnectType = ProtocolType.UDP,
            ProcessId = udpEvent.ProcessId,
            ThreadId = udpEvent.ThreadId,
            ProcessName = udpEvent.ProcessName,
            SourceIp = udpEvent.SourceIp,
            DestinationIp = udpEvent.DestinationIp,
            LastSeenTime = lastSeenTime,
            BytesSent = 0,
            BytesReceived = 0,
            State = ConnectionState.Connecting,
            SourcePort = udpEvent.SourcePort,
            DestinationPort = udpEvent.DestinationPort,
            StartTime = lastSeenTime,
            Direction = udpEvent.Direction,
            RecordTime = lastSeenTime,
            IsPartialConnection = true,
        };

        NetworkInfoManger.Instance.SetUdpCache(networkModel);

        string str = $"连接建立: {networkModel.DestinationIp}:{networkModel.DestinationPort} ," +
                     $"地址: {networkModel.SourceIp}:{networkModel.SourcePort}";
        NetworkInfoManger.Instance.RecordEvent(str);
    }

    private void ProcessNtpPacket(UdpPacketEventData udpEvent)
    {
        // 连接建立时间
        var lastSeenTime = DateTime.Now;

        var networkModel = new NetworkModel
        {
            ConnectType = ProtocolType.UDP,
            ProcessId = udpEvent.ProcessId,
            ThreadId = udpEvent.ThreadId,
            ProcessName = udpEvent.ProcessName,
            SourceIp = udpEvent.SourceIp,
            DestinationIp = udpEvent.DestinationIp,
            LastSeenTime = lastSeenTime,
            BytesSent = 0,
            BytesReceived = 0,
            State = ConnectionState.Connecting,
            SourcePort = udpEvent.SourcePort,
            DestinationPort = udpEvent.DestinationPort,
            StartTime = lastSeenTime,
            Direction = udpEvent.Direction,
            RecordTime = lastSeenTime,
            IsPartialConnection = true,
        };

        NetworkInfoManger.Instance.SetUdpCache(networkModel);

        string str = $"连接建立: {networkModel.DestinationIp}:{networkModel.DestinationPort} ," +
                     $"地址: {networkModel.SourceIp}:{networkModel.SourcePort}";
        NetworkInfoManger.Instance.RecordEvent(str);
        
    }

    private void ProcessDhcpPacket(UdpPacketEventData udpEvent)
    {
        Console.WriteLine($"[DHCP] DHCP通信: {udpEvent.SourceIp} <-> {udpEvent.DestinationIp}");
    }

    private void ProcessGenericUdpPacket(UdpPacketEventData udpEvent)
    {
        // 对于未知UDP流量，主要进行安全检查
        if (udpEvent.DataLength > 1024 * 64) // 64KB
        {
            Console.WriteLine($"[UDP警告] 大型UDP数据包: {udpEvent.ProcessName} " +
                              $"发送 {udpEvent.DataLength} 字节到 {udpEvent.DestinationIp}:{udpEvent.DestinationPort}");
        }
    }

    public void PrintTrafficSummary()
    {
        Console.WriteLine("\n=== UDP流量统计 ===");
        Console.WriteLine("端口流量TOP 10:");
        foreach (var item in portTraffic.OrderByDescending(x => x.Value).Take(10))
        {
            Console.WriteLine($"  端口 {item.Key}: {item.Value / 1024}KB");
        }

        Console.WriteLine("\n源IP流量TOP 10:");
        foreach (var item in sourceTraffic.OrderByDescending(x => x.Value).Take(10))
        {
            Console.WriteLine($"  {item.Key}: {item.Value / 1024}KB");
        }
    }
}
