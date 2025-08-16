using System;
using System.Threading.Tasks;
using Application;
using Application.Utils;
using Common;
using Common.Logger;
using Common.Net.WebSocketConn;
using Common.Utils;
using Utils.ETW.Etw;
using Utils.ETW.Core;

namespace Shell.Services;

public class ServerStartupManager
{
    private readonly ApiRouteManager _apiRouteManager;
    private readonly StaticFileServer _staticFileServer;
    private HighPrecisionNetworkMonitor? _networkMonitor;

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

            Log.Info("高精度网络监控已启动 - 基于TCP连接表方案，解决ETW突发流量统计不准确问题");
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
    }

    #region 网络监控事件处理器

    /// <summary>
    /// 处理单个进程的网络统计更新
    /// </summary>
    private void OnProcessNetworkStatsUpdated(HighPrecisionNetworkMonitor.ProcessNetworkStats stats)
    {
        // 可以在这里将统计数据推送给前端或写入数据库
        Log.Debug($"进程网络统计更新: {stats}");

        // 示例：通过WebSocket推送给前端
        // WebSocketManager.Instance.BroadcastMessage("network_stats", stats);
    }

    /// <summary>
    /// 处理所有活跃进程的网络统计批量更新
    /// </summary>
    private void OnAllProcessStatsUpdated(List<HighPrecisionNetworkMonitor.ProcessNetworkStats> statsList)
    {
        Log.Debug($"批量网络统计更新: {statsList.Count} 个活跃进程");

        // 示例：可以在这里进行数据聚合或向前端推送汇总信息
        var totalSpeed = statsList.Sum(s => s.TotalBytesPerSecond);
        Log.Info($"系统总网速: {FormatSpeed(totalSpeed)}");
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

    #endregion
}
