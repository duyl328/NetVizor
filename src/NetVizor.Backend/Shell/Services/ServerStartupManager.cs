using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Application;
using Application.Utils;
using Common;
using Common.Logger;
using Common.Net.WebSocketConn;
using Common.Utils;
using Utils.ETW.Etw;
using Utils.ETW.Core;
using Utils.ETW.Models;
using Infrastructure.Models;
using Infrastructure.utils;
using Data;
using Data.Models;
using Shell.Controllers;

namespace Shell.Services;

public class ServerStartupManager
{
    private readonly ApiRouteManager _apiRouteManager;
    private readonly StaticFileServer _staticFileServer;
    private HighPrecisionNetworkMonitor? _networkMonitor;
    private EnhancedEtwNetworkManager? _etwNetworkManager;

    public ServerStartupManager()
    {
        _apiRouteManager = new ApiRouteManager();
        _staticFileServer = new StaticFileServer();
    }

    public async Task StartAllServicesAsync()
    {
        await InitializeNetworkMonitoringAsync();
        StartWebSocketServer();
        StartApiServer();
        StartStaticFileServer();
    }

    private async Task InitializeNetworkMonitoringAsync()
    {
        if (!SysHelper.IsAdministrator())
        {
            Console.WriteLine("此程序需要管理员权限才能运行网络监控！");
            Console.WriteLine("请以管理员身份重新运行程序。");
            Console.ReadKey();
            return;
        }

        try
        {
            // 初始化高精度网络监控器（基于TCP连接表，替代ETW方案）
            _networkMonitor = new HighPrecisionNetworkMonitor(
                updateIntervalMs: 1000, // 1秒更新间隔
                enableDetailedStats: true // 启用详细统计（如果系统支持）
            );

            // 设置事件处理器
            _networkMonitor.OnProcessStatsUpdated += OnProcessNetworkStatsUpdated;
            _networkMonitor.OnAllProcessStatsUpdated += OnAllProcessStatsUpdated;
            _networkMonitor.OnPerformanceStatsUpdated += OnPerformanceStatsUpdated;

            // 启动监控
            _networkMonitor.Start();

            // 同时启动ETW监控以确保兼容性和数据完整性
            _etwNetworkManager = new EnhancedEtwNetworkManager();
            _etwNetworkManager.StartMonitoring();

            Log.Info("高精度网络监控已启动 - 基于TCP连接表方案，解决ETW突发流量统计不准确问题");
            Log.Info("ETW网络监控已启动 - 确保数据完整性和兼容性");

            // 设置NetworkController的ServerManager引用
            NetworkController.SetServerManager(this);
        }
        catch (Exception ex)
        {
            Log.Error($"初始化网络监控失败: {ex.Message}");
            throw;
        }
    }

    private void StartWebSocketServer()
    {
        int port = SysHelper.GetAvailablePort();
        AppConfig.Instance.WebSocketPort = port;
        AppConfig.Instance.WebSocketPath = $"ws://127.0.0.1:{port}";

        Log.Information4Ctx($"服务启动在端口: {AppConfig.Instance.WebSocketPort}");
        Log.Information($"服务完整地址: {AppConfig.Instance.WebSocketPath}");

        WebSocketManager.Instance.Start(AppConfig.Instance.WebSocketPath);

        WebSocketManager.Instance.SubscribeConnectionClosed((args =>
        {
            if (args.Uuid != null)
            {
                DispatchEngine.Instance.DeleteApplicationInfo(args.Uuid);
                DispatchEngine.Instance.DeleteProcessInfo(args.Uuid);
                DispatchEngine.Instance.DeleteAppDetailInfo(args.Uuid);
            }
        }));

        DispatchEngine.Instance.ApplicationInfoDistribute();
        DispatchEngine.Instance.ProcessInfoDistribute();
        DispatchEngine.Instance.AppDetailInfoDistribute();
    }

    private void StartApiServer()
    {
        int port = SysHelper.GetAvailablePort();
        AppConfig.Instance.HttpApiPort = port;
        AppConfig.Instance.HttpApiPath = $"http://127.0.0.1:{port}";

        Task.Run(() => _apiRouteManager.StartAsync(AppConfig.Instance.HttpApiPath));
    }

    private void StartStaticFileServer()
    {
        int port = SysHelper.GetAvailablePort();
        // int port = 3000;
        AppConfig.Instance.HttpPort = port;
        AppConfig.Instance.HttpPath = $"http://127.0.0.1:{port}";

        _staticFileServer.Start(AppConfig.Instance.HttpPort);
    }

    public void Stop()
    {
        _staticFileServer?.Stop();
        _apiRouteManager?.Stop();
        _networkMonitor?.Stop();
        _networkMonitor?.Dispose();
        _etwNetworkManager?.StopMonitoring();
        _etwNetworkManager?.Dispose();
    }

    #region 网络监控事件处理器

    /// <summary>
    /// 处理单个进程的网络统计更新
    /// </summary>
    private async void OnProcessNetworkStatsUpdated(HighPrecisionNetworkMonitor.ProcessNetworkStats stats)
    {
        try
        {
            // 将统计数据推送给前端
            var networkData = new
            {
                type = "process_network_stats",
                data = new
                {
                    processId = stats.ProcessId,
                    processName = stats.ProcessName,
                    connectionCount = stats.ConnectionCount,
                    bytesInPerSecond = stats.BytesInPerSecond,
                    bytesOutPerSecond = stats.BytesOutPerSecond,
                    totalBytesPerSecond = stats.TotalBytesPerSecond,
                    formattedDownloadSpeed = stats.FormattedDownloadSpeed,
                    formattedUploadSpeed = stats.FormattedUploadSpeed,
                    formattedTotalSpeed = stats.FormattedTotalSpeed,
                    lastUpdate = stats.LastUpdate
                }
            };

            // 通过WebSocket推送给前端
            var message = new Common.Net.WebSocketConn.ResponseMessage
            {
                Type = "network_stats",
                Data = networkData,
                Success = true
            };
            await WebSocketManager.Instance.BroadcastMessage(message);

            // 更新到GlobalNetworkMonitor以保持兼容性
            UpdateGlobalNetworkMonitorWithStats(stats);

            Log.Debug($"进程网络统计更新: {stats}");
        }
        catch (Exception ex)
        {
            Log.Error($"处理进程网络统计更新时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理所有活跃进程的网络统计批量更新
    /// </summary>
    private async void OnAllProcessStatsUpdated(List<HighPrecisionNetworkMonitor.ProcessNetworkStats> statsList)
    {
        try
        {
            Log.Debug($"批量网络统计更新: {statsList.Count} 个活跃进程");

            // 计算系统总流量
            var totalUploadSpeed = statsList.Sum(s => s.BytesOutPerSecond);
            var totalDownloadSpeed = statsList.Sum(s => s.BytesInPerSecond);
            var totalSpeed = totalUploadSpeed + totalDownloadSpeed;

            // 创建全局网络统计数据
            var globalStats = new
            {
                type = "global_network_stats",
                data = new
                {
                    activeProcessCount = statsList.Count,
                    totalUploadSpeed = totalUploadSpeed,
                    totalDownloadSpeed = totalDownloadSpeed,
                    totalSpeed = totalSpeed,
                    formattedUploadSpeed = FormatSpeed(totalUploadSpeed),
                    formattedDownloadSpeed = FormatSpeed(totalDownloadSpeed),
                    formattedTotalSpeed = FormatSpeed(totalSpeed),
                    timestamp = DateTime.Now,
                    topProcesses = statsList.OrderByDescending(s => s.TotalBytesPerSecond)
                        .Take(5)
                        .Select(s => new
                        {
                            processId = s.ProcessId,
                            processName = s.ProcessName,
                            speed = s.TotalBytesPerSecond,
                            formattedSpeed = s.FormattedTotalSpeed
                        })
                        .ToList()
                }
            };

            // 推送全局统计给前端
            var globalMessage = new Common.Net.WebSocketConn.ResponseMessage
            {
                Type = "global_network_stats",
                Data = globalStats,
                Success = true
            };
            await WebSocketManager.Instance.BroadcastMessage(globalMessage);

            // 存储全局网络数据到数据库
            _ = Task.Run(() => SaveGlobalNetworkStatsAsync(totalUploadSpeed, totalDownloadSpeed));

            Log.Info(
                $"系统总网速: {FormatSpeed(totalSpeed)} (↑{FormatSpeed(totalUploadSpeed)} ↓{FormatSpeed(totalDownloadSpeed)})");
        }
        catch (Exception ex)
        {
            Log.Error($"处理批量网络统计更新时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理监控性能统计更新
    /// </summary>
    private void OnPerformanceStatsUpdated(HighPrecisionNetworkMonitor.MonitoringPerformanceStats perfStats)
    {
        // 监控系统本身的性能，用于调试和优化
        if (perfStats.LastProcessingTimeMs > 100) // 如果处理时间超过100ms则记录警告
        {
            Log.Warning($"网络监控处理耗时较长: {perfStats}");
        }
    }

    /// <summary>
    /// 格式化网速显示
    /// </summary>
    private static string FormatSpeed(double bytesPerSec)
    {
        if (bytesPerSec >= 1024 * 1024 * 1024)
            return $"{bytesPerSec / (1024.0 * 1024 * 1024):F1} GB/s";
        if (bytesPerSec >= 1024 * 1024)
            return $"{bytesPerSec / (1024.0 * 1024):F1} MB/s";
        if (bytesPerSec >= 1024)
            return $"{bytesPerSec / 1024.0:F1} KB/s";
        return $"{bytesPerSec:F0} B/s";
    }

    /// <summary>
    /// 获取指定进程的网络统计（供外部调用）
    /// </summary>
    public HighPrecisionNetworkMonitor.ProcessNetworkStats? GetProcessNetworkStats(uint processId)
    {
        return _networkMonitor?.GetProcessStats(processId);
    }

    /// <summary>
    /// 获取所有活跃进程的网络统计（供外部调用）
    /// </summary>
    public List<HighPrecisionNetworkMonitor.ProcessNetworkStats> GetAllActiveProcessStats()
    {
        return _networkMonitor?.GetAllActiveProcessStats() ??
               new List<HighPrecisionNetworkMonitor.ProcessNetworkStats>();
    }

    /// <summary>
    /// 更新GlobalNetworkMonitor以保持兼容性
    /// </summary>
    private void UpdateGlobalNetworkMonitorWithStats(HighPrecisionNetworkMonitor.ProcessNetworkStats stats)
    {
        try
        {
            // 创建一个NetworkModel来更新GlobalNetworkMonitor
            var networkModel = new NetworkModel
            {
                ProcessId = (int)stats.ProcessId,
                ThreadId = 0, // 使用默认值
                ProcessName = stats.ProcessName,
                SourceIp = IPAddress.Loopback, // 使用默认值
                DestinationIp = IPAddress.Loopback, // 使用默认值 
                SourcePort = 0, // 使用默认值
                DestinationPort = 0, // 使用默认值
                ConnectType = ProtocolType.TCP, // 使用TCP
                IsPartialConnection = false, // 使用默认值
                BytesSent = (long)stats.BytesOutPerSecond, // 当前秒的发送字节数
                BytesReceived = (long)stats.BytesInPerSecond, // 当前秒的接收字节数
                LastSeenTime = DateTime.Now,
                RecordTime = stats.LastUpdate,
                IsIncrementalData = true,
                State = ConnectionState.Connected
            };

            // 更新到GlobalNetworkMonitor
            GlobalNetworkMonitor.Instance.UpdateConnectionInfo(networkModel);
        }
        catch (Exception ex)
        {
            Log.Error($"更新GlobalNetworkMonitor时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 异步保存全局网络统计到数据库
    /// </summary>
    private async Task SaveGlobalNetworkStatsAsync(double uploadSpeed, double downloadSpeed)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // 创建全局网络记录
            var globalNetwork = new GlobalNetwork
            {
                Timestep = timestamp,
                Upload = (long)uploadSpeed,
                Download = (long)downloadSpeed,
                NetworkCardGuid = "system_total" // 使用固定值表示系统总计
            };

            // 保存到数据库
            await DatabaseManager.Instance.Networks.SaveGlobalNetworkAsync(globalNetwork);
        }
        catch (Exception ex)
        {
            Log.Error($"保存全局网络统计到数据库时出错: {ex.Message}");
        }
    }

    #endregion
}