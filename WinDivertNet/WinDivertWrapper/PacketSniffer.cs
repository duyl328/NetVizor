namespace WinDivertNet.WinDivertWrapper;

using System;
using System.Runtime.InteropServices;

public static class PacketSniffer
{
    public static void Start()
    {
        Console.WriteLine($"进入 12313 ");
        var handle = WinDivert.Open("true", WinDivert.WINDIVERT_LAYER_NETWORK, 0, WinDivert.WINDIVERT_FLAG_SNIFF);

        byte[] buffer = new byte[65535];
        WinDivert.WINDIVERT_ADDRESS addr = default;
        uint readLen = 0;

        while (true)
        {
            try
            {
                if (WinDivert.Recv(handle, buffer, (uint)buffer.Length, ref addr, ref readLen))
                {
                    ParsePacketAndLog(buffer, readLen, addr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收数据包时出错: {ex.Message}");
            }
        }

void ParsePacketAndLog(byte[] buffer, uint length, WinDivert.WINDIVERT_ADDRESS addr)
{
    if (buffer == null || length == 0)
        return;

    try
    {
        // IPv4 数据包的最小长度为 20 字节
        if (length >= 20)
        {
            // 检查版本 (IPv4 = 4, 位于第一个字节的高 4 位)
            int version = (buffer[0] >> 4) & 0xF;
            
            if (version == 4) // IPv4
            {
                int headerLength = (buffer[0] & 0xF) * 4; // IHL * 4 = 字节数
                byte protocol = buffer[9]; // 协议字段
                
                // 源 IP 和目标 IP
                uint srcIp = (uint)((buffer[12]) | (buffer[13] << 8) | (buffer[14] << 16) | (buffer[15] << 24));
                uint dstIp = (uint)((buffer[16]) | (buffer[17] << 8) | (buffer[18] << 16) | (buffer[19] << 24));
                
                string srcIpStr = $"{buffer[12]}.{buffer[13]}.{buffer[14]}.{buffer[15]}";
                string dstIpStr = $"{buffer[16]}.{buffer[17]}.{buffer[18]}.{buffer[19]}";
                
                Console.WriteLine($"IPv4: Src={srcIpStr}, Dst={dstIpStr}, Protocol={protocol}");
                
                // 如果有足够的数据，解析 TCP 或 UDP
                if (length >= (uint)(headerLength + 4) && (protocol == 6 || protocol == 17))
                {
                    // 获取端口 (TCP 和 UDP 的前 4 字节都是源端口和目标端口)
                    int offset = headerLength;
                    ushort srcPort = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
                    ushort dstPort = (ushort)((buffer[offset + 2] << 8) | buffer[offset + 3]);
                    
                    if (protocol == 6) // TCP
                    {
                        Console.WriteLine($"TCP: SrcPort={srcPort}, DstPort={dstPort}");
                    }
                    else if (protocol == 17) // UDP
                    {
                        Console.WriteLine($"UDP: SrcPort={srcPort}, DstPort={dstPort}");
                    }
                }
            }
            else if (version == 6 && length >= 40) // IPv6 (头部固定 40 字节)
            {
                // IPv6 解析代码...
                // 这里略过，需要时可以添加
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"解析数据包时出错: {ex.Message}");
    }
}    }
}
