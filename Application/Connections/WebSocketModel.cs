namespace Application.Connections;

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text.Json;
using Fleck;
using System.Timers;

// 主程序示例
public class Program
{
    public static void Main(string[] args)
    {
        var wsManager = WebSocketManager.Instance;
        var networkMonitor = new NetworkMonitorService();

        try
        {
            // 启动WebSocket服务器
            wsManager.Start("ws://127.0.0.1:8080");

            // 注册自定义处理器
            wsManager.RegisterHandler("customCommand", async (cmd, socket) =>
            {
                Console.WriteLine($"处理自定义命令: {cmd.Command}");
                await wsManager.SendToClient(socket.ConnectionInfo.Id, new ResponseMessage
                {
                    Type = "customResponse",
                    Success = true,
                    Message = "自定义命令处理完成"
                });
            });

            // 启动网络监控
            networkMonitor.StartMonitoring(3000);

            Console.WriteLine("服务器运行中，按任意键退出...");
            Console.ReadKey();
        }
        finally
        {
            networkMonitor.StopMonitoring();
            wsManager.Stop();
        }
    }
}
