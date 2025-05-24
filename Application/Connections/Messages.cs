namespace Application.Connections;

// 消息基类
public abstract class BaseMessage
{
    public string Type { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

// 网络状态消息
public class NetworkStatusMessage : BaseMessage
{
    public string InterfaceName { get; set; }
    public string Status { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public string IpAddress { get; set; }
}

// 防火墙规则消息
public class FirewallRuleMessage : BaseMessage
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
    public string Command { get; set; }
    public object Data { get; set; }
}

// 响应消息
public class ResponseMessage : BaseMessage
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
}
