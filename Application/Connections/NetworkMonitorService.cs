namespace Application.Connections;

public class NetworkMonitorService
{
    private System.Timers.Timer _monitorTimer;
    private readonly WebSocketManager _wsManager;

    public NetworkMonitorService()
    {
        _wsManager = WebSocketManager.Instance;
    }

    // 启动监控
    public void StartMonitoring(int intervalMs = 5000)
    {
        _monitorTimer = new System.Timers.Timer(intervalMs);
        _monitorTimer.Elapsed += async (sender, e) => await SendNetworkUpdate();
        _monitorTimer.AutoReset = true;
        _monitorTimer.Start();
        Console.WriteLine($"网络监控已启动，更新间隔: {intervalMs}ms");
    }

    // 停止监控
    public void StopMonitoring()
    {
        _monitorTimer?.Stop();
        _monitorTimer?.Dispose();
        Console.WriteLine("网络监控已停止");
    }

    // 发送网络状态更新
    private async Task SendNetworkUpdate()
    {
        if (_wsManager.GetConnectionCount() > 0)
        {
            var networkData = GetCurrentNetworkStatus();
            await _wsManager.BroadcastMessage(new ResponseMessage
            {
                Type = "networkStatusUpdate",
                Success = true,
                Data = networkData
            });
        }
    }

    private object GetCurrentNetworkStatus()
    {
        // 实现实时网络状态获取
        return new
        {
            timestamp = DateTime.Now,
            interfaces = new[]
            {
                new
                {
                    name = "以太网",
                    status = "已连接",
                    speed = "1000 Mbps",
                    bytesSent = new Random().Next(1000000, 5000000),
                    bytesReceived = new Random().Next(2000000, 8000000)
                }
            }
        };
    }
}
