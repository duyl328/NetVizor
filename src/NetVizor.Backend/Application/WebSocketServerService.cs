namespace Application;


// 6. 主要的WebSocket服务实现
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class WebSocketServerService : IWebSocketServerService, IDisposable
{
    private readonly IConnectionManager _connectionManager;
    private readonly Dictionary<string, IMessageHandler> _messageHandlers;
    private HttpListener? _httpListener;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly SemaphoreSlim _startStopSemaphore = new(1, 1);

    public event Action<string, string>? MessageReceived;
    public event Action<string>? ClientConnected;
    public event Action<string>? ClientDisconnected;
    public event Action<string>? ServerMessage;

    public bool IsRunning { get; private set; }
    public int ConnectionCount => _connectionManager.Count;

    public WebSocketServerService(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
        _messageHandlers = new Dictionary<string, IMessageHandler>();
    }

    // 注册消息处理器
    public void RegisterMessageHandler(IMessageHandler handler)
    {
        _messageHandlers[handler.MessageType.ToLower()] = handler;
    }

    public async Task<bool> StartAsync(string url)
    {
        await _startStopSemaphore.WaitAsync();
        try
        {
            if (IsRunning) return true;

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(url);
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                _httpListener.Start();
                IsRunning = true;
                ServerMessage?.Invoke($"WebSocket服务器已启动: {url}");

                // 开始监听连接
                _ = Task.Run(() => ListenForConnectionsAsync(_cancellationTokenSource.Token));
                return true;
            }
            catch (Exception ex)
            {
                ServerMessage?.Invoke($"启动服务器失败: {ex.Message}");
                return false;
            }
        }
        finally
        {
            _startStopSemaphore.Release();
        }
    }

    public async Task StopAsync()
    {
        await _startStopSemaphore.WaitAsync();
        try
        {
            if (!IsRunning) return;

            _cancellationTokenSource?.Cancel();
            _httpListener?.Stop();
            _httpListener?.Close();

            // 关闭所有连接
            foreach (var connectionId in _connectionManager.GetAllConnectionIds().ToList())
            {
                _connectionManager.RemoveConnection(connectionId);
            }

            IsRunning = false;
            ServerMessage?.Invoke("WebSocket服务器已停止");
        }
        finally
        {
            _startStopSemaphore.Release();
        }
    }

    private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _httpListener!.IsListening)
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                
                if (context.Request.IsWebSocketRequest)
                {
                    // 不等待，让连接处理在后台进行
                    _ = Task.Run(() => ProcessWebSocketRequestAsync(context, cancellationToken), cancellationToken);
                }
                else
                {
                    // 对非WebSocket请求返回404
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                ServerMessage?.Invoke($"监听连接时出错: {ex.Message}");
            }
        }
    }

    private async Task ProcessWebSocketRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        WebSocketContext? webSocketContext = null;
        string connectionId = Guid.NewGuid().ToString();

        try
        {
            webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;
            
            _connectionManager.AddConnection(connectionId, webSocket);
            ClientConnected?.Invoke(connectionId);
            ServerMessage?.Invoke($"客户端连接: {connectionId}");

            await HandleWebSocketConnectionAsync(webSocket, connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            ServerMessage?.Invoke($"处理WebSocket请求时出错 [{connectionId}]: {ex.Message}");
        }
        finally
        {
            _connectionManager.RemoveConnection(connectionId);
            ClientDisconnected?.Invoke(connectionId);
            ServerMessage?.Invoke($"客户端断开: {connectionId}");
        }
    }

    private async Task HandleWebSocketConnectionAsync(WebSocket webSocket, string connectionId, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessageReceived?.Invoke(connectionId, message);
                    
                    // 处理消息但不等待响应，避免阻塞接收循环
                    _ = Task.Run(() => ProcessMessageAsync(message, connectionId), cancellationToken);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 正常取消操作
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            // 客户端异常断开连接
        }
        catch (Exception ex)
        {
            ServerMessage?.Invoke($"处理WebSocket连接时出错 [{connectionId}]: {ex.Message}");
        }
    }

    private async Task ProcessMessageAsync(string message, string connectionId)
    {
        try
        {
            var messageObj = JsonSerializer.Deserialize<WebSocketMessage>(message);
            if (messageObj == null) return;

            messageObj.ConnectionId = connectionId;
            messageObj.Timestamp = DateTime.UtcNow;

            // 查找对应的消息处理器
            if (_messageHandlers.TryGetValue(messageObj.Type.ToLower(), out var handler))
            {
                var response = await handler.HandleAsync(connectionId, messageObj.Data);
                if (response != null)
                {
                    await SendMessageAsync(connectionId, response);
                }
            }
            else
            {
                var errorResponse = new { status = "error", message = $"未知消息类型: {messageObj.Type}" };
                await SendMessageAsync(connectionId, errorResponse);
            }
        }
        catch (JsonException)
        {
            var errorResponse = new { status = "error", message = "无效的JSON格式" };
            await SendMessageAsync(connectionId, errorResponse);
        }
        catch (Exception ex)
        {
            ServerMessage?.Invoke($"处理消息时出错 [{connectionId}]: {ex.Message}");
        }
    }

    public async Task SendMessageAsync(string connectionId, object message)
    {
        var webSocket = _connectionManager.GetConnection(connectionId);
        if (webSocket != null)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var buffer = Encoding.UTF8.GetBytes(json);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                ServerMessage?.Invoke($"发送消息失败 [{connectionId}]: {ex.Message}");
            }
        }
    }

    public async Task BroadcastMessageAsync(object message)
    {
        var json = JsonSerializer.Serialize(message);
        var tasks = new List<Task>();

        foreach (var connectionId in _connectionManager.GetAllConnectionIds())
        {
            tasks.Add(SendMessageAsync(connectionId, message));
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    public IEnumerable<string> GetConnectedClients()
    {
        return _connectionManager.GetAllConnectionIds();
    }

    public void Dispose()
    {
        _ = Task.Run(async () => await StopAsync());
        _startStopSemaphore?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}
