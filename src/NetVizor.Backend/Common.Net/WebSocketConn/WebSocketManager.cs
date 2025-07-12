using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Web;
using Common.Logger;
using Common.Utils;
using Fleck;

namespace Common.Net.WebSocketConn;

// 连接关闭事件参数
public class ConnectionClosedEventArgs : EventArgs
{
    public Guid SocketId { get; init; }
    public string? Uuid { get; init; }
    public DateTime ClosedAt { get; init; } = DateTime.Now;
    public string? Reason { get; init; }
}

// 连接关闭委托
public delegate void ConnectionClosedHandler(ConnectionClosedEventArgs args);

// WebSocket管理器 - 单例模式
public class WebSocketManager
{
    private static readonly Lazy<WebSocketManager> _instance = new(() => new WebSocketManager());
    public static WebSocketManager Instance => _instance.Value;

    private WebSocketServer _server;

    /// <summary>
    /// 所有建立客户端连接
    /// </summary>
    private readonly ConcurrentDictionary<Guid, ClientConnection> _connections;

    /// <summary>
    /// 客户端私有 ID 与 socket id 对应表
    /// </summary>
    private readonly ConcurrentDictionary<string, Guid> _uuidToSocketId = new();

    private readonly object _lockObject = new();
    private bool _isStarted;

    // 消息处理器字典
    private readonly ConcurrentDictionary<string, Func<CommandMessage, IWebSocketConnection, Task>> _messageHandlers;

    /// <summary>
    /// 连接关闭事件
    /// </summary>
    public event ConnectionClosedHandler? ConnectionClosed;

    private WebSocketManager()
    {
        _connections = new ConcurrentDictionary<Guid, ClientConnection>();
        _messageHandlers = new ConcurrentDictionary<string, Func<CommandMessage, IWebSocketConnection, Task>>();
        RegisterDefaultHandlers();
    }

    #region 连接关闭委托相关方法

    /// <summary>
    /// 订阅连接关闭事件
    /// </summary>
    /// <param name="handler">事件处理器</param>
    public void SubscribeConnectionClosed(ConnectionClosedHandler handler)
    {
        ConnectionClosed += handler;
    }

    /// <summary>
    /// 取消订阅连接关闭事件
    /// </summary>
    /// <param name="handler">事件处理器</param>
    public void UnsubscribeConnectionClosed(ConnectionClosedHandler handler)
    {
        ConnectionClosed -= handler;
    }

    /// <summary>
    /// 触发连接关闭事件
    /// </summary>
    /// <param name="args">事件参数</param>
    private void OnConnectionClosedEvent(ConnectionClosedEventArgs args)
    {
        try
        {
            ConnectionClosed?.Invoke(args);
        }
        catch (Exception ex)
        {
            Log.Error($"处理连接关闭事件时出错: {ex.Message}");
        }
    }

    #endregion

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

    /// <summary>
    /// 压缩需要发送的数据
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    private static byte[] Compress(string json)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(json);
        using var outputStream = new MemoryStream();
        // leaveOpen: false，确保 DeflateStream 完全关闭时写入尾部
        using (var deflate = new DeflateStream(outputStream, CompressionLevel.Fastest))
        {
            deflate.Write(inputBytes, 0, inputBytes.Length);
        } // Dispose 在这里调用，确保数据写入完成

        return outputStream.ToArray();
    }

    private static byte[] CompressGzip(string json)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(json);
        using var outputStream = new MemoryStream();
        using (var gzip = new GZipStream(outputStream, CompressionLevel.Fastest))
        {
            gzip.Write(inputBytes, 0, inputBytes.Length);
        }

        return outputStream.ToArray();
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
            _uuidToSocketId.Clear();
            _isStarted = false;
            Log.Information("WebSocket服务器已停止");
        }
    }

    public async Task CloseAllSocketsAsync()
    {
        foreach (var webSocketConnection in _connections)
        {
            var socketConnection = webSocketConnection.Value.Socket;
            socketConnection.Close();
            var valueUuid = webSocketConnection.Value.Uuid;
            _uuidToSocketId.TryRemove(valueUuid, out _);

            // 触发连接关闭事件
            OnConnectionClosedEvent(new ConnectionClosedEventArgs
            {
                SocketId = webSocketConnection.Key,
                Uuid = valueUuid,
                Reason = "服务器关闭"
            });
        }
    }

    /// <summary>
    /// 主动关闭指定连接
    /// </summary>
    /// <param name="socketId">连接ID</param>
    /// <param name="reason">关闭原因</param>
    public async Task CloseConnection(Guid socketId, string reason = "主动关闭")
    {
        if (_connections.TryGetValue(socketId, out var connection))
        {
            connection.Socket.Close();

            // OnConnectionClose会被自动调用，但我们可以在这里预先记录关闭原因
            Log.Information($"主动关闭连接: {socketId}, 原因: {reason}");
        }
    }

    /// <summary>
    /// 主动关闭指定UUID的连接
    /// </summary>
    /// <param name="uuid">用户UUID</param>
    /// <param name="reason">关闭原因</param>
    public async Task CloseConnection(string uuid, string reason = "主动关闭")
    {
        if (_uuidToSocketId.TryGetValue(uuid, out var socketId))
        {
            await CloseConnection(socketId, reason);
        }
    }

    /// <summary>
    /// 连接建立
    /// </summary>
    /// <param name="socket"></param>
    private void OnConnectionOpen(IWebSocketConnection socket)
    {
        // 从Path中解析查询参数
        var path = socket.ConnectionInfo.Path;
        var userId = ExtractUserIdFromPath(path, "uuid");

        if (!string.IsNullOrEmpty(userId))
        {
            var conn = new ClientConnection
            {
                SocketId = socket.ConnectionInfo.Id,
                Uuid = userId,
                Socket = socket
            };
            _connections.TryAdd(socket.ConnectionInfo.Id, conn);
            Log.Info($"客户端连接: {socket.ConnectionInfo.Id}, 私有 id : {userId}");
            _uuidToSocketId.TryAdd(userId, socket.ConnectionInfo.Id);

            // 发送欢迎消息
            _ = SendToClient(socket.ConnectionInfo.Id, new ResponseMessage
            {
                Type = "welcome",
                Success = true,
                Message = "连接成功",
                Data = new { ClientId = socket.ConnectionInfo.Id }
            });
        }
        else
        {
            Log.Info("连接缺少userId参数");
            socket.Close();
        }
    }

    /// <summary>
    /// 解析参数
    /// </summary>
    /// <param name="path">要解析的路径</param>
    /// <param name="key">想要获取的目标 value </param>
    /// <returns></returns>
    private string? ExtractUserIdFromPath(string path, string key)
    {
        try
        {
            // 解析类似 "/?userId=123456" 的路径
            if (path.Contains("?"))
            {
                var queryString = path.Split('?')[1];
                var queryParams = HttpUtility.ParseQueryString(queryString);
                return queryParams[key];
            }
        }
        catch (Exception ex)
        {
            Log.Info($"解析userId时出错: {ex.Message}");
        }

        return null;
    }

    // 连接关闭
    private void OnConnectionClose(IWebSocketConnection socket)
    {
        _connections.TryRemove(socket.ConnectionInfo.Id, out var conn);
        string? uuid = null;

        if (conn?.Uuid != null)
        {
            uuid = conn.Uuid;
            _uuidToSocketId.TryRemove(conn.Uuid, out _);
        }

        Log.Information($"客户端断开: {socket.ConnectionInfo.Id}");

        // 触发连接关闭事件
        OnConnectionClosedEvent(new ConnectionClosedEventArgs
        {
            SocketId = socket.ConnectionInfo.Id,
            Uuid = uuid,
            Reason = "客户端断开连接"
        });
    }

    // 接收消息
    private async void OnMessageReceived(IWebSocketConnection socket, string message)
    {
        try
        {
            Log.Info($"{DateTime.Now}");
            var commandMessage = JsonHelper.FromJson<CommandMessage>(message);
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

    /// <summary>
    /// 发送消息给指定客户端
    /// </summary>
    public async Task<bool> SendToClient(Guid clientId, ResponseMessage message)
    {
        if (_connections.TryGetValue(clientId, out var socket))
        {
            try
            {
                var json = JsonHelper.ToJson(message);
                await socket.Socket.Send(CompressGzip(json));
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

    /// <summary>
    /// 发送消息给指定客户端
    /// </summary>
    public async Task<bool> SendToClient(string uuid, ResponseMessage message)
    {
        var tryGetValue = _uuidToSocketId.TryGetValue(uuid, out var socketId);
        var ans = false;
        if (tryGetValue)
        {
            ans = await SendToClient(socketId, message);
        }
        else
        {
            Log.Error4Ctx($"无法获取有效客户端 uuid !, uuid: {uuid}, socketId: {socketId}");
        }

        return ans;
    }

    /// <summary>
    /// 广播消息给所有客户端
    /// </summary>
    public async Task BroadcastMessage(ResponseMessage message)
    {
        var json = JsonHelper.ToJson(message);
        var tasks = new List<Task>();

        foreach (var connection in _connections.Values)
        {
            tasks.Add(connection.Socket.Send(CompressGzip(json)));
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

    /// <summary>
    /// 获取指定UUID的连接信息
    /// </summary>
    public ClientConnection? GetConnection(string uuid)
    {
        if (_uuidToSocketId.TryGetValue(uuid, out var socketId) &&
            _connections.TryGetValue(socketId, out var connection))
        {
            return connection;
        }

        return null;
    }

    /// <summary>
    /// 检查指定UUID的连接是否存在
    /// </summary>
    public bool IsConnected(string uuid)
    {
        return _uuidToSocketId.ContainsKey(uuid);
    }

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

public class ClientConnection
{
    public Guid SocketId { get; init; }
    public string Uuid { get; init; }
    public IWebSocketConnection Socket { get; init; }
}

// 使用示例类
public static class WebSocketManagerUsageExample
{
    public static void Initialize()
    {
        var wsManager = WebSocketManager.Instance;

        // 订阅连接关闭事件
        wsManager.SubscribeConnectionClosed(OnConnectionClosed);

        // 启动服务器
        wsManager.Start();
    }

    private static void OnConnectionClosed(ConnectionClosedEventArgs args)
    {
        Log.Info($"连接已关闭:");
        Log.Info($"  Socket ID: {args.SocketId}");
        Log.Info($"  UUID: {args.Uuid ?? "未知"}");
        Log.Info($"  关闭时间: {args.ClosedAt}");
        Log.Info($"  关闭原因: {args.Reason ?? "未知"}");

        // 在这里可以执行清理逻辑，比如：
        // - 清理用户相关的缓存数据
        // - 更新用户在线状态
        // - 记录用户离线时间
        // - 通知其他相关服务

        // 示例：清理用户数据
        if (!string.IsNullOrEmpty(args.Uuid))
        {
            CleanupUserData(args.Uuid);
        }
    }

    private static void CleanupUserData(string uuid)
    {
        // 实现用户数据清理逻辑
        Log.Info($"清理用户 {uuid} 的相关数据");
    }
}