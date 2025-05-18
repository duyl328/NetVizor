namespace WinDivertNet.WinDivertWrapper;

using System;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// WinDivert 的 C# 封装类
/// </summary>
public static class WinDivert
{
    #region 常量定义

    // 层级常量
    public const uint WINDIVERT_LAYER_NETWORK = 0;
    public const uint WINDIVERT_LAYER_NETWORK_FORWARD = 1;
    public const uint WINDIVERT_LAYER_FLOW = 2;
    public const uint WINDIVERT_LAYER_SOCKET = 3;
    public const uint WINDIVERT_LAYER_REFLECT = 4;

    // 标志常量
    public const ulong WINDIVERT_FLAG_SNIFF = 1;
    public const ulong WINDIVERT_FLAG_DROP = 2;
    public const ulong WINDIVERT_FLAG_RECV_ONLY = 4;
    public const ulong WINDIVERT_FLAG_SEND_ONLY = 8;
    public const ulong WINDIVERT_FLAG_NO_INSTALL = 16;
    public const ulong WINDIVERT_FLAG_FRAGMENTS = 32;
    public const ulong WINDIVERT_FLAG_RECV_PARTIAL = 64;
    public const ulong WINDIVERT_FLAG_SEND_PARTIAL = 128;

    // 事件常量
    public const byte WINDIVERT_EVENT_NETWORK_PACKET = 0;
    public const byte WINDIVERT_EVENT_FLOW_ESTABLISHED = 1;
    public const byte WINDIVERT_EVENT_FLOW_DELETED = 2;
    public const byte WINDIVERT_EVENT_SOCKET_BIND = 3;
    public const byte WINDIVERT_EVENT_SOCKET_CONNECT = 4;
    public const byte WINDIVERT_EVENT_SOCKET_LISTEN = 5;
    public const byte WINDIVERT_EVENT_SOCKET_ACCEPT = 6;
    public const byte WINDIVERT_EVENT_REFLECT_OPEN = 7;
    public const byte WINDIVERT_EVENT_REFLECT_CLOSE = 8;

    // 方向常量
    public const byte WINDIVERT_DIRECTION_OUTBOUND = 0;
    public const byte WINDIVERT_DIRECTION_INBOUND = 1;

    // 参数常量
    public const int WINDIVERT_PARAM_QUEUE_LENGTH = 0;
    public const int WINDIVERT_PARAM_QUEUE_TIME = 1;
    public const int WINDIVERT_PARAM_QUEUE_SIZE = 2;
    public const int WINDIVERT_PARAM_VERSION_MAJOR = 3;
    public const int WINDIVERT_PARAM_VERSION_MINOR = 4;

    // 校验和常量
    public const uint WINDIVERT_HELPER_NO_REPLACE = 0;
    public const uint WINDIVERT_HELPER_NO_ICMPV6 = 0;
    public const uint WINDIVERT_HELPER_NO_ICMP = 0;
    public const uint WINDIVERT_HELPER_NO_TCP = 0;
    public const uint WINDIVERT_HELPER_NO_UDP = 0;
    public const uint WINDIVERT_HELPER_NO_IP = 0;
    public const uint WINDIVERT_HELPER_NO_IPV6 = 0;

    // 优先级范围
    public const short WINDIVERT_PRIORITY_HIGHEST = 30000;
    public const short WINDIVERT_PRIORITY_LOWEST = -30000;
    public const short WINDIVERT_PRIORITY_DEFAULT = 0;

    #endregion

    #region 结构体定义

    /// <summary>
    /// WinDivert 地址结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDIVERT_ADDRESS
    {
        public long Timestamp; // 时间戳
        public byte Layer; // 层级
        public byte Event; // 事件
        public byte Flags; // 标志
        public byte Reserved1; // 保留
        public uint IfIdx; // 接口索引
        public uint SubIfIdx; // 子接口索引
        public byte Direction; // 方向
        public byte Reserved2; // 保留
        public ulong Reserved3; // 保留
    }

    /// <summary>
    /// IPv4 头结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDIVERT_IPHDR
    {
        public byte HdrLength4; // 组合字段: 版本 (4 bits) + 头长度 (4 bits)
        public byte TOS; // 服务类型
        public ushort Length; // 总长度
        public ushort Id; // 标识
        public ushort FragOff0; // 组合字段: 标志 (3 bits) + 分片偏移 (13 bits)
        public byte TTL; // 生存时间
        public byte Protocol; // 协议
        public ushort Checksum; // 检验和
        public uint SrcAddr; // 源地址
        public uint DstAddr; // 目标地址
    }

    /// <summary>
    /// IPv6 头结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDIVERT_IPV6HDR
    {
        public uint FlowLabel0; // 组合字段: 版本 (4 bits) + 通信类 (8 bits) + 流标签 (20 bits)
        public ushort Length; // 有效载荷长度
        public byte NextHdr; // 下一个头
        public byte HopLimit; // 跳数限制

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] SrcAddr; // 源地址

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] DstAddr; // 目标地址
    }

    /// <summary>
    /// TCP 头结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDIVERT_TCPHDR
    {
        public ushort SrcPort; // 源端口
        public ushort DstPort; // 目标端口
        public uint SeqNum; // 序列号
        public uint AckNum; // 确认号
        public ushort Reserved10; // 组合字段: 数据偏移 (4 bits) + 保留 (6 bits) + 标志 (6 bits)
        public ushort Window; // 窗口大小
        public ushort Checksum; // 检验和
        public ushort UrgPtr; // 紧急指针
    }

    /// <summary>
    /// UDP 头结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDIVERT_UDPHDR
    {
        public ushort SrcPort; // 源端口
        public ushort DstPort; // 目标端口
        public ushort Length; // 长度
        public ushort Checksum; // 检验和
    }

    /// <summary>
    /// ICMP 头结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDIVERT_ICMPHDR
    {
        public byte Type; // 类型
        public byte Code; // 代码
        public ushort Checksum; // 检验和
        public uint Body; // 消息体
    }

    /// <summary>
    /// ICMPv6 头结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDIVERT_ICMPV6HDR
    {
        public byte Type; // 类型
        public byte Code; // 代码
        public ushort Checksum; // 检验和
        public uint Body; // 消息体
    }

    #endregion

    #region 原生 API 导入

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr WinDivertOpen(
        [MarshalAs(UnmanagedType.LPStr)] string filter,
        uint layer,
        short priority,
        ulong flags);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertRecv(
        IntPtr handle,
        byte[] pPacket,
        uint packetLen,
        ref WINDIVERT_ADDRESS pAddr,
        ref uint readLen);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertRecvEx(
        IntPtr handle,
        byte[] pPacket,
        uint packetLen,
        uint flags,
        ref WINDIVERT_ADDRESS pAddr,
        ref uint readLen,
        IntPtr overlapped);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertSend(
        IntPtr handle,
        byte[] pPacket,
        uint packetLen,
        ref WINDIVERT_ADDRESS pAddr,
        ref uint writeLen);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertSendEx(
        IntPtr handle,
        byte[] pPacket,
        uint packetLen,
        uint flags,
        ref WINDIVERT_ADDRESS pAddr,
        ref uint writeLen,
        IntPtr overlapped);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertClose(
        IntPtr handle);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertSetParam(
        IntPtr handle,
        int param,
        ulong value);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertGetParam(
        IntPtr handle,
        int param,
        ref ulong value);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint WinDivertHelperCalcChecksums(
        byte[] pPacket,
        uint packetLen,
        ref WINDIVERT_ADDRESS pAddr,
        uint flags);

    // [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    // public static extern bool WinDivertHelperParsePacket(
    //     byte[] pPacket,
    //     uint packetLen,
    //     ref IntPtr ppIpHdr,
    //     ref IntPtr ppIpv6Hdr,
    //     ref IntPtr ppIcmpHdr,
    //     ref IntPtr ppIcmpv6Hdr,
    //     ref IntPtr ppTcpHdr,
    //     ref IntPtr ppUdpHdr,
    //     ref IntPtr ppData,
    //     ref uint pDataLen);
    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertHelperParsePacket(
        [In] byte[] pPacket,
        uint packetLen,
        [Out] out IntPtr ppIpHdr,
        [Out] out IntPtr ppIpv6Hdr,
        [Out] out IntPtr ppIcmpHdr,
        [Out] out IntPtr ppIcmpv6Hdr,
        [Out] out IntPtr ppTcpHdr,
        [Out] out IntPtr ppUdpHdr,
        [Out] out IntPtr ppData,
        [Out] out uint pDataLen);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertHelperParseIPv4Address(
        [MarshalAs(UnmanagedType.LPStr)] string str,
        ref uint pAddr);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertHelperParseIPv6Address(
        [MarshalAs(UnmanagedType.LPStr)] string str,
        byte[] pAddr);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertHelperFormatIPv4Address(
        uint addr,
        [MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer,
        uint bufLen);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertHelperFormatIPv6Address(
        byte[] addr,
        [MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer,
        uint bufLen);

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertHelperCheckFilter(
        [MarshalAs(UnmanagedType.LPStr)] string filter,
        uint layer,
        [MarshalAs(UnmanagedType.LPStr)] StringBuilder errorStr,
        uint errorStrLen);

    #endregion

    #region 辅助方法

    /// <summary>
    /// 打开 WinDivert 句柄
    /// </summary>
    /// <param name="filter">过滤器表达式</param>
    /// <param name="layer">层级</param>
    /// <param name="priority">优先级</param>
    /// <param name="flags">标志</param>
    /// <returns>WinDivert 句柄</returns>
    public static IntPtr Open(string filter, uint layer, short priority, ulong flags) =>
        WinDivertOpen(filter, layer, priority, flags);

    /// <summary>
    /// 接收数据包
    /// </summary>
    /// <param name="handle">WinDivert 句柄</param>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="len">缓冲区长度</param>
    /// <param name="addr">地址结构</param>
    /// <param name="readLen">实际读取长度</param>
    /// <returns>是否成功</returns>
    public static bool Recv(IntPtr handle, byte[] buffer, uint len, ref WINDIVERT_ADDRESS addr, ref uint readLen) =>
        WinDivertRecv(handle, buffer, len, ref addr, ref readLen);

    /// <summary>
    /// 扩展接收数据包
    /// </summary>
    /// <param name="handle">WinDivert 句柄</param>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="len">缓冲区长度</param>
    /// <param name="flags">标志</param>
    /// <param name="addr">地址结构</param>
    /// <param name="readLen">实际读取长度</param>
    /// <param name="overlapped">重叠 I/O 结构</param>
    /// <returns>是否成功</returns>
    public static bool RecvEx(IntPtr handle, byte[] buffer, uint len, uint flags, ref WINDIVERT_ADDRESS addr,
        ref uint readLen, IntPtr overlapped) =>
        WinDivertRecvEx(handle, buffer, len, flags, ref addr, ref readLen, overlapped);

    /// <summary>
    /// 发送数据包
    /// </summary>
    /// <param name="handle">WinDivert 句柄</param>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="len">缓冲区长度</param>
    /// <param name="addr">地址结构</param>
    /// <param name="writeLen">实际写入长度</param>
    /// <returns>是否成功</returns>
    public static bool Send(IntPtr handle, byte[] buffer, uint len, ref WINDIVERT_ADDRESS addr, ref uint writeLen) =>
        WinDivertSend(handle, buffer, len, ref addr, ref writeLen);

    /// <summary>
    /// 扩展发送数据包
    /// </summary>
    /// <param name="handle">WinDivert 句柄</param>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="len">缓冲区长度</param>
    /// <param name="flags">标志</param>
    /// <param name="addr">地址结构</param>
    /// <param name="writeLen">实际写入长度</param>
    /// <param name="overlapped">重叠 I/O 结构</param>
    /// <returns>是否成功</returns>
    public static bool SendEx(IntPtr handle, byte[] buffer, uint len, uint flags, ref WINDIVERT_ADDRESS addr,
        ref uint writeLen, IntPtr overlapped) =>
        WinDivertSendEx(handle, buffer, len, flags, ref addr, ref writeLen, overlapped);

    /// <summary>
    /// 关闭 WinDivert 句柄
    /// </summary>
    /// <param name="handle">WinDivert 句柄</param>
    /// <returns>是否成功</returns>
    public static bool Close(IntPtr handle) =>
        WinDivertClose(handle);

    /// <summary>
    /// 设置参数
    /// </summary>
    /// <param name="handle">WinDivert 句柄</param>
    /// <param name="param">参数类型</param>
    /// <param name="value">参数值</param>
    /// <returns>是否成功</returns>
    public static bool SetParam(IntPtr handle, int param, ulong value) =>
        WinDivertSetParam(handle, param, value);

    /// <summary>
    /// 获取参数
    /// </summary>
    /// <param name="handle">WinDivert 句柄</param>
    /// <param name="param">参数类型</param>
    /// <param name="value">参数值</param>
    /// <returns>是否成功</returns>
    public static bool GetParam(IntPtr handle, int param, ref ulong value) =>
        WinDivertGetParam(handle, param, ref value);

    /// <summary>
    /// 计算校验和
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="len">缓冲区长度</param>
    /// <param name="addr">地址结构</param>
    /// <param name="flags">标志</param>
    /// <returns>校验和结果</returns>
    public static uint CalcChecksums(byte[] buffer, uint len, ref WINDIVERT_ADDRESS addr, uint flags) =>
        WinDivertHelperCalcChecksums(buffer, len, ref addr, flags);

    /// <summary>
    /// 解析数据包
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="len">缓冲区长度</param>
    /// <param name="ppIpHdr">IPv4 头指针</param>
    /// <param name="ppIpv6Hdr">IPv6 头指针</param>
    /// <param name="ppIcmpHdr">ICMP 头指针</param>
    /// <param name="ppIcmpv6Hdr">ICMPv6 头指针</param>
    /// <param name="ppTcpHdr">TCP 头指针</param>
    /// <param name="ppUdpHdr">UDP 头指针</param>
    /// <param name="ppData">数据指针</param>
    /// <param name="pDataLen">数据长度</param>
    /// <returns>是否成功</returns>
    public static bool ParsePacket(byte[] buffer, uint len, ref IntPtr ppIpHdr, ref IntPtr ppIpv6Hdr,
        ref IntPtr ppIcmpHdr, ref IntPtr ppIcmpv6Hdr, ref IntPtr ppTcpHdr, ref IntPtr ppUdpHdr,
        ref IntPtr ppData, ref uint pDataLen) =>
        WinDivertHelperParsePacket(buffer, len, out ppIpHdr, out ppIpv6Hdr, out ppIcmpHdr, out ppIcmpv6Hdr,
            out ppTcpHdr, out ppUdpHdr, out ppData, out pDataLen);

    /// <summary>
    /// 解析 IPv4 地址
    /// </summary>
    /// <param name="str">IP 地址字符串</param>
    /// <param name="pAddr">地址结果</param>
    /// <returns>是否成功</returns>
    public static bool ParseIPv4Address(string str, ref uint pAddr) =>
        WinDivertHelperParseIPv4Address(str, ref pAddr);

    /// <summary>
    /// 解析 IPv6 地址
    /// </summary>
    /// <param name="str">IP 地址字符串</param>
    /// <param name="pAddr">地址结果</param>
    /// <returns>是否成功</returns>
    public static bool ParseIPv6Address(string str, byte[] pAddr) =>
        WinDivertHelperParseIPv6Address(str, pAddr);

    /// <summary>
    /// 格式化 IPv4 地址
    /// </summary>
    /// <param name="addr">IP 地址</param>
    /// <returns>格式化后的字符串</returns>
    public static string FormatIPv4Address(uint addr)
    {
        StringBuilder buffer = new StringBuilder(16);
        if (WinDivertHelperFormatIPv4Address(addr, buffer, 16))
        {
            return buffer.ToString();
        }

        return string.Empty;
    }

    /// <summary>
    /// 格式化 IPv6 地址
    /// </summary>
    /// <param name="addr">IP 地址</param>
    /// <returns>格式化后的字符串</returns>
    public static string FormatIPv6Address(byte[] addr)
    {
        StringBuilder buffer = new StringBuilder(46);
        if (WinDivertHelperFormatIPv6Address(addr, buffer, 46))
        {
            return buffer.ToString();
        }

        return string.Empty;
    }

    /// <summary>
    /// 检查过滤器语法
    /// </summary>
    /// <param name="filter">过滤器表达式</param>
    /// <param name="layer">层级</param>
    /// <param name="errorStr">错误信息</param>
    /// <param name="errorStrLen">错误信息长度</param>
    /// <returns>是否有效</returns>
    public static bool CheckFilter(string filter, uint layer, StringBuilder errorStr, uint errorStrLen) =>
        WinDivertHelperCheckFilter(filter, layer, errorStr, errorStrLen);

    #endregion
    
    [StructLayout(LayoutKind.Sequential)]
    public struct PacketHeaders
    {
        public IntPtr IpHdr;
        public IntPtr Ipv6Hdr;
        public IntPtr IcmpHdr;
        public IntPtr Icmpv6Hdr;
        public IntPtr TcpHdr;
        public IntPtr UdpHdr;
        public IntPtr Data;
        public uint DataLen;
    }

    [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool WinDivertHelperParsePacketEx(
        [In] byte[] pPacket,
        uint packetLen,
        ref PacketHeaders headers);

}
