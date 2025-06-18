using System.Text.Json;

namespace Application;


// 7. 示例消息处理器
public class ChatMessageHandler : IMessageHandler
{
    public string MessageType => "chat";

    public async Task<object?> HandleAsync(string connectionId, string data)
    {
        try
        {
            var chatData = JsonSerializer.Deserialize<ChatData>(data);
            if (chatData == null) return new { status = "error", message = "无效的聊天数据" };

            // 处理聊天逻辑
            return new
            {
                status = "success",
                message = "聊天消息已接收",
                echo = new
                {
                    from = connectionId,
                    content = chatData.Content,
                    timestamp = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            return new { status = "error", message = ex.Message };
        }
    }

    public class ChatData
    {
        public string Content { get; set; } = string.Empty;
    }
}
