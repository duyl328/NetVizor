namespace WinDivertNet.WinDivertWrapper;

using System;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

/// <summary>
/// WinDivert 高级封装类
/// </summary>
public class WinDivertController : IDisposable
{
    private IntPtr _handle;
    private bool _disposed = false;

    /// <summary>
    /// 获取句柄是否有效
    /// </summary>
    public bool IsValid => _handle != IntPtr.Zero;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WinDivertController()
    {
        _handle = IntPtr.Zero;
    }

    /// <summary>
    /// 析构函数
    /// </summary>
    ~WinDivertController()
    {
        Dispose(false);
    }

    /// <summary>
    /// 打开 WinDivert 句柄
    /// </summary>
    /// <param name="filter">过滤器表达式</param>
    /// <param name="layer">层级</param>
    /// <param name="priority">优先级</param>
    /// <param name="flags">标志</param>
    /// <returns>是否成功</returns>
    public bool Open(string filter, uint layer = WinDivert.WINDIVERT_LAYER_NETWORK,
        short priority = WinDivert.WINDIVERT_PRIORITY_DEFAULT,
        ulong flags = 0)
    {
        if (IsValid)
        {
            Close();
        }

        _handle = WinDivert.Open(filter, layer, priority, flags);
        return IsValid;
    }

    /// <summary>
    /// 关闭 WinDivert 句柄
    /// </summary>
    public void Close()
    {
        if (IsValid)
        {
            WinDivert.Close(_handle);
            _handle = IntPtr.Zero;
        }
    }

    /// <summary>
    /// 接收数据包
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="addr">地址结构</param>
    /// <returns>实际读取的字节数，失败返回 0</returns>
    public uint Receive(byte[] buffer, ref WinDivert.WINDIVERT_ADDRESS addr)
    {
        if (!IsValid || buffer == null)
        {
            return 0;
        }

        uint readLen = 0;
        if (WinDivert.Recv(_handle, buffer, (uint)buffer.Length, ref addr, ref readLen))
        {
            return readLen;
        }

        return 0;
    }

    /// <summary>
    /// 异步接收数据包
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="addr">地址结构</param>
    /// <returns>实际读取的字节数的任务</returns>
    public Task<uint> ReceiveAsync(byte[] buffer, WinDivert.WINDIVERT_ADDRESS addr)
    {
        return Task.Run(() => Receive(buffer, ref addr));
    }

    /// <summary>
    /// 发送数据包
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="addr">地址结构</param>
    /// <returns>实际写入的字节数，失败返回 0</returns>
    public uint Send(byte[] buffer, ref WinDivert.WINDIVERT_ADDRESS addr)
    {
        if (!IsValid || buffer == null)
        {
            return 0;
        }

        uint writeLen = 0;
        if (WinDivert.Send(_handle, buffer, (uint)buffer.Length, ref addr, ref writeLen))
        {
            return writeLen;
        }

        return 0;
    }

    /// <summary>
    /// 异步发送数据包
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="addr">地址结构</param>
    /// <returns>实际写入的字节数的任务</returns>
    public Task<uint> SendAsync(byte[] buffer, WinDivert.WINDIVERT_ADDRESS addr)
    {
        return Task.Run(() => Send(buffer, ref addr));
    }

    /// <summary>
    /// 设置参数
    /// </summary>
    /// <param name="param">参数类型</param>
    /// <param name="value">参数值</param>
    /// <returns>是否成功</returns>
    public bool SetParam(int param, ulong value)
    {
        if (!IsValid)
        {
            return false;
        }

        return WinDivert.SetParam(_handle, param, value);
    }

    /// <summary>
    /// 获取参数
    /// </summary>
    /// <param name="param">参数类型</param>
    /// <param name="value">参数值</param>
    /// <returns>是否成功</returns>
    public bool GetParam(int param, ref ulong value)
    {
        if (!IsValid)
        {
            return false;
        }

        return WinDivert.GetParam(_handle, param, ref value);
    }

    /// <summary>
    /// 校验包数据的校验和
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="addr">地址结构</param>
    /// <param name="flags">标志</param>
    /// <returns>校验和结果</returns>
    public uint CalcChecksums(byte[] buffer, ref WinDivert.WINDIVERT_ADDRESS addr, uint flags = 0)
    {
        if (buffer == null)
        {
            return 0;
        }

        return WinDivert.CalcChecksums(buffer, (uint)buffer.Length, ref addr, flags);
    }

    /// <summary>
    /// 检查过滤器语法
    /// </summary>
    /// <param name="filter">过滤器表达式</param>
    /// <param name="layer">层级</param>
    /// <returns>检查结果和错误信息</returns>
    public (bool IsValid, string ErrorMessage) CheckFilter(string filter,
        uint layer = WinDivert.WINDIVERT_LAYER_NETWORK)
    {
        StringBuilder errorStr = new StringBuilder(256);
        bool isValid = WinDivert.CheckFilter(filter, layer, errorStr, 256);
        return (isValid, errorStr.ToString());
    }

    /// <summary>
    /// 解析数据包
    /// </summary>
    /// <param name="packet">数据包</param>
    /// <returns>解析结果</returns>
    public PacketHeaders ParsePacket(byte[] packet)
    {
        if (packet == null)
        {
            return new PacketHeaders();
        }

        IntPtr ppIpHdr = IntPtr.Zero;
        IntPtr ppIpv6Hdr = IntPtr.Zero;
        IntPtr ppIcmpHdr = IntPtr.Zero;
        IntPtr ppIcmpv6Hdr = IntPtr.Zero;
        IntPtr ppTcpHdr = IntPtr.Zero;
        IntPtr ppUdpHdr = IntPtr.Zero;
        IntPtr ppData = IntPtr.Zero;
        uint pDataLen = 0;

        bool success = WinDivert.ParsePacket(packet, (uint)packet.Length, ref ppIpHdr, ref ppIpv6Hdr,
            ref ppIcmpHdr, ref ppIcmpv6Hdr, ref ppTcpHdr, ref ppUdpHdr, ref ppData, ref pDataLen);

        PacketHeaders headers = new PacketHeaders
        {
            Success = success,
            DataLength = pDataLen
        };

        if (success)
        {
            if (ppIpHdr != IntPtr.Zero)
            {
                headers.IpHeader = Marshal.PtrToStructure<WinDivert.WINDIVERT_IPHDR>(ppIpHdr);
                headers.HasIpHeader = true;
            }

            if (ppIpv6Hdr != IntPtr.Zero)
            {
                headers.Ipv6Header = Marshal.PtrToStructure<WinDivert.WINDIVERT_IPV6HDR>(ppIpv6Hdr);
                headers.HasIpv6Header = true;
            }

            if (ppTcpHdr != IntPtr.Zero)
            {
                headers.TcpHeader = Marshal.PtrToStructure<WinDivert.WINDIVERT_TCPHDR>(ppTcpHdr);
                headers.HasTcpHeader = true;
            }

            if (ppUdpHdr != IntPtr.Zero)
            {
                headers.UdpHeader = Marshal.PtrToStructure<WinDivert.WINDIVERT_UDPHDR>(ppUdpHdr);
                headers.HasUdpHeader = true;
            }

            if (ppIcmpHdr != IntPtr.Zero)
            {
                headers.IcmpHeader = Marshal.PtrToStructure<WinDivert.WINDIVERT_ICMPHDR>(ppIcmpHdr);
                headers.HasIcmpHeader = true;
            }

            if (ppIcmpv6Hdr != IntPtr.Zero)
            {
                headers.Icmpv6Header = Marshal.PtrToStructure<WinDivert.WINDIVERT_ICMPV6HDR>(ppIcmpv6Hdr);
                headers.HasIcmpv6Header = true;
            }

            if (ppData != IntPtr.Zero && pDataLen > 0)
            {
                headers.Data = new byte[pDataLen];
                Marshal.Copy(ppData, headers.Data, 0, (int)pDataLen);
            }
        }

        return headers;
    }

    /// <summary>
    /// 修改包的源 IP 地址
    /// </summary>
    /// <param name="packet">数据包</param>
    /// <param name="sourceIp">新的源 IP 地址</param>
    /// <returns>是否成功</returns>
    public bool ModifySourceIpv4Address(byte[] packet, string sourceIp)
    {
        if (packet == null || string.IsNullOrEmpty(sourceIp))
        {
            return false;
        }

        PacketHeaders headers = ParsePacket(packet);
        if (!headers.Success || !headers.HasIpHeader)
        {
            return false;
        }

        uint addr = 0;
        if (!WinDivert.ParseIPv4Address(sourceIp, ref addr))
        {
            return false;
        }

        // 计算 IP 头的偏移量
        int ipHdrOffset = Marshal.SizeOf<WinDivert.WINDIVERT_IPHDR>();

        // 修改源 IP 地址
        byte[] addrBytes = BitConverter.GetBytes(addr);
        Array.Copy(addrBytes, 0, packet, ipHdrOffset - 8, 4);

        return true;
    }

    /// <summary>
    /// 修改包的目标 IP 地址
    /// </summary>
    /// <param name="packet">数据包</param>
    /// <param name="destinationIp">新的目标 IP 地址</param>
    /// <returns>是否成功</returns>
    public bool ModifyDestinationIpv4Address(byte[] packet, string destinationIp)
    {
        if (packet == null || string.IsNullOrEmpty(destinationIp))
        {
            return false;
        }

        PacketHeaders headers = ParsePacket(packet);
        if (!headers.Success || !headers.HasIpHeader)
        {
            return false;
        }

        uint addr = 0;
        if (!WinDivert.ParseIPv4Address(destinationIp, ref addr))
        {
            return false;
        }

        // 计算 IP 头的偏移量
        int ipHdrOffset = Marshal.SizeOf<WinDivert.WINDIVERT_IPHDR>();

        // 修改目标 IP 地址
        byte[] addrBytes = BitConverter.GetBytes(addr);
        Array.Copy(addrBytes, 0, packet, ipHdrOffset - 4, 4);

        return true;
    }

    /// <summary>
    /// 修改包的源端口
    /// </summary>
    /// <param name="packet">数据包</param>
    /// <param name="sourcePort">新的源端口</param>
    /// <returns>是否成功</returns>
    public bool ModifySourcePort(byte[] packet, ushort sourcePort)
    {
        if (packet == null)
        {
            return false;
        }

        PacketHeaders headers = ParsePacket(packet);
        if (!headers.Success)
        {
            return false;
        }

        int offset = -1;

        if (headers.HasTcpHeader)
        {
            // 计算 TCP 头的偏移量（简化计算，实际应该考虑 IP 选项）
            offset = (headers.HasIpHeader ? 20 : 0) + (headers.HasIpv6Header ? 40 : 0);
        }
        else if (headers.HasUdpHeader)
        {
            // 计算 UDP 头的偏移量（简化计算，实际应该考虑 IP 选项）
            offset = (headers.HasIpHeader ? 20 : 0) + (headers.HasIpv6Header ? 40 : 0);
        }

        if (offset < 0)
        {
            return false;
        }

        // 修改源端口
        byte[] portBytes = BitConverter.GetBytes(sourcePort);
        Array.Copy(portBytes, 0, packet, offset, 2);

        return true;
    }

    /// <summary>
    /// 修改包的目标端口
    /// </summary>
    /// <param name="packet">数据包</param>
    /// <param name="destinationPort">新的目标端口</param>
    /// <returns>是否成功</returns>
    public bool ModifyDestinationPort(byte[] packet, ushort destinationPort)
    {
        if (packet == null)
        {
            return false;
        }

        PacketHeaders headers = ParsePacket(packet);
        if (!headers.Success)
        {
            return false;
        }

        int offset = -1;

        if (headers.HasTcpHeader)
        {
            // 计算 TCP 头的偏移量（简化计算，实际应该考虑 IP 选项）
            offset = (headers.HasIpHeader ? 20 : 0) + (headers.HasIpv6Header ? 40 : 0) + 2;
        }
        else if (headers.HasUdpHeader)
        {
            // 计算 UDP 头的偏移量（简化计算，实际应该考虑 IP 选项）
            offset = (headers.HasIpHeader ? 20 : 0) + (headers.HasIpv6Header ? 40 : 0) + 2;
        }

        if (offset < 0)
        {
            return false;
        }

        // 修改目标端口
        byte[] portBytes = BitConverter.GetBytes(destinationPort);
        Array.Copy(portBytes, 0, packet, offset, 2);

        return true;
    }

    /// <summary>
    /// 重新计算包的校验和
    /// </summary>
    /// <param name="packet">数据包</param>
    /// <param name="addr">地址结构</param>
    /// <returns>是否成功</returns>
    public bool RecalculateChecksums(byte[] packet, ref WinDivert.WINDIVERT_ADDRESS addr)
    {
        if (packet == null)
        {
            return false;
        }

        uint result = WinDivert.CalcChecksums(packet, (uint)packet.Length, ref addr, 0);
        return result > 0;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="disposing">是否为显式释放</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
            }

            // 释放非托管资源
            Close();
            _disposed = true;
        }
    }
}


/// <summary>
/// 数据包头信息
/// </summary>
public class PacketHeaders
{
    /// <summary>
    /// 解析是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 是否包含 IPv4 头
    /// </summary>
    public bool HasIpHeader { get; set; }

    /// <summary>
    /// 是否包含 IPv6 头
    /// </summary>
    public bool HasIpv6Header { get; set; }

    /// <summary>
    /// 是否包含 TCP 头
    /// </summary>
    public bool HasTcpHeader { get; set; }

    /// <summary>
    /// 是否包含 UDP 头
    /// </summary>
    public bool HasUdpHeader { get; set; }

    /// <summary>
    /// 是否包含 ICMP 头
    /// </summary>
    public bool HasIcmpHeader { get; set; }

    /// <summary>
    /// 是否包含 ICMPv6 头
    /// </summary>
    public bool HasIcmpv6Header { get; set; }

    /// <summary>
    /// IPv4 头
    /// </summary>
    public WinDivert.WINDIVERT_IPHDR IpHeader { get; set; }

    /// <summary>
    /// IPv6 头
    /// </summary>
    public WinDivert.WINDIVERT_IPV6HDR Ipv6Header { get; set; }

    /// <summary>
    /// TCP 头
    /// </summary>
    public WinDivert.WINDIVERT_TCPHDR TcpHeader { get; set; }

    /// <summary>
    /// UDP 头
    /// </summary>
    public WinDivert.WINDIVERT_UDPHDR UdpHeader { get; set; }

    /// <summary>
    /// ICMP 头
    /// </summary>
    public WinDivert.WINDIVERT_ICMPHDR IcmpHeader { get; set; }

    /// <summary>
    /// ICMPv6 头
    /// </summary>
    public WinDivert.WINDIVERT_ICMPV6HDR Icmpv6Header { get; set; }

    /// <summary>
    /// 数据部分
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// 数据长度
    /// </summary>
    public uint DataLength { get; set; }
}
