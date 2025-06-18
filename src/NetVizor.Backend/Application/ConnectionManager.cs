namespace Application;


// 4. 实现连接管理器
using System.Collections.Concurrent;
using System.Net.WebSockets;

public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public void AddConnection(string connectionId, WebSocket webSocket)
    {
        _connections.TryAdd(connectionId, webSocket);
    }

    public void RemoveConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var webSocket))
        {
            if (webSocket.State == WebSocketState.Open)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection removed", CancellationToken.None);
                    }
                    catch { /* 忽略关闭时的异常 */ }
                });
            }
        }
    }

    public WebSocket? GetConnection(string connectionId)
    {
        _connections.TryGetValue(connectionId, out var webSocket);
        return webSocket?.State == WebSocketState.Open ? webSocket : null;
    }

    public IEnumerable<string> GetAllConnectionIds()
    {
        return _connections.Where(c => c.Value.State == WebSocketState.Open).Select(c => c.Key);
    }

    public int Count => _connections.Count(c => c.Value.State == WebSocketState.Open);
}
