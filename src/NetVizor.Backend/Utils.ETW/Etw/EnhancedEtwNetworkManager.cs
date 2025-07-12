using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Common.Logger;
using Infrastructure.utils;
using Utils.ETW.Core;
using Utils.ETW.Models;

namespace Utils.ETW.Etw;

/// <summary>
/// 增强的ETW网络管理器 - 负责收集网络数据并更新到GlobalNetworkMonitor
/// </summary>
public class EnhancedEtwNetworkManager : IDisposable
{
    private readonly EtwNetworkCapture _networkCapture = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    // 进程信息缓存
    private readonly ConcurrentDictionary<int, ProcessInfo> _processCache = new();

    // DNS 反向查询任务队列
    private readonly ConcurrentQueue<string> _dnsLookupQueue = new();

    private bool _isRunning = false;

    public EnhancedEtwNetworkManager()
    {
        SetupEventHandlers();
    }

    /// <summary>
    /// 设置事件处理器
    /// </summary>
    private void SetupEventHandlers()
    {
        // TCP事件处理
        _networkCapture.OnTcpConnectionEvent += HandleTcpEvent;

        // UDP事件处理
        _networkCapture.OnUdpPacketEvent += HandleUdpEvent;

        // DNS事件处理
        _networkCapture.OnDnsEvent += HandleDnsEvent;

        // HTTP事件处理
        _networkCapture.OnHttpEvent += HandleHttpEvent;
    }

    /// <summary>
    /// 开始监控
    /// </summary>
    public void StartMonitoring()
    {
        if (_isRunning) return;

        _isRunning = true;

        // 启动ETW捕获
        _networkCapture.StartCapture();

        // 启动辅助任务
        Task.Run(() => ProcessApplicationInfoAsync(_cancellationTokenSource.Token));
        Task.Run(() => ProcessDnsLookupsAsync(_cancellationTokenSource.Token));
        Task.Run(() => CleanupExpiredDataAsync(_cancellationTokenSource.Token));

        Log.Info("增强型网络监控已启动");
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public void StopMonitoring()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _cancellationTokenSource.Cancel();
        _networkCapture.StopCapture();

        Log.Info("增强型网络监控已停止");
    }

    #region 事件处理方法

    private void HandleTcpEvent(TcpConnectionEventData tcpEvent)
    {
        try
        {
            // 更新进程信息
            UpdateProcessInfo(tcpEvent.ProcessId, tcpEvent.ProcessName, tcpEvent.ThreadId);

            // 创建或更新网络模型
            var networkModel = new NetworkModel
            {
                ConnectType = ProtocolType.TCP,
                ProcessId = tcpEvent.ProcessId,
                ThreadId = tcpEvent.ThreadId,
                ProcessName = tcpEvent.ProcessName,
                SourceIp = tcpEvent.SourceIp,
                DestinationIp = tcpEvent.DestinationIp,
                SourcePort = tcpEvent.SourcePort,
                DestinationPort = tcpEvent.DestinationPort,
                LastSeenTime = DateTime.Now,
                RecordTime = tcpEvent.Timestamp,
                Direction = tcpEvent.Direction,
                IsPartialConnection = false,
                State = ConnectionState.Connecting,
                // 明确标识这是增量还是累积值
                BytesSent = tcpEvent.DataLength, // 如果是增量
                BytesReceived = tcpEvent.DataLength,
                IsIncrementalData = true // 新增标识
            };

            // 根据事件类型处理
            switch (tcpEvent.EventType)
            {
                case "TcpConnect":
                    networkModel.StartTime = tcpEvent.Timestamp;
                    networkModel.State = ConnectionState.Connecting;
                    break;

                case "TcpDisconnect":
                    networkModel.EndTime = tcpEvent.Timestamp;
                    networkModel.State = ConnectionState.Disconnected;
                    break;

                case "TcpSend":
                    networkModel.BytesSent = tcpEvent.DataLength;
                    networkModel.State = ConnectionState.Connected;
                    break;

                case "TcpReceive":
                    networkModel.BytesReceived = tcpEvent.DataLength;
                    networkModel.State = ConnectionState.Connected;
                    break;
            }

            // 更新到全局监控器
            GlobalNetworkMonitor.Instance.UpdateConnectionInfo(networkModel);

            // 添加到DNS查询队列
            if (!IsPrivateIp(tcpEvent.DestinationIp))
            {
                _dnsLookupQueue.Enqueue(tcpEvent.DestinationIp.ToString());
            }
        }
        catch (Exception ex)
        {
            Log.Info($"处理TCP事件时出错: {ex.Message}");
        }
    }

    private void HandleUdpEvent(UdpPacketEventData udpEvent)
    {
        try
        {
            // 更新进程信息
            UpdateProcessInfo(udpEvent.ProcessId, udpEvent.ProcessName, udpEvent.ThreadId);

            var networkModel = new NetworkModel
            {
                ConnectType = ProtocolType.UDP,
                ProcessId = udpEvent.ProcessId,
                ThreadId = udpEvent.ThreadId,
                ProcessName = udpEvent.ProcessName,
                SourceIp = udpEvent.SourceIp,
                DestinationIp = udpEvent.DestinationIp,
                SourcePort = udpEvent.SourcePort,
                DestinationPort = udpEvent.DestinationPort,
                StartTime = udpEvent.Timestamp,
                LastSeenTime = DateTime.Now,
                RecordTime = udpEvent.Timestamp,
                Direction = udpEvent.Direction,
                State = ConnectionState.Connected,
                IsPartialConnection = false
            };

            if (udpEvent.EventType == "UdpSend")
            {
                networkModel.BytesSent = udpEvent.DataLength;
            }
            else if (udpEvent.EventType == "UdpReceive")
            {
                networkModel.BytesReceived = udpEvent.DataLength;
            }

            // 更新到全局监控器
            GlobalNetworkMonitor.Instance.UpdateConnectionInfo(networkModel);

            // 添加到DNS查询队列
            if (!IsPrivateIp(udpEvent.DestinationIp))
            {
                _dnsLookupQueue.Enqueue(udpEvent.DestinationIp.ToString());
            }
        }
        catch (Exception ex)
        {
            Log.Info($"处理UDP事件时出错: {ex.Message}");
        }
    }

    private void HandleDnsEvent(DnsEventData dnsEvent)
    {
        try
        {
            // 更新DNS缓存
            if (!string.IsNullOrEmpty(dnsEvent.QueryName) && dnsEvent.ResolvedAddresses?.Count > 0)
            {
                foreach (var ip in dnsEvent.ResolvedAddresses)
                {
                    GlobalNetworkMonitor.Instance.UpdateDnsInfo(ip, dnsEvent.QueryName, dnsEvent.QueryType);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Info($"处理DNS事件时出错: {ex.Message}");
        }
    }

    private void HandleHttpEvent(HttpEventData httpEvent)
    {
        try
        {
            // 可以从HTTP事件中提取更多应用层信息
            Log.Info($"[HTTP] {httpEvent.HttpMethod} {httpEvent.Url}");
        }
        catch (Exception ex)
        {
            Log.Info($"处理HTTP事件时出错: {ex.Message}");
        }
    }

    #endregion

    #region 辅助方法

    private void UpdateProcessInfo(int processId, string processName, int threadId)
    {
        try
        {
            // 更新进程网络信息
            GlobalNetworkMonitor.Instance.UpdateProcessNetworkInfo(processId, processName, threadId);

            // 如果进程信息不在缓存中，添加到缓存并获取详细信息
            if (!_processCache.ContainsKey(processId))
            {
                _processCache.TryAdd(processId, new ProcessInfo
                {
                    ProcessId = processId,
                    ProcessName = processName,
                    NeedsUpdate = true
                });
            }
        }
        catch (Exception ex)
        {
            Log.Info($"更新进程信息时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 异步处理应用程序信息
    /// </summary>
    private async Task ProcessApplicationInfoAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var keyValuePairs = _processCache.Where(p => p.Value.NeedsUpdate);
                foreach (var kvp in keyValuePairs)
                {
                    var processInfo = kvp.Value;

                    try
                    {
                        // 获取进程详细信息
                        var inspectProcess = SysInfoUtils.InspectProcess(processInfo.ProcessId);
                        if (inspectProcess != null)
                        {
                            // 更新应用程序信息
                            GlobalNetworkMonitor.Instance.UpdateApplicationInfo(
                                processInfo.ProcessId,
                                inspectProcess
                            );

                            processInfo.NeedsUpdate = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        // 进程可能已经退出
                        Log.Info($"获取进程 {processInfo.ProcessId} 信息失败: {ex.Message}");
                        processInfo.NeedsUpdate = false;
                    }
                }

                await Task.Delay(1000, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Info($"处理应用程序信息时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 异步处理DNS查询
    /// </summary>
    private async Task ProcessDnsLookupsAsync(CancellationToken cancellationToken)
    {
        var processedIps = new HashSet<string>();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_dnsLookupQueue.TryDequeue(out var ip))
                {
                    if (!processedIps.Contains(ip))
                    {
                        processedIps.Add(ip);

                        try
                        {
                            // 执行反向DNS查询
                            var hostEntry = await Dns.GetHostEntryAsync(ip);
                            if (!string.IsNullOrEmpty(hostEntry.HostName))
                            {
                                GlobalNetworkMonitor.Instance.UpdateDnsInfo(ip, hostEntry.HostName, "PTR");
                            }
                        }
                        catch
                        {
                            // DNS查询失败，忽略
                        }
                    }
                }
                else
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Log.Info($"处理DNS查询时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 定期清理过期数据
    /// </summary>
    private async Task CleanupExpiredDataAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 清理断开的连接
                var snapshot = GlobalNetworkMonitor.Instance.GetSnapshot();
                foreach (var app in snapshot.Applications)
                {
                    foreach (var conn in app.Connections.Where(c =>
                                 !c.IsActive && c.Duration > TimeSpan.FromMinutes(5)))
                    {
                        GlobalNetworkMonitor.Instance.RemoveConnection(conn.ConnectionKey);
                        // 这里可以添加清理逻辑
                        Log.Info(
                            $"清理过期连接: {app.ProgramInfo?.ProductName} - {conn.RemoteIp}:{conn.RemotePort}");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Info($"清理过期数据时出错: {ex.Message}");
            }
        }
    }

    private bool IsPrivateIp(IPAddress ip)
    {
        if (ip == null) return true;

        var bytes = ip.GetAddressBytes();

        // 10.0.0.0 - 10.255.255.255
        if (bytes[0] == 10) return true;

        // 172.16.0.0 - 172.31.255.255
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;

        // 192.168.0.0 - 192.168.255.255
        if (bytes[0] == 192 && bytes[1] == 168) return true;

        // 127.0.0.0 - 127.255.255.255 (loopback)
        if (bytes[0] == 127) return true;

        return false;
    }

    #endregion

    public void Dispose()
    {
        StopMonitoring();
        _networkCapture?.Dispose();
        _cancellationTokenSource?.Dispose();
    }

    /// <summary>
    /// 进程信息缓存项
    /// </summary>
    private class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public bool NeedsUpdate { get; set; }
    }
}