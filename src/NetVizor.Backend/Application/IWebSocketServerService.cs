using System.Net.WebSockets;

namespace Application;

// 1. 服务接口定义 - 便于单元测试和依赖注入
public interface IWebSocketServerService
{
    event Action<string, string>? MessageReceived;
    event Action<string>? ClientConnected;
    event Action<string>? ClientDisconnected;
    event Action<string>? ServerMessage;

    Task<bool> StartAsync(string url);
    Task StopAsync();
    Task SendMessageAsync(string connectionId, object message);
    Task BroadcastMessageAsync(object message);
    bool IsRunning { get; }
    int ConnectionCount { get; }
    IEnumerable<string> GetConnectedClients();
}

// 2. 消息处理策略接口
public interface IMessageHandler
{
    string MessageType { get; }
    Task<object?> HandleAsync(string connectionId, string data);
}

// 3. 连接管理接口
public interface IConnectionManager
{
    void AddConnection(string connectionId, WebSocket webSocket);
    void RemoveConnection(string connectionId);
    WebSocket? GetConnection(string connectionId);
    IEnumerable<string> GetAllConnectionIds();
    int Count { get; }
}