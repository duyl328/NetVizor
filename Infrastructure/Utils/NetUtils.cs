using System.Net;
using System.Runtime.InteropServices;
using Common.Logger;

namespace Infrastructure.utils;

using Microsoft.Diagnostics.Tracing.Parsers;
using System;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

public enum TCP_TABLE_CLASS
{
    TCP_TABLE_BASIC_LISTENER,
    TCP_TABLE_BASIC_CONNECTIONS,
    TCP_TABLE_BASIC_ALL,
    TCP_TABLE_OWNER_PID_LISTENER,
    TCP_TABLE_OWNER_PID_CONNECTIONS,
    TCP_TABLE_OWNER_PID_ALL, // 使用这个
    TCP_TABLE_OWNER_MODULE_LISTENER,
    TCP_TABLE_OWNER_MODULE_CONNECTIONS,
    TCP_TABLE_OWNER_MODULE_ALL
}

public enum UDP_TABLE_CLASS
{
    UDP_TABLE_BASIC,
    UDP_TABLE_OWNER_PID,
    UDP_TABLE_OWNER_MODULE
}

public static class NetUtils
{
    /// <summary>
    /// 联网内容监视 
    /// </summary>
    public static void GetNetInfo()
    {
        if (TraceEventSession.IsElevated() == false)
        {
            Log.Information("请以管理员身份运行此程序。");
            return;
        }

        using (var session = new TraceEventSession("NetTraceSession"))
        {
            // 启用 Kernel 网络事件
            session.EnableKernelProvider(
                KernelTraceEventParser.Keywords.NetworkTCPIP);

            Log.Information("监听网络事件中（按 Ctrl+C 停止）...");
            session.Source.Kernel.TcpIpConnect += data =>
            {
                Log.Information(
                    $"[TCP Connect] PID={data.ProcessID} {data.saddr}:{data.sport} -> {data.daddr}:{data.dport}");
            };
            session.Source.Kernel.TcpIpDisconnect += data =>
            {
                Log.Information(
                    $"[TCP Disconnect] PID={data.ProcessID} {data.saddr}:{data.sport} -> {data.daddr}:{data.dport}");
            };
            session.Source.Kernel.TcpIpSend += data =>
            {
                Log.Information(
                    $"[TCP Send] PID={data.ProcessID} {data.saddr}:{data.sport} -> {data.daddr}:{data.dport}, Size={data.size} bytes");
            };
            session.Source.Kernel.TcpIpRecv += data =>
            {
                Log.Information(
                    $"[TCP Recv] PID={data.ProcessID} {data.saddr}:{data.sport} <- {data.daddr}:{data.dport}, Size={data.size} bytes");
            };
            session.Source.Kernel.UdpIpSend += data =>
            {
                Log.Information(
                    $"[UDP Send] PID={data.ProcessID} {data.saddr}:{data.sport} -> {data.daddr}:{data.dport}, Size={data.size} bytes");
            };
            session.Source.Kernel.UdpIpRecv += data =>
            {
                Log.Information(
                    $"[UDP Recv] PID={data.ProcessID} {data.saddr}:{data.sport} <- {data.daddr}:{data.dport}, Size={data.size} bytes");
            };
            session.Source.Process(); // 开始监听
        }
    }

    /// <summary>
    /// 获取所有连接
    /// </summary>
    public static void GetAllTcpConnections()
    {
        int buffSize = 0;
        GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, 2, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
        IntPtr tcpTablePtr = Marshal.AllocHGlobal(buffSize);

        try
        {
            if (GetExtendedTcpTable(tcpTablePtr, ref buffSize, true, 2,
                    TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0) == 0)
            {
                int rowCount = Marshal.ReadInt32(tcpTablePtr);
                IntPtr rowPtr = IntPtr.Add(tcpTablePtr, 4);

                for (int i = 0; i < rowCount; i++)
                {
                    MIB_TCPROW_OWNER_PID row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
                    IPAddress localIp = new IPAddress(row.localAddr);
                    IPAddress remoteIp = new IPAddress(row.remoteAddr);
                    int localPort = ntohs((ushort)row.localPort);
                    int remotePort = ntohs((ushort)row.remotePort);

                    Log.Information(
                        $"PID: {row.owningPid}, {localIp}:{localPort} -> {remoteIp}:{remotePort}, State: {row.state}");
                    rowPtr = IntPtr.Add(rowPtr, Marshal.SizeOf<MIB_TCPROW_OWNER_PID>());
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(tcpTablePtr);
        }
    }

    /// <summary>
    /// 网络字节序转主机字节序
    /// </summary>
    /// <param name="netshort"></param>
    /// <returns></returns>
    private static ushort ntohs(ushort netshort) => (ushort)((netshort >> 8) | (netshort << 8));

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        TCP_TABLE_CLASS tableClass,
        uint reserved = 0);

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID
    {
        public uint state; // 状态：如 ESTABLISHED, LISTENING
        public uint localAddr; // 本地 IP
        public uint localPort; // 本地端口（需要字节序转换）
        public uint remoteAddr; // 远程 IP
        public uint remotePort; // 远程端口
        public int owningPid; // 对应的进程 PID
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPTABLE_OWNER_PID
    {
        public uint dwNumEntries; // 总连接数
        public MIB_TCPROW_OWNER_PID table; // 实际连接（数组）
    }


    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedUdpTable(
        IntPtr pUdpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        UDP_TABLE_CLASS tableClass,
        uint reserved = 0);


    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPROW_OWNER_PID
    {
        public uint localAddr; // 本地 IP
        public uint localPort; // 本地端口（需要字节序转换）
        public int owningPid; // 进程 PID
    }

    public static void GetAllUdpListeners()
    {
        int buffSize = 0;
        GetExtendedUdpTable(IntPtr.Zero, ref buffSize, true, 2, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
        IntPtr udpTablePtr = Marshal.AllocHGlobal(buffSize);

        try
        {
            if (GetExtendedUdpTable(udpTablePtr, ref buffSize, true, 2, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0) == 0)
            {
                int rowCount = Marshal.ReadInt32(udpTablePtr);
                IntPtr rowPtr = IntPtr.Add(udpTablePtr, 4);

                for (int i = 0; i < rowCount; i++)
                {
                    MIB_UDPROW_OWNER_PID row = Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(rowPtr);
                    IPAddress localIp = new IPAddress(row.localAddr);
                    int localPort = ntohs1((ushort)row.localPort);

                    Log.Information($"PID: {row.owningPid}, UDP {localIp}:{localPort}");
                    rowPtr = IntPtr.Add(rowPtr, Marshal.SizeOf<MIB_UDPROW_OWNER_PID>());
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(udpTablePtr);
        }
    }

    private static ushort ntohs1(ushort netshort) => (ushort)((netshort >> 8) | (netshort << 8));
}
