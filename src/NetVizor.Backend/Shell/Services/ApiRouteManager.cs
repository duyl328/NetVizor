using System;
using System.Threading.Tasks;
using Common.Net.HttpConn;
using Common.Net.Models;
using Shell.Controllers;

namespace Shell.Services;

public class ApiRouteManager : IDisposable
{
    private HttpServer? _server;
    private readonly SubscriptionController _subscriptionController;
    private readonly FirewallController _firewallController;
    private readonly NetworkController _networkController;
    private readonly AppController _appController;
    private readonly SystemController _systemController;
    private bool _isDisposed;

    public ApiRouteManager()
    {
        _subscriptionController = new SubscriptionController();
        _firewallController = new FirewallController();
        _networkController = new NetworkController();
        _appController = new AppController();
        _systemController = new SystemController();
    }

    public async Task StartAsync(string serverPath)
    {
        _server = new HttpServer(serverPath);

        // 添加中间件
        _server.UseMiddleware(Middlewares.RequestLogging);
        _server.UseMiddleware(Middlewares.Cors);

        // 基础API
        _server.Get("/api", async (context) => { await context.Response.WriteJsonAsync(new { message = "Hi!" }); });

        // 订阅相关API
        RegisterSubscriptionRoutes();

        // 防火墙相关API
        RegisterFirewallRoutes();

        // 网络统计API
        RegisterNetworkRoutes();

        // 流量数据API
        RegisterTrafficRoutes();

        // 应用排行API
        RegisterAppRoutes();

        // 取消订阅API
        RegisterUnsubscribeRoutes();

        // 网络数据API
        RegisterNetworkDataRoutes();

        // 应用详细网络分析API
        RegisterAppAnalysisRoutes();

        try
        {
            await _server.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server error: {ex.Message}");
        }
    }

    private void RegisterSubscriptionRoutes()
    {
        _server.Post("/api/subscribe-application", _subscriptionController.SubscribeApplicationAsync);
        _server.Post("/api/subscribe-process", _subscriptionController.SubscribeProcessAsync);
        _server.Post("/api/subscribe-appinfo", _subscriptionController.SubscribeAppInfoAsync);
    }

    private void RegisterFirewallRoutes()
    {
        _server.Get("/api/firewall/rules", _firewallController.GetRulesAsync);
        _server.Post("/api/firewall/rules", _firewallController.CreateRuleAsync);
        _server.Put("/api/firewall/rules", _firewallController.UpdateRuleAsync);
        _server.Delete("/api/firewall/rules", _firewallController.DeleteRuleAsync);
        _server.Get("/api/firewall/status", _firewallController.GetStatusAsync);
        _server.Get("/api/firewall/statistics", _firewallController.GetStatisticsAsync);
        _server.Post("/api/firewall/switch", _firewallController.SwitchFirewallAsync);
    }

    private void RegisterNetworkRoutes()
    {
        _server.Get("/api/statistics/interfaces", _networkController.GetInterfacesAsync);
        _server.Get("/api/statistics/available-ranges", _networkController.GetAvailableRangesAsync);
        _server.Post("/api/statistics/clear-cache", _networkController.ClearCacheAsync);
    }

    private void RegisterTrafficRoutes()
    {
        _server.Get("/api/traffic/trends", _networkController.GetTrafficTrendsAsync);
        _server.Get("/api/traffic/top-apps", _networkController.GetTopAppsAsync);
    }

    private void RegisterAppRoutes()
    {
        _server.Get("/api/apps/top-traffic", _appController.GetTopTrafficAppsAsync);
    }

    private void RegisterUnsubscribeRoutes()
    {
        _server.Post("/api/unsubscribe", _subscriptionController.UnsubscribeAsync);
    }

    private void RegisterNetworkDataRoutes()
    {
        _server.Get("/api/network/global/realtime", _systemController.GetRealtimeNetworkDataAsync);
        _server.Get("/api/realtime/active-apps", _systemController.GetActiveAppsAsync);
        _server.Get("/api/system/info", _systemController.GetSystemInfoAsync);
        _server.Get("/api/system/collection-stats", _systemController.GetCollectionStatsAsync);
        _server.Get("/api/network/realtime-stats", _networkController.GetRealTimeStatsAsync);
    }

    private void RegisterAppAnalysisRoutes()
    {
        _server.Get("/api/apps/network-analysis", _appController.GetNetworkAnalysisAsync);
    }

    public void Stop()
    {
        _server?.Stop();
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            Stop();
            _server = null;
            _isDisposed = true;
        }
    }
}