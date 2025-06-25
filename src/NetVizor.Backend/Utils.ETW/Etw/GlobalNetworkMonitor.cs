using System.Collections.Concurrent;
using System.Net;
using Infrastructure.Models;
using Utils.ETW.Models;

namespace Utils.ETW.Etw;

/// <summary>
/// 全局网络监控数据管理器 - 单例模式
/// 负责收集、整合和管理所有网络监控数据
/// </summary>
public sealed class GlobalNetworkMonitor
{
    private static readonly Lazy<GlobalNetworkMonitor> _instance = new(() => new GlobalNetworkMonitor());
    public static GlobalNetworkMonitor Instance => _instance.Value;

    /// <summary>
    /// 读写锁，保证数据一致性
    /// </summary>
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// 应用程序信息缓存 (ProcessId -> AppInfo)
    /// </summary>
    private readonly ConcurrentDictionary<int, ApplicationInfo> _applications = new();

    /// <summary>
    /// 进程信息缓存 (ProcessId -> ProcessInfo)
    /// </summary>
    private readonly ConcurrentDictionary<int, ProcessNetworkInfo> _processes = new();

    /// <summary>
    /// 连接信息缓存 (ConnectionKey -> ConnectionInfo)
    /// </summary>
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();

    /// <summary>
    /// DNS解析缓存 (IP -> Domain)
    /// </summary>
    private readonly ConcurrentDictionary<string, DnsResolveInfo> _dnsCache = new();

    /// <summary>
    /// 端口流量统计
    /// </summary>
    private readonly ConcurrentDictionary<int, PortTrafficInfo> _portTraffic = new();

    /// <summary>
    /// IP流量统计
    /// </summary>
    private readonly ConcurrentDictionary<string, IpTrafficInfo> _ipTraffic = new();

    /// <summary>
    /// 统计信息更新时间
    /// </summary>
    private DateTime _lastUpdateTime = DateTime.Now;

    private GlobalNetworkMonitor()
    {
        // 初始化
    }

    #region 数据更新方法 (只能由ETW模块调用)

    /// <summary>
    /// 更新应用程序信息
    /// </summary>
    public void UpdateApplicationInfo(int processId, ProgramInfo info)
    {
        _lock.EnterWriteLock();
        try
        {
            _applications.AddOrUpdate(processId,
                new ApplicationInfo
                {
                    ProcessId = processId,
                    ProgramInfo = info,
                    FirstSeenTime = DateTime.Now,
                    LastUpdateTime = DateTime.Now
                },
                (key, existing) =>
                {
                    existing.LastUpdateTime = DateTime.Now;
                    return existing;
                });
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 更新进程网络信息
    /// </summary>
    public void UpdateProcessNetworkInfo(int processId, string processName, int threadId)
    {
        _lock.EnterWriteLock();
        try
        {
            _processes.AddOrUpdate(processId,
                new ProcessNetworkInfo
                {
                    ProcessId = processId,
                    ProcessName = processName,
                    FirstSeenTime = DateTime.Now,
                    LastActiveTime = DateTime.Now,
                    ActiveConnections = new List<string>()
                },
                (key, existing) =>
                {
                    existing.LastActiveTime = DateTime.Now;
                    return existing;
                });
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 更新连接信息
    /// </summary>
    public void UpdateConnectionInfo(NetworkModel networkModel)
    {
        _lock.EnterWriteLock();
        try
        {
            var connectionKey = networkModel.GetKey();

            _connections.AddOrUpdate(connectionKey,
                new ConnectionInfo
                {
                    ConnectionKey = connectionKey,
                    ProcessId = networkModel.ProcessId,
                    Protocol = networkModel.ConnectType,
                    LocalEndpoint = new IPEndPoint(networkModel.SourceIp, networkModel.SourcePort),
                    RemoteEndpoint = new IPEndPoint(networkModel.DestinationIp, networkModel.DestinationPort),
                    State = networkModel.State,
                    Direction = networkModel.Direction,
                    StartTime = networkModel.StartTime ?? DateTime.Now,
                    LastActiveTime = networkModel.LastSeenTime,
                    BytesSent = networkModel.BytesSent,
                    BytesReceived = networkModel.BytesReceived,
                    IsActive = networkModel.State == ConnectionState.Connected
                },
                (key, existing) =>
                {
                    existing.BytesSent = networkModel.BytesSent;
                    existing.BytesReceived = networkModel.BytesReceived;
                    existing.LastActiveTime = networkModel.LastSeenTime;
                    existing.State = networkModel.State;
                    existing.IsActive = networkModel.State == ConnectionState.Connected;

                    // 计算速率
                    var timeDiff = (networkModel.LastSeenTime - existing.LastSpeedCalculationTime).TotalSeconds;
                    if (timeDiff > 0)
                    {
                        existing.CurrentSendSpeed = (networkModel.BytesSent - existing.LastBytesSent) / timeDiff;
                        existing.CurrentReceiveSpeed =
                            (networkModel.BytesReceived - existing.LastBytesReceived) / timeDiff;
                        existing.LastBytesSent = networkModel.BytesSent;
                        existing.LastBytesReceived = networkModel.BytesReceived;
                        existing.LastSpeedCalculationTime = networkModel.LastSeenTime;
                    }

                    return existing;
                });

            // 更新进程的活跃连接列表
            if (_processes.TryGetValue(networkModel.ProcessId, out var process))
            {
                if (!process.ActiveConnections.Contains(connectionKey))
                {
                    process.ActiveConnections.Add(connectionKey);
                }
            }

            // 更新端口流量统计
            UpdatePortTraffic(networkModel.SourcePort, networkModel.BytesSent, networkModel.BytesReceived, true);
            UpdatePortTraffic(networkModel.DestinationPort, networkModel.BytesSent, networkModel.BytesReceived, false);

            // 更新IP流量统计
            UpdateIpTraffic(networkModel.SourceIp.ToString(), networkModel.BytesSent, networkModel.BytesReceived, true);
            UpdateIpTraffic(networkModel.DestinationIp.ToString(), networkModel.BytesSent, networkModel.BytesReceived,
                false);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 更新DNS解析信息
    /// </summary>
    public void UpdateDnsInfo(string ipAddress, string domainName, string queryType = "A")
    {
        _lock.EnterWriteLock();
        try
        {
            _dnsCache.AddOrUpdate(ipAddress,
                new DnsResolveInfo
                {
                    IpAddress = ipAddress,
                    DomainName = domainName,
                    QueryType = queryType,
                    ResolveTime = DateTime.Now,
                    LastUsedTime = DateTime.Now
                },
                (key, existing) =>
                {
                    existing.LastUsedTime = DateTime.Now;
                    if (!existing.AlternativeDomains.Contains(domainName))
                    {
                        existing.AlternativeDomains.Add(domainName);
                    }

                    return existing;
                });
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 移除连接
    /// </summary>
    public void RemoveConnection(string connectionKey)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_connections.TryRemove(connectionKey, out var connection))
            {
                // 从进程的活跃连接列表中移除
                if (_processes.TryGetValue(connection.ProcessId, out var process))
                {
                    process.ActiveConnections.Remove(connectionKey);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    #endregion

    #region 数据读取方法 (供其他模块调用)

    /// <summary>
    /// 获取完整的网络监控数据快照
    /// </summary>
    public NetworkMonitorSnapshot GetSnapshot()
    {
        _lock.EnterReadLock();
        try
        {
            var snapshot = new NetworkMonitorSnapshot
            {
                SnapshotTime = DateTime.Now,
                Applications = new List<ApplicationSnapshot>()
            };

            // 按应用程序组织数据
            var appGroups = _processes.GroupBy(p => p.Value.ProcessId);

            foreach (var group in appGroups)
            {
                var processId = group.Key;
                var processInfo = _processes[processId];

                // 获取应用程序信息
                _applications.TryGetValue(processId, out var appInfo);

                var appSnapshot = new ApplicationSnapshot
                {
                    ProcessId = processId,
                    ProgramInfo = appInfo?.ProgramInfo,
                    FirstSeenTime = processInfo.FirstSeenTime,
                    LastActiveTime = processInfo.LastActiveTime,
                    Connections = new List<ConnectionSnapshot>()
                };

                // 添加所有连接信息
                foreach (var connKey in processInfo.ActiveConnections)
                {
                    if (_connections.TryGetValue(connKey, out var conn))
                    {
                        var connSnapshot = new ConnectionSnapshot
                        {
                            Protocol = conn.Protocol.ToString(),
                            LocalPort = conn.LocalEndpoint.Port,
                            LocalIp = conn.LocalEndpoint.Address.ToString(),
                            RemoteIp = conn.RemoteEndpoint.Address.ToString(),
                            RemotePort = conn.RemoteEndpoint.Port,
                            RemoteDomain = GetDomainForIp(conn.RemoteEndpoint.Address.ToString()),
                            State = conn.State.ToString(),
                            Direction = conn.Direction.ToString(),
                            StartTime = conn.StartTime,
                            Duration = DateTime.Now - conn.StartTime,
                            BytesSent = conn.BytesSent,
                            BytesReceived = conn.BytesReceived,
                            CurrentSendSpeed = conn.CurrentSendSpeed,
                            CurrentReceiveSpeed = conn.CurrentReceiveSpeed,
                            IsActive = conn.IsActive
                        };

                        appSnapshot.Connections.Add(connSnapshot);
                    }
                }

                // 计算应用程序级别的统计
                appSnapshot.TotalConnections = appSnapshot.Connections.Count;
                appSnapshot.ActiveConnections = appSnapshot.Connections.Count(c => c.IsActive);
                appSnapshot.TotalBytesSent = appSnapshot.Connections.Sum(c => c.BytesSent);
                appSnapshot.TotalBytesReceived = appSnapshot.Connections.Sum(c => c.BytesReceived);
                appSnapshot.TotalSendSpeed = appSnapshot.Connections.Sum(c => c.CurrentSendSpeed);
                appSnapshot.TotalReceiveSpeed = appSnapshot.Connections.Sum(c => c.CurrentReceiveSpeed);

                snapshot.Applications.Add(appSnapshot);
            }

            // 添加全局统计信息
            snapshot.GlobalStats = new GlobalNetworkStats
            {
                TotalApplications = snapshot.Applications.Count,
                TotalConnections = _connections.Count,
                ActiveConnections = _connections.Count(c => c.Value.IsActive),
                PortTraffic = _portTraffic.Values.ToList(),
                IpTraffic = _ipTraffic.Values.OrderByDescending(i => i.TotalBytes).Take(100).ToList()
            };

            return snapshot;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 获取指定进程的网络信息
    /// </summary>
    public ProcessNetworkDetails GetProcessDetails(int processId)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_processes.TryGetValue(processId, out var processInfo))
                return null;

            _applications.TryGetValue(processId, out var appInfo);

            var details = new ProcessNetworkDetails
            {
                ProcessId = processId,
                ProcessName = processInfo.ProcessName,
                ApplicationInfo = appInfo,
                Connections = new List<ConnectionInfo>()
            };

            foreach (var connKey in processInfo.ActiveConnections)
            {
                if (_connections.TryGetValue(connKey, out var conn))
                {
                    details.Connections.Add(conn);
                }
            }

            return details;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 获取所有应用程序【有网络活动】
    /// </summary>
    /// <returns></returns>
    public List<ProgramInfo> GetAllPrograms()
    {
        return _applications.Values.ToList().Select(applicationInfo => applicationInfo.ProgramInfo).ToList();
    }

    #endregion

    #region 私有辅助方法

    private void UpdatePortTraffic(int port, long bytesSent, long bytesReceived, bool isLocal)
    {
        _portTraffic.AddOrUpdate(port,
            new PortTrafficInfo
            {
                Port = port,
                BytesSent = isLocal ? bytesSent : 0,
                BytesReceived = isLocal ? 0 : bytesReceived,
                LastUpdateTime = DateTime.Now
            },
            (key, existing) =>
            {
                if (isLocal)
                    existing.BytesSent = bytesSent;
                else
                    existing.BytesReceived = bytesReceived;
                existing.LastUpdateTime = DateTime.Now;
                return existing;
            });
    }

    private void UpdateIpTraffic(string ip, long bytesSent, long bytesReceived, bool isSource)
    {
        _ipTraffic.AddOrUpdate(ip,
            new IpTrafficInfo
            {
                IpAddress = ip,
                BytesSent = isSource ? bytesSent : 0,
                BytesReceived = isSource ? 0 : bytesReceived,
                LastUpdateTime = DateTime.Now
            },
            (key, existing) =>
            {
                if (isSource)
                    existing.BytesSent = bytesSent;
                else
                    existing.BytesReceived = bytesReceived;
                existing.LastUpdateTime = DateTime.Now;
                return existing;
            });
    }

    private string GetDomainForIp(string ip)
    {
        return _dnsCache.TryGetValue(ip, out var dnsInfo) ? dnsInfo.DomainName : null;
    }

    #endregion
}