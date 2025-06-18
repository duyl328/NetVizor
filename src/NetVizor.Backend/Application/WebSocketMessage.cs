namespace Application;


// 5. 消息模型
public class WebSocketMessage
{
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ConnectionId { get; set; }
}
