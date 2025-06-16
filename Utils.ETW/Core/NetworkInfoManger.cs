using System.Collections.Concurrent;
using System.Net;
using Utils.ETW.Models;

namespace Utils.ETW.Core;

/// <summary>
/// 网络信息管理【网络活动记录】
/// </summary>
public sealed class NetworkInfoManger
{
    /// <summary>
    /// 网络日志
    /// </summary>
    Queue<string> networkEvents = new();

    /// <summary>
    /// 最大日志记录条数
    /// </summary>
    public int NetworkEventsMaxSize = 1_000;

    /// <summary>
    /// 活跃的 TCP 连接
    /// </summary>
    private readonly ConcurrentDictionary<string, NetworkModel> _activeTcpSessions = new();
    
    private static readonly Lazy<NetworkInfoManger> _instance = new(() => new NetworkInfoManger());

    /// <summary>
    /// 端口流量记录
    /// </summary>
    public readonly Dictionary<int, ulong> PortTrafficSent = new();
    public readonly Dictionary<int, ulong> PortTrafficReceived = new();

    /// <summary>
    /// IP 流量记录
    /// </summary>
    public readonly Dictionary<IPAddress, ulong> SourceTrafficSent = new();
    public readonly Dictionary<IPAddress, ulong> SourceTrafficReceived = new();

    public static NetworkInfoManger Instance => _instance.Value;

    private NetworkInfoManger()
    {
    }


    #region TCP 缓存信息
    
    public void SetTcpCache(string key, NetworkModel value)
    {
        _activeTcpSessions[key] = value;
    }
    public void SetTcpCache(NetworkModel value)
    {
        _activeTcpSessions[value.GetKey()] = value;
    }
    public (bool tryRemove, NetworkModel? session) RemoveTcpCache(NetworkModel value)
    {
        return RemoveTcpCache(value.GetKey());
    }
    public (bool tryRemove, NetworkModel? session) RemoveTcpCache(string str)
    {
        var tryRemove = _activeTcpSessions.TryRemove(str, out var session);
        return (tryRemove, session);
    }
    public int GetTcpSize()
        => _activeTcpSessions.Count;

    #endregion
    
    /// <summary>
    /// 记录动作
    /// </summary>
    /// <param name="eventStr"></param>
    public void RecordEvent(string eventStr)
    {
        if (networkEvents.Count >= NetworkEventsMaxSize)
            networkEvents.Dequeue(); // 超出最大长度时，移除旧数据

        networkEvents.Enqueue(eventStr);
    }

    // todo: 2025年6月2日 22点00分 统计每个连接的时长、流量 （检测是否是僵尸进程【长连接，但低流量】）；统计当前活跃连接
    // todo: 2025年6月2日 22点01分 按照 端口、IP 统计流量
}
