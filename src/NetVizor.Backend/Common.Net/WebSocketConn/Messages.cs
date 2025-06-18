using System.Text.Json.Serialization;

namespace Common.Net.WebSocketConn;

// 消息基类
public abstract class BaseMessage
{
    [JsonPropertyName("messageId")] public string? MessageId { get; set; } = Guid.NewGuid().ToString();
    [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; } = DateTime.Now;
}

// 网络状态消息
public class NetworkStatusMessage : NotificationMessage
{
    public string InterfaceName { get; set; }
    public string Status { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public string IpAddress { get; set; }
}

// 防火墙规则消息
public class FirewallRuleMessage : NotificationMessage
{
    public string RuleName { get; set; }
    public string Action { get; set; }
    public string Protocol { get; set; }
    public string Port { get; set; }
    public bool Enabled { get; set; }
}

// 命令消息
public class CommandMessage : BaseMessage
{
    [JsonPropertyName("command")] public string Command { get; set; }

    [JsonPropertyName("data")] public object Data { get; set; }
}

// 响应消息
public class ResponseMessage : NotificationMessage
{
    [JsonPropertyName("success")] public bool Success { get; set; }
}

/// <summary>
/// 通知消息 【服务端发往客户端的消息】
/// </summary>
public class NotificationMessage : BaseMessage
{
    [JsonPropertyName("type")] public string Type { get; set; } // 通知类型，如 "networkStatusUpdate", "firewallRuleChanged"
    [JsonPropertyName("data")] public object Data { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; }
}
