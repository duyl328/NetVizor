using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using Common.Logger;
using Fleck;

namespace Common.Net.WebSocketConn;

// WebSocket管理器 - 单例模式
public class WebSocketManager
{
    private static readonly Lazy<WebSocketManager> _instance = new(() => new WebSocketManager());
    public static WebSocketManager Instance => _instance.Value;

    private WebSocketServer _server;
    private readonly ConcurrentDictionary<Guid, IWebSocketConnection> _connections;
    private readonly object _lockObject = new();
    private bool _isStarted;

    // 消息处理器字典
    private readonly ConcurrentDictionary<string, Func<CommandMessage, IWebSocketConnection, Task>> _messageHandlers;

    private WebSocketManager()
    {
        _connections = new ConcurrentDictionary<Guid, IWebSocketConnection>();
        _messageHandlers = new ConcurrentDictionary<string, Func<CommandMessage, IWebSocketConnection, Task>>();
        RegisterDefaultHandlers();
    }

    // 启动WebSocket服务器
    public void Start(string url = "ws://127.0.0.1:8080")
    {
        lock (_lockObject)
        {
            if (_isStarted) return;

            _server = new WebSocketServer(url);
            _server.Start(socket =>
            {
                socket.OnOpen = () => OnConnectionOpen(socket);
                socket.OnClose = () => OnConnectionClose(socket);
                socket.OnMessage = message => OnMessageReceived(socket, message);
            });

            _isStarted = true;
            Log.Information($"WebSocket服务器已启动: {url}");
        }
    }

    // 停止服务器
    public void Stop()
    {
        lock (_lockObject)
        {
            if (!_isStarted) return;
            AppConfig.Instance.WebSocketPath = "";
            _server?.Dispose();
            CloseAllSocketsAsync();
            _connections.Clear();
            _isStarted = false;
            Log.Information("WebSocket服务器已停止");
        }
    }
    public async Task CloseAllSocketsAsync()
    {
        foreach (var webSocketConnection in _connections)
        {
            webSocketConnection.Value.Close();
        }
    }
    // 连接建立
    private void OnConnectionOpen(IWebSocketConnection socket)
    {
        _connections.TryAdd(socket.ConnectionInfo.Id, socket);
        Log.Information($"客户端连接: {socket.ConnectionInfo.Id}");

        // 发送欢迎消息
        _ = SendToClient(socket.ConnectionInfo.Id, new ResponseMessage
        {
            Type = "welcome",
            Success = true,
            Message = "连接成功",
            Data = new { ClientId = socket.ConnectionInfo.Id }
        });
    }

    // 连接关闭
    private void OnConnectionClose(IWebSocketConnection socket)
    {
        _connections.TryRemove(socket.ConnectionInfo.Id, out _);
        Log.Information($"客户端断开: {socket.ConnectionInfo.Id}");
    }

    // 接收消息
    private async void OnMessageReceived(IWebSocketConnection socket, string message)
    {
        try
        {
            Console.WriteLine(DateTime.Now);
            var commandMessage = JsonSerializer.Deserialize<CommandMessage>(message);
            if (commandMessage != null && !string.IsNullOrEmpty(commandMessage.Command))
            {
                await ProcessCommand(commandMessage, socket);
            }
        }
        catch (Exception ex)
        {
            Log.Information($"消息处理错误: {ex.Message}");
            await SendErrorResponse(socket, "消息格式错误");
        }
    }

    // 处理命令
    private async Task ProcessCommand(CommandMessage command, IWebSocketConnection socket)
    {
        if (_messageHandlers.TryGetValue(command.Command, out var handler))
        {
            await handler(command, socket);
        }
        else
        {
            await SendErrorResponse(socket, $"未知命令: {command.Command}");
        }
    }

    // 注册消息处理器
    public void RegisterHandler(string command, Func<CommandMessage, IWebSocketConnection, Task> handler)
    {
        _messageHandlers.AddOrUpdate(command, handler, (key, oldValue) => handler);
    }

    // 注册默认处理器
    private void RegisterDefaultHandlers()
    {
        // 获取网络状态
        RegisterHandler("getNetworkStatus", async (cmd, socket) =>
        {
            // cmd.Command;
            // 这里应该调用你的网络状态获取逻辑
            var networkStatus = GetNetworkStatus();
            await SendToClient(socket.ConnectionInfo.Id, new ResponseMessage
            {
                Type = "networkStatus",
                Success = true,
                Data = networkStatus
            });
        });

        // 获取防火墙规则
        RegisterHandler("getFirewallRules", async (cmd, socket) =>
        {
            var firewallRules = GetFirewallRules();
            await SendToClient(socket.ConnectionInfo.Id, new ResponseMessage
            {
                Type = "firewallRules",
                Success = true,
                Data = firewallRules
            });
        });

        // 添加防火墙规则
        RegisterHandler("addFirewallRule", async (cmd, socket) =>
        {
            try
            {
                var ruleData = JsonSerializer.Deserialize<FirewallRuleMessage>(cmd.Data.ToString());
                var success = AddFirewallRule(ruleData);

                await SendToClient(socket.ConnectionInfo.Id, new ResponseMessage
                {
                    Type = "addFirewallRuleResponse",
                    Success = success,
                    Message = success ? "规则添加成功" : "规则添加失败"
                });

                // 广播更新给所有客户端
                if (success)
                {
                    await BroadcastFirewallUpdate();
                }
            }
            catch (Exception ex)
            {
                await SendErrorResponse(socket, $"添加规则失败: {ex.Message}");
            }
        });

        // 心跳检测
        RegisterHandler("ping", async (cmd, socket) =>
        {
            await SendToClient(socket.ConnectionInfo.Id, new ResponseMessage
            {
                Type = "pong",
                Success = true,
                Message = "服务器正常"
            });
        });
    }

    // 发送消息给指定客户端
    public async Task<bool> SendToClient(Guid clientId, ResponseMessage message)
    {
        if (_connections.TryGetValue(clientId, out var socket))
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                await socket.Send(json);
                return true;
            }
            catch (Exception ex)
            {
                Log.Information($"发送消息失败: {ex.Message}");
                return false;
            }
        }

        return false;
    }

    // 广播消息给所有客户端
    public async Task BroadcastMessage(ResponseMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var tasks = new List<Task>();

        foreach (var connection in _connections.Values)
        {
            tasks.Add(connection.Send(json));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Log.Information($"广播消息失败: {ex.Message}");
        }
    }

    // 发送错误响应
    private async Task SendErrorResponse(IWebSocketConnection socket, string errorMessage)
    {
        await SendToClient(socket.ConnectionInfo.Id, new ResponseMessage
        {
            Type = "error",
            Success = false,
            Message = errorMessage
        });
    }

    // 获取连接数量
    public int GetConnectionCount() => _connections.Count;

    // 获取所有连接ID
    public Guid[] GetConnectionIds() => _connections.Keys.ToArray();

    #region 业务逻辑示例方法（需要根据实际需求实现）

    private object GetNetworkStatus()
    {
        // 实现网络状态获取逻辑
        return new[]
        {
            new NetworkStatusMessage
            {
                Type = "networkStatus",
                InterfaceName = "以太网",
                Status = "已连接",
                BytesSent = 1024000,
                BytesReceived = 2048000,
                IpAddress = "192.168.1.100"
            }
        };
    }

    private object GetFirewallRules()
    {
        // 实现防火墙规则获取逻辑
        return new[]
        {
            new FirewallRuleMessage
            {
                Type = "firewallRule",
                RuleName = "HTTP流量",
                Action = "允许",
                Protocol = "TCP",
                Port = "80",
                Enabled = true
            }
        };
    }

    private bool AddFirewallRule(FirewallRuleMessage rule)
    {
        // 实现添加防火墙规则逻辑
        Log.Information($"添加防火墙规则: {rule.RuleName}");
        return true; // 示例返回
    }

    private async Task BroadcastFirewallUpdate()
    {
        var rules = GetFirewallRules();
        await BroadcastMessage(new ResponseMessage
        {
            Type = "firewallRulesUpdate",
            Success = true,
            Data = rules
        });
    }

    #endregion
}
