using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Utils.ETW.Native;
using Common.Logger;

namespace Utils.ETW.Core;

/// <summary>
/// TCP连接枚举器 - 用于获取系统中所有TCP连接信息
/// 这是比ETW更精确的网络流量统计方案的核心组件
/// </summary>
public class TcpConnectionEnumerator
{
    private const int AF_INET = 2; // IPv4

    /// <summary>
    /// TCP连接信息数据模型
    /// </summary>
    public class TcpConnectionInfo
    {
        public uint ProcessId { get; set; }
        public string LocalAddress { get; set; } = "";
        public ushort LocalPort { get; set; }
        public string RemoteAddress { get; set; } = "";
        public ushort RemotePort { get; set; }
        public string State { get; set; } = "";
        public TcpConnectionAPI.MIB_TCP_STATE RawState { get; set; }

        /// <summary>
        /// 连接的唯一标识符（用于跟踪同一连接的统计变化）
        /// </summary>
        public string ConnectionKey => $"{ProcessId}_{LocalAddress}:{LocalPort}_{RemoteAddress}:{RemotePort}";

        /// <summary>
        /// 是否为已建立的连接（只有已建立的连接才有流量统计）
        /// </summary>
        public bool IsEstablished => RawState == TcpConnectionAPI.MIB_TCP_STATE.MIB_TCP_STATE_ESTAB;

        /// <summary>
        /// 获取MIB_TCPROW结构，用于后续的统计信息查询
        /// </summary>
        public TcpConnectionAPI.MIB_TCPROW GetTcpRow()
        {
            // 需要将IP地址和端口转换回网络字节序
            return new TcpConnectionAPI.MIB_TCPROW
            {
                dwLocalAddr = IpAddressToUint(LocalAddress),
                dwLocalPort = HostToNetworkOrder(LocalPort),
                dwRemoteAddr = IpAddressToUint(RemoteAddress),
                dwRemotePort = HostToNetworkOrder(RemotePort)
            };
        }

        private static uint IpAddressToUint(string ipAddress)
        {
            var parts = ipAddress.Split('.');
            if (parts.Length != 4) return 0;

            return (uint)(byte.Parse(parts[0]) |
                          (byte.Parse(parts[1]) << 8) |
                          (byte.Parse(parts[2]) << 16) |
                          (byte.Parse(parts[3]) << 24));
        }

        private static uint HostToNetworkOrder(ushort hostPort)
        {
            return (uint)((hostPort >> 8) | (hostPort << 8));
        }

        public override string ToString()
        {
            return $"PID:{ProcessId} {LocalAddress}:{LocalPort} -> {RemoteAddress}:{RemotePort} [{State}]";
        }
    }

    /// <summary>
    /// 获取系统中所有TCP连接
    /// </summary>
    /// <param name="onlyEstablished">是否只返回已建立的连接</param>
    /// <returns>TCP连接列表</returns>
    public static List<TcpConnectionInfo> GetTcpConnections(bool onlyEstablished = true)
    {
        var connections = new List<TcpConnectionInfo>();

        try
        {
            // 第一次调用获取所需缓冲区大小
            int bufferSize = 0;
            int result = TcpConnectionAPI.GetExtendedTcpTable(
                IntPtr.Zero,
                ref bufferSize,
                true, // 排序
                AF_INET,
                TcpConnectionAPI.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL,
                0);

            if (result != TcpConnectionAPI.ERROR_INSUFFICIENT_BUFFER)
            {
                Log.Error($"获取TCP表缓冲区大小失败: {result}");
                return connections;
            }

            // 分配缓冲区
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                // 第二次调用获取实际数据
                result = TcpConnectionAPI.GetExtendedTcpTable(
                    buffer,
                    ref bufferSize,
                    true,
                    AF_INET,
                    TcpConnectionAPI.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL,
                    0);

                if (result != TcpConnectionAPI.NO_ERROR)
                {
                    Log.Error($"获取TCP表数据失败: {result}");
                    return connections;
                }

                // 解析数据
                int entriesCount = Marshal.ReadInt32(buffer);
                Log.Debug($"发现 {entriesCount} 个TCP连接");

                IntPtr currentPtr = buffer + Marshal.SizeOf<uint>(); // 跳过entries count

                for (int i = 0; i < entriesCount; i++)
                {
                    var tcpRow = Marshal.PtrToStructure<TcpConnectionAPI.MIB_TCPROW_OWNER_PID>(currentPtr);

                    var connectionInfo = new TcpConnectionInfo
                    {
                        ProcessId = tcpRow.dwOwningPid,
                        LocalAddress = TcpConnectionAPI.UintToIpAddress(tcpRow.dwLocalAddr),
                        LocalPort = TcpConnectionAPI.NetworkToHostOrder(tcpRow.dwLocalPort),
                        RemoteAddress = TcpConnectionAPI.UintToIpAddress(tcpRow.dwRemoteAddr),
                        RemotePort = TcpConnectionAPI.NetworkToHostOrder(tcpRow.dwRemotePort),
                        State = TcpConnectionAPI.GetStateName(tcpRow.dwState),
                        RawState = tcpRow.dwState
                    };

                    // 过滤条件
                    if (onlyEstablished && !connectionInfo.IsEstablished)
                    {
                        currentPtr += Marshal.SizeOf<TcpConnectionAPI.MIB_TCPROW_OWNER_PID>();
                        continue;
                    }

                    connections.Add(connectionInfo);
                    currentPtr += Marshal.SizeOf<TcpConnectionAPI.MIB_TCPROW_OWNER_PID>();
                }

                Log.Info($"成功获取 {connections.Count} 个TCP连接" +
                         (onlyEstablished ? "（仅已建立）" : ""));
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"枚举TCP连接时发生异常: {ex.Message}");
        }

        return connections;
    }

    /// <summary>
    /// 按进程ID分组获取TCP连接
    /// </summary>
    /// <param name="onlyEstablished">是否只返回已建立的连接</param>
    /// <returns>按进程ID分组的TCP连接字典</returns>
    public static Dictionary<uint, List<TcpConnectionInfo>> GetTcpConnectionsByProcess(bool onlyEstablished = true)
    {
        var connectionsByProcess = new Dictionary<uint, List<TcpConnectionInfo>>();
        var allConnections = GetTcpConnections(onlyEstablished);

        foreach (var connection in allConnections)
        {
            if (!connectionsByProcess.ContainsKey(connection.ProcessId))
            {
                connectionsByProcess[connection.ProcessId] = new List<TcpConnectionInfo>();
            }

            connectionsByProcess[connection.ProcessId].Add(connection);
        }

        Log.Debug($"TCP连接按进程分组: {connectionsByProcess.Count} 个进程有活跃连接");
        return connectionsByProcess;
    }

    /// <summary>
    /// 获取指定进程的TCP连接
    /// </summary>
    /// <param name="processId">进程ID</param>
    /// <param name="onlyEstablished">是否只返回已建立的连接</param>
    /// <returns>指定进程的TCP连接列表</returns>
    public static List<TcpConnectionInfo> GetTcpConnectionsForProcess(uint processId, bool onlyEstablished = true)
    {
        var allConnections = GetTcpConnections(onlyEstablished);
        var processConnections = allConnections.Where(c => c.ProcessId == processId).ToList();

        Log.Debug($"进程 {processId} 有 {processConnections.Count} 个TCP连接");
        return processConnections;
    }

    /// <summary>
    /// 获取系统网络连接统计概要
    /// </summary>
    /// <returns>连接统计信息</returns>
    public static NetworkConnectionSummary GetConnectionSummary()
    {
        var allConnections = GetTcpConnections(false); // 获取所有状态的连接
        var summary = new NetworkConnectionSummary();

        var groupedByState = allConnections.GroupBy(c => c.RawState);
        foreach (var group in groupedByState)
        {
            var state = group.Key;
            var count = group.Count();

            switch (state)
            {
                case TcpConnectionAPI.MIB_TCP_STATE.MIB_TCP_STATE_ESTAB:
                    summary.EstablishedConnections = count;
                    break;
                case TcpConnectionAPI.MIB_TCP_STATE.MIB_TCP_STATE_LISTEN:
                    summary.ListeningConnections = count;
                    break;
                case TcpConnectionAPI.MIB_TCP_STATE.MIB_TCP_STATE_TIME_WAIT:
                    summary.TimeWaitConnections = count;
                    break;
            }
        }

        summary.TotalConnections = allConnections.Count;
        summary.ActiveProcesses = allConnections.Select(c => c.ProcessId).Distinct().Count();

        Log.Info($"网络连接概要: 总连接={summary.TotalConnections}, " +
                 $"已建立={summary.EstablishedConnections}, " +
                 $"监听={summary.ListeningConnections}, " +
                 $"活跃进程={summary.ActiveProcesses}");

        return summary;
    }

    /// <summary>
    /// 网络连接统计概要
    /// </summary>
    public class NetworkConnectionSummary
    {
        public int TotalConnections { get; set; }
        public int EstablishedConnections { get; set; }
        public int ListeningConnections { get; set; }
        public int TimeWaitConnections { get; set; }
        public int ActiveProcesses { get; set; }
    }
}