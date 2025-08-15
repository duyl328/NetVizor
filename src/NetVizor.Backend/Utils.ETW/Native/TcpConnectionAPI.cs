using System;
using System.Runtime.InteropServices;

namespace Utils.ETW.Native;

/// <summary>
/// TCP连接表相关的Win32 API声明
/// 用于精确统计网络流量，替代ETW方案解决突发流量统计问题
/// </summary>
public static class TcpConnectionAPI
{
    #region 常量定义

    public const int NO_ERROR = 0;
    public const int ERROR_INSUFFICIENT_BUFFER = 122;
    public const int ERROR_INVALID_PARAMETER = 87;

    // TCP连接状态
    public enum MIB_TCP_STATE
    {
        MIB_TCP_STATE_CLOSED = 1,
        MIB_TCP_STATE_LISTEN = 2,
        MIB_TCP_STATE_SYN_SENT = 3,
        MIB_TCP_STATE_SYN_RCVD = 4,
        MIB_TCP_STATE_ESTAB = 5,
        MIB_TCP_STATE_FIN_WAIT1 = 6,
        MIB_TCP_STATE_FIN_WAIT2 = 7,
        MIB_TCP_STATE_CLOSE_WAIT = 8,
        MIB_TCP_STATE_CLOSING = 9,
        MIB_TCP_STATE_LAST_ACK = 10,
        MIB_TCP_STATE_TIME_WAIT = 11,
        MIB_TCP_STATE_DELETE_TCB = 12
    }

    // TCP表类型
    public enum TCP_TABLE_CLASS
    {
        TCP_TABLE_BASIC_LISTENER,
        TCP_TABLE_BASIC_CONNECTIONS,
        TCP_TABLE_BASIC_ALL,
        TCP_TABLE_OWNER_PID_LISTENER,
        TCP_TABLE_OWNER_PID_CONNECTIONS,
        TCP_TABLE_OWNER_PID_ALL,
        TCP_TABLE_OWNER_MODULE_LISTENER,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS,
        TCP_TABLE_OWNER_MODULE_ALL
    }

    // 统计信息启用标志
    public enum TCP_ESTATS_TYPE
    {
        TcpConnectionEstatsData,
        TcpConnectionEstatsSynOpts,
        TcpConnectionEstatsPath,
        TcpConnectionEstatsSendBuff,
        TcpConnectionEstatsRec,
        TcpConnectionEstatsObs,
        TcpConnectionEstatsBandwidth,
        TcpConnectionEstatsFineRtt,
        TcpConnectionEstatsMaximum
    }

    public enum TCP_BOOLEAN_OPTIONAL
    {
        TcpBoolOptDisabled = -1,
        TcpBoolOptEnabled = 1,
        TcpBoolOptUnchanged = -2
    }

    #endregion

    #region 数据结构

    /// <summary>
    /// TCP连接行（包含进程ID）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID
    {
        public MIB_TCP_STATE dwState;
        public uint dwLocalAddr;
        public uint dwLocalPort; // 网络字节序
        public uint dwRemoteAddr;
        public uint dwRemotePort; // 网络字节序
        public uint dwOwningPid;
    }

    /// <summary>
    /// TCP连接表（包含进程ID）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPTABLE_OWNER_PID
    {
        public uint dwNumEntries;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public MIB_TCPROW_OWNER_PID[] table;
    }

    /// <summary>
    /// TCP连接的4元组标识
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW
    {
        public uint dwLocalAddr;
        public uint dwLocalPort;
        public uint dwRemoteAddr;
        public uint dwRemotePort;
    }

    /// <summary>
    /// TCP连接数据统计信息
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TCP_ESTATS_DATA_ROD_v0
    {
        public ulong DataBytesOut; // 发送的数据字节数
        public ulong DataSegsOut; // 发送的数据段数
        public ulong DataBytesIn; // 接收的数据字节数
        public ulong DataSegsIn; // 接收的数据段数
        public ulong SegsOut; // 发送的总段数
        public ulong SegsIn; // 接收的总段数
        public uint SoftErrors; // 软错误数
        public uint SoftErrorReason; // 软错误原因
        public uint SndUna; // 发送未确认序号
        public uint SndNxt; // 发送下一个序号
        public uint SndMax; // 发送最大序号
        public uint ThruBytesAcked; // 确认的吞吐字节数
        public uint RcvNxt; // 接收下一个序号
        public uint ThruBytesReceived; // 接收的吞吐字节数
    }

    /// <summary>
    /// 统计信息启用设置
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TCP_ESTATS_DATA_RW_v0
    {
        public TCP_BOOLEAN_OPTIONAL EnableCollection;
    }

    #endregion

    #region Win32 API函数

    /// <summary>
    /// 获取扩展TCP连接表（包含进程ID）
    /// </summary>
    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern int GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int dwOutBufLen,
        bool bOrder,
        int ulAf, // AF_INET = 2
        TCP_TABLE_CLASS TableClass,
        uint Reserved);

    /// <summary>
    /// 设置TCP连接的统计信息收集
    /// </summary>
    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern int SetPerTcpConnectionEStats(
        ref MIB_TCPROW Row,
        TCP_ESTATS_TYPE EstatsType,
        IntPtr Rw,
        uint RwVersion,
        uint RwSize,
        uint Offset);

    /// <summary>
    /// 获取TCP连接的统计信息
    /// </summary>
    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern int GetPerTcpConnectionEStats(
        ref MIB_TCPROW Row,
        TCP_ESTATS_TYPE EstatsType,
        IntPtr Rw,
        uint RwVersion,
        uint RwSize,
        IntPtr Ros,
        uint RosVersion,
        uint RosSize,
        IntPtr Rod,
        uint RodVersion,
        uint RodSize);

    #endregion

    #region 辅助方法

    /// <summary>
    /// 将网络字节序端口转换为主机字节序
    /// </summary>
    public static ushort NetworkToHostOrder(uint networkPort)
    {
        return (ushort)((networkPort & 0xFF) << 8 | (networkPort >> 8) & 0xFF);
    }

    /// <summary>
    /// 将IP地址从uint转换为字符串
    /// </summary>
    public static string UintToIpAddress(uint ip)
    {
        return $"{ip & 0xFF}.{(ip >> 8) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 24) & 0xFF}";
    }

    /// <summary>
    /// 获取TCP连接状态的友好名称
    /// </summary>
    public static string GetStateName(MIB_TCP_STATE state)
    {
        return state switch
        {
            MIB_TCP_STATE.MIB_TCP_STATE_CLOSED => "CLOSED",
            MIB_TCP_STATE.MIB_TCP_STATE_LISTEN => "LISTEN",
            MIB_TCP_STATE.MIB_TCP_STATE_SYN_SENT => "SYN_SENT",
            MIB_TCP_STATE.MIB_TCP_STATE_SYN_RCVD => "SYN_RCVD",
            MIB_TCP_STATE.MIB_TCP_STATE_ESTAB => "ESTABLISHED",
            MIB_TCP_STATE.MIB_TCP_STATE_FIN_WAIT1 => "FIN_WAIT1",
            MIB_TCP_STATE.MIB_TCP_STATE_FIN_WAIT2 => "FIN_WAIT2",
            MIB_TCP_STATE.MIB_TCP_STATE_CLOSE_WAIT => "CLOSE_WAIT",
            MIB_TCP_STATE.MIB_TCP_STATE_CLOSING => "CLOSING",
            MIB_TCP_STATE.MIB_TCP_STATE_LAST_ACK => "LAST_ACK",
            MIB_TCP_STATE.MIB_TCP_STATE_TIME_WAIT => "TIME_WAIT",
            MIB_TCP_STATE.MIB_TCP_STATE_DELETE_TCB => "DELETE_TCB",
            _ => "UNKNOWN"
        };
    }

    #endregion
}