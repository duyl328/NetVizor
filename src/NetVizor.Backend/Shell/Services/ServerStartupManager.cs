using System;
using System.Threading.Tasks;
using Application;
using Application.Utils;
using Common;
using Common.Logger;
using Common.Net.WebSocketConn;
using Common.Utils;
using Utils.ETW.Etw;

namespace Shell.Services;

public class ServerStartupManager
{
    private readonly ApiRouteManager _apiRouteManager;
    private readonly StaticFileServer _staticFileServer;

    public ServerStartupManager()
    {
        _apiRouteManager = new ApiRouteManager();
        _staticFileServer = new StaticFileServer();
    }

    public async Task StartAllServicesAsync()
    {
        await InitializeEtwMonitoringAsync();
        StartWebSocketServer();
        StartApiServer();
        StartStaticFileServer();
    }

    private async Task InitializeEtwMonitoringAsync()
    {
        if (!SysHelper.IsAdministrator())
        {
            Console.WriteLine("此程序需要管理员权限才能运行ETW监控！");
            Console.WriteLine("请以管理员身份重新运行程序。");
            Console.ReadKey();
            return;
        }

        var networkManager = new EnhancedEtwNetworkManager();
        networkManager.StartMonitoring();
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
        // int port = SysHelper.GetAvailablePort();
        int port = 3000;
        AppConfig.Instance.HttpPort = port;
        AppConfig.Instance.HttpPath = $"http://127.0.0.1:{port}";
        
        // _staticFileServer.Start(AppConfig.Instance.HttpPort);
    }

    public void Stop()
    {
        _staticFileServer?.Stop();
        _apiRouteManager?.Stop();
    }
}
