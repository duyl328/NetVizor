using System.Collections.Concurrent;
using System.Net;
using Common.Logger;
using Utils.ETW.Core;
using Utils.ETW.Models;

namespace Utils.ETW.EtwTracker;

/// <summary>
/// TCP连接追踪器
/// </summary>
public class TcpTracker : INetTracker
{
    /// <summary>
    /// 最大连接数
    /// </summary>
    private readonly int _maxConnections = 10_000;

    /// <summary>
    /// 活跃会话
    /// </summary>
    private readonly ConcurrentDictionary<long, TcpConnectionSession> _activeSessions = new();

    public void SetupEtwHandlers(EtwNetworkCapture networkCapture)
    {
        // TCP事件需要单独详细处理
        networkCapture.OnTcpConnectionEvent += tcpEvent =>
        {
            ProcessTcpEvent(tcpEvent);

            // 额外的TCP特定检查
            CheckTcpSecurity(tcpEvent);
            MonitorTcpPerformance(tcpEvent);
        };
    }

    /// <summary>
    /// 安全性检查
    /// </summary>
    /// <param name="tcpEvent"></param>
    private void CheckTcpSecurity(TcpConnectionEventData tcpEvent)
    {
        // TCP安全检查
        if (tcpEvent.EventType == "TcpConnect")
        {
            // 检测端口扫描
            // 检测恶意外连
            // 检测异常连接模式
        }
    }

    /// <summary>
    /// 性能监视
    /// </summary>
    /// <param name="tcpEvent"></param>
    private void MonitorTcpPerformance(TcpConnectionEventData tcpEvent)
    {
        // TCP性能监控
        // 重传检测
        // 窗口大小分析
        // 连接建立延迟
    }

    /// <summary>
    /// 不同连接状态事件
    /// </summary>
    /// <param name="tcpEvent"></param>
    public void ProcessTcpEvent(TcpConnectionEventData tcpEvent)
    {
        Console.WriteLine($"[TCP] {tcpEvent.EventType}: {tcpEvent.SourceIp}:{tcpEvent.SourcePort} -> " +
                          $"{tcpEvent.DestinationIp}:{tcpEvent.DestinationPort} ({tcpEvent.ProcessName})");

        switch (tcpEvent.EventType)
        {
            // 连接建立 - 需要记录连接开始时间
            case "TcpConnect":
                StartSession(tcpEvent);
                break;
            // 连接断开 - 计算连接持续时间，统计传输数据量
            case "TcpDisconnect":
                EndSession(tcpEvent);
                break;
            // 数据传输 - 累计传输字节数，检测异常流量
            case "TcpSend":
                UpdateSentData(tcpEvent);
                break;
            case "TcpReceive":
                UpdateReceivedData(tcpEvent);
                break;
        }
    }

    /// <summary>
    /// 开始监听
    /// </summary>
    /// <param name="tcpEvent"></param>
    private void StartSession(TcpConnectionEventData tcpEvent)
    {
        // 连接建立时间
        var lastSeenTime = DateTime.Now;

        var networkModel = new NetworkModel
        {
            ConnectType = ProtocolType.TCP,
            ProcessId = tcpEvent.ProcessId,
            ThreadId = tcpEvent.ThreadId,
            ProcessName = tcpEvent.ProcessName,
            SourceIp = tcpEvent.SourceIp,
            DestinationIp = tcpEvent.DestinationIp,
            LastSeenTime = lastSeenTime,
            BytesSent = 0,
            BytesReceived = 0,
            State = ConnectionState.Connecting,
            SourcePort = tcpEvent.SourcePort,
            DestinationPort = tcpEvent.DestinationPort,
            StartTime = lastSeenTime,
            Direction = tcpEvent.Direction,
            RecordTime = lastSeenTime,
            IsPartialConnection = true,
        };

        var session = new TcpConnectionSession
        {
            ConnectionId = tcpEvent.ConnectionId,
            StartTime = tcpEvent.Timestamp,
            SourceIp = tcpEvent.SourceIp,
            DestinationIp = tcpEvent.DestinationIp,
            SourcePort = tcpEvent.SourcePort,
            DestinationPort = tcpEvent.DestinationPort,
            ProcessName = tcpEvent.ProcessName
        };
        NetworkInfoManger.Instance.SetTcpCache(networkModel);

        string str = $"连接建立: {networkModel.DestinationIp}:{networkModel.DestinationPort} ," +
                     $"地址: {networkModel.SourceIp}:{networkModel.SourcePort}";
        NetworkInfoManger.Instance.RecordEvent(str);

        // 业务逻辑处理
        AnalyzeNewConnection(session);
    }

    /// <summary>
    /// 结束会话
    /// </summary>
    /// <param name="tcpEvent"></param>
    private void EndSession(TcpConnectionEventData tcpEvent)
    {
        var key = NetworkModel.GetKey(
            tcpEvent.SourceIp,
            tcpEvent.SourcePort,
            tcpEvent.DestinationIp,
            tcpEvent.DestinationPort,
            tcpEvent.ProcessId,
            ProtocolType.TCP,
            tcpEvent.Timestamp
        );

        var removeTcpCache = NetworkInfoManger.Instance.RemoveTcpCache(key);

        if (removeTcpCache.tryRemove)
        {
            if (removeTcpCache.session == null)
            {
                Log.Warning("移除成功，但获取数据为 Null !");
                return;
            }

            removeTcpCache.session.EndTime = tcpEvent.Timestamp;
            removeTcpCache.session.State = ConnectionState.Disconnected;

            string str = $"连接断开: {removeTcpCache.session.DestinationIp}:{removeTcpCache.session.DestinationPort} ," +
                         $"地址: {removeTcpCache.session.SourceIp}:{removeTcpCache.session.SourcePort}";

            NetworkInfoManger.Instance.RecordEvent(str);
            // 连接结束时的分析
            // AnalyzeCompletedConnection(session);
        }
    }

    /// <summary>
    /// 更新发送的数据量
    /// </summary>
    /// <param name="tcpEvent"></param>
    private void UpdateSentData(TcpConnectionEventData tcpEvent)
    {
        var key = NetworkModel.GetKey(
            tcpEvent.SourceIp,
            tcpEvent.SourcePort,
            tcpEvent.DestinationIp,
            tcpEvent.DestinationPort,
            tcpEvent.ProcessId,
            ProtocolType.TCP,
            tcpEvent.Timestamp
        );
        var (tryRemove, networkModel) = NetworkInfoManger.Instance.RemoveTcpCache(key);

        if (tcpEvent.DataLength < 0)
        {
            throw new Exception("数据长度不可能为 0 !!");
        }

        // 统计端口流量
        NetworkInfoManger.Instance.PortTrafficSent.TryAdd(tcpEvent.DestinationPort, 0);
        NetworkInfoManger.Instance.PortTrafficSent[tcpEvent.DestinationPort] += (ulong)tcpEvent.DataLength;

        NetworkInfoManger.Instance.SourceTrafficSent.TryAdd(tcpEvent.SourceIp, 0);
        NetworkInfoManger.Instance.SourceTrafficSent[tcpEvent.SourceIp] += (ulong)tcpEvent.DataLength;

        if (tryRemove)
        {
            if (networkModel == null)
            {
                Log.Warning("发送新数据, 但是数据获取为空 !");
                return;
            }

            networkModel.BytesSent += tcpEvent.DataLength;
            networkModel.State = ConnectionState.Connected;
            networkModel.LastSeenTime = DateTime.Now;

            // todo: 检测 1 分钟、10 分钟的数据传输量；统计 1 分钟、10 分钟平均速率

            string str = $"发送: {networkModel.SourceIp}:{networkModel.SourcePort} {tcpEvent.DataLength} 字节";
            NetworkInfoManger.Instance.RecordEvent(str);
        }
    }

    /// <summary>
    /// 更新接收的数据量
    /// </summary>
    /// <param name="tcpEvent"></param>
    private void UpdateReceivedData(TcpConnectionEventData tcpEvent)
    {
        var key = NetworkModel.GetKey(
            tcpEvent.SourceIp,
            tcpEvent.SourcePort,
            tcpEvent.DestinationIp,
            tcpEvent.DestinationPort,
            tcpEvent.ProcessId,
            ProtocolType.TCP,
            tcpEvent.Timestamp
        );
        var (tryRemove, networkModel) = NetworkInfoManger.Instance.RemoveTcpCache(key);

        if (tcpEvent.DataLength < 0)
        {
            throw new Exception("数据长度不可能为 0 !!");
        }

        // 统计端口流量
        NetworkInfoManger.Instance.PortTrafficReceived.TryAdd(tcpEvent.DestinationPort, 0);
        NetworkInfoManger.Instance.PortTrafficReceived[tcpEvent.DestinationPort] += (ulong)tcpEvent.DataLength;

        NetworkInfoManger.Instance.SourceTrafficReceived.TryAdd(tcpEvent.SourceIp, 0);
        NetworkInfoManger.Instance.SourceTrafficReceived[tcpEvent.SourceIp] += (ulong)tcpEvent.DataLength;

        if (tryRemove)
        {
            if (networkModel == null)
            {
                Log.Warning("发送新数据, 但是数据获取为空 !");
                return;
            }

            networkModel.BytesReceived += tcpEvent.DataLength;
            networkModel.State = ConnectionState.Connected;
            networkModel.LastSeenTime = DateTime.Now;

            string str = $"接收: {networkModel.SourceIp}:{networkModel.SourcePort} {tcpEvent.DataLength} 字节";
            NetworkInfoManger.Instance.RecordEvent(str);
        }
    }

    /// <summary>
    /// 分析新连接
    /// </summary>
    /// <param name="session"></param>
    private void AnalyzeNewConnection(TcpConnectionSession session)
    {
        // 1. 安全检查
        // if (IsExternalConnection(session.DestinationIp))
        // {
        //     Console.WriteLine(
        //         $"[SECURITY] 外部连接警告: {session.ProcessName} -> {session.DestinationIp}:{session.DestinationPort}");
        // }

        // 2. 业务监控
        // if (IsBusinessCriticalPort(session.DestinationPort))
        // {
        //     Console.WriteLine($"[BUSINESS] 关键业务连接: {session.ProcessName} -> {session.DestinationPort}");
        // }

        // 3. 性能监控
        var tcpSize = NetworkInfoManger.Instance.GetTcpSize();
        if (tcpSize > 1000)
        {
            Console.WriteLine($"[PERFORMANCE] 连接数量警告: 当前活跃连接 {tcpSize}");
        }
    }

    /// <summary>
    /// 是否是连接到外部
    /// </summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    private bool IsExternalConnection(IPAddress ip)
    {
        // 简单检查是否为外部IP（实际应用中需要更复杂的逻辑）
        return !ip.ToString().StartsWith("192.168.") &&
               !ip.ToString().StartsWith("10.") &&
               !ip.ToString().StartsWith("172.") &&
               !IPAddress.IsLoopback(ip);
    }

    /// <summary>
    /// 是否是业务端口
    /// </summary>
    /// <param name="port">要监测的端口号</param>
    /// <returns></returns>
    private bool IsBusinessCriticalPort(int port)
    {
        var businessPorts = new[]
            { 80, 443, 3306, 5432, 1433, 6379 }; // HTTP, HTTPS, MySQL, PostgreSQL, SQL Server, Redis
        return businessPorts.Contains(port);
    }

    /// <summary>
    /// 获取活跃连接
    /// </summary>
    /// <returns></returns>
    public List<TcpConnectionSession> GetActiveSessions()
    {
        return _activeSessions.Values.ToList();
    }

    /// <summary>
    /// 获取长连接的会话
    /// </summary>
    /// <param name="threshold">连接时长</param>
    /// <returns></returns>
    public List<TcpConnectionSession> GetLongRunningSessions(TimeSpan threshold)
    {
        return _activeSessions.Values
            .Where(s => s.Duration > threshold)
            .ToList();
    }
}
