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

            switch (udpEvent.EventType)
            {
                // 数据发送
                case "UdpSend":
                    UdpSend(udpEvent);
                    break;
                // 数据接收
                case "UdpReceive":
                    UdpReceive(udpEvent);
                    break;
            }


            // 根据端口进行专门处理
            // switch (udpEvent.DestinationPort)
            // {
            //     case 53: // DNS
            //         ProcessDnsPacket(udpEvent);
            //         break;
            //     case 123: // NTP
            //         ProcessNtpPacket(udpEvent);
            //         break;
            //     case 67:
            //     case 68: // DHCP
            //         ProcessDhcpPacket(udpEvent);
            //         break;
            //     default:
            //         ProcessGenericUdpPacket(udpEvent);
            //         break;
            // }
        };
    }

    /// <summary>
    /// 数据接收
    /// </summary>
    /// <param name="eventData"></param>
    private void UdpSend(UdpPacketEventData eventData)
    {
        var key = NetworkModel.GetKey(
            eventData.SourceIp,
            eventData.SourcePort,
            eventData.DestinationIp,
            eventData.DestinationPort,
            eventData.ProcessId,
            ProtocolType.TCP,
            eventData.Timestamp
        );
        var (tryRemove, networkModel) = NetworkInfoManger.Instance.RemoveTcpCache(key);

        if (eventData.DataLength < 0)
        {
            throw new Exception("数据长度不可能为 0 !!");
        }
        // 统计端口流量
        NetworkInfoManger.Instance.PortTrafficSent.TryAdd(eventData.DestinationPort, 0);
        NetworkInfoManger.Instance.PortTrafficSent[eventData.DestinationPort] += (ulong)eventData.DataLength;

        NetworkInfoManger.Instance.SourceTrafficSent.TryAdd(eventData.SourceIp, 0);
        NetworkInfoManger.Instance.SourceTrafficSent[eventData.SourceIp] += (ulong)eventData.DataLength;

        
        
        // 数据处理【UDP 数据整体处理】
        var lastSeenTime = DateTime.Now;

        var netModel = new NetworkModel
        {
            ConnectType = ProtocolType.UDP,
            ProcessId = eventData.ProcessId,
            ThreadId = eventData.ThreadId,
            ProcessName = eventData.ProcessName,
            SourceIp = eventData.SourceIp,
            DestinationIp = eventData.DestinationIp,
            LastSeenTime = lastSeenTime,
            BytesSent = 0,
            BytesReceived = 0,
            State = ConnectionState.Connecting,
            SourcePort = eventData.SourcePort,
            DestinationPort = eventData.DestinationPort,
            StartTime = lastSeenTime,
            Direction = eventData.Direction,
            RecordTime = lastSeenTime,
            IsPartialConnection = true,
        };

        string str = $"连接建立: {networkModel.DestinationIp}:{networkModel.DestinationPort} ," +
                     $"地址: {networkModel.SourceIp}:{networkModel.SourcePort}";
        NetworkInfoManger.Instance.RecordEvent(str);

            
        // UDP处理主要关注数据包本身，而不是连接状态
        UpdateTrafficStats(eventData);
    }


    /// <summary>
    /// 数据接收
    /// </summary>
    private void UdpReceive(UdpPacketEventData eventData)
    {
        
    }

    private void UpdateTrafficStats(UdpPacketEventData udpEvent)
    {
        // 统计端口流量
        portTraffic.TryAdd(udpEvent.DestinationPort, 0);
        portTraffic[udpEvent.DestinationPort] += udpEvent.DataLength;

        // 统计源IP流量
        sourceTraffic.TryAdd(udpEvent.SourceIp, 0);
        sourceTraffic[udpEvent.SourceIp] += udpEvent.DataLength;
    }

    private void ProcessDnsPacket(UdpPacketEventData udpEvent)
    {
    }

    private void ProcessNtpPacket(UdpPacketEventData udpEvent)
    {
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
