using Common.Logger;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace Common.Test.Logger;

public class LogTest
{
    [Test]
    public void Test1()
    {
        TaskTest();
        Assert.Pass();
    }

    async void TaskTest()
    {
        // ===== 使用方式1: 使用默认配置 =====
        using (var logger = new UniversalLogger())
        {
            logger.LogInformation("应用程序启动");
            logger.LogWarning("这是一个警告消息");

            try
            {
                throw new Exception("测试异常");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "捕获到异常: {Message}", ex.Message);
            }
        }

        // ===== 使用方式2: 自定义配置 =====
        var config = new LoggerConfig
        {
            EnableConsole = true,
            EnableFile = true,
            LogPath = "MyAppLogs",
            MaxFileSizeMB = 2,
            RetentionDays = 15,
            CompressDaysThreshold = 2,
            MinimumLevel = LogEventLevel.Debug,
            UseJsonFormat = false
        };

        using (var logger = new UniversalLogger(config))
        {
            logger.LogDebug("调试信息");
            logger.LogInformation("信息消息");
            logger.LogWarning("警告消息");
            logger.LogError("错误消息");

            // 带上下文信息的日志
            var context = new Dictionary<string, object>
            {
                ["UserId"] = 12345,
                ["Action"] = "Login",
                ["IP"] = "192.168.1.100"
            };
            logger.LogWithContext(LogEventLevel.Information, "用户登录", context);
        }

        // ===== 使用方式3: 从配置文件读取 =====
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        using (var logger = UniversalLogger.FromConfiguration(configuration, "Logging"))
        {
            logger.LogInformation("从配置文件创建的日志记录器");
        }

        // ===== 使用方式4: 静态日志记录器 =====
        // 初始化静态日志记录器
        Log.Initialize(config);

        Log.Information("使用静态日志记录器");
        Log.Warning("警告消息");
        Log.Error("错误消息");

        try
        {
            throw new InvalidOperationException("测试异常");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "静态日志记录器捕获异常");
        }

        // 手动执行维护任务
        await Log.RunMaintenanceAsync();
    }
}

// ===== 扩展示例: 自定义日志级别和过滤器 =====
public static class LoggerExtensions
{
    /// <summary>
    /// 记录性能日志
    /// </summary>
    public static void LogPerformance(this UniversalLogger logger, string operation, TimeSpan duration,
        Dictionary<string, object> additionalData = null)
    {
        var context = new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["Duration"] = duration.TotalMilliseconds,
            ["Category"] = "Performance"
        };

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        logger.LogWithContext(LogEventLevel.Information, "性能监控: {Operation} 耗时 {Duration}ms", context);
    }

    /// <summary>
    /// 记录业务日志
    /// </summary>
    public static void LogBusiness(this UniversalLogger logger, string action, string entityType, object entityId,
        Dictionary<string, object> details = null)
    {
        var context = new Dictionary<string, object>
        {
            ["Action"] = action,
            ["EntityType"] = entityType,
            ["EntityId"] = entityId,
            ["Category"] = "Business"
        };

        if (details != null)
        {
            foreach (var kvp in details)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        logger.LogWithContext(LogEventLevel.Information, "业务操作: {Action} {EntityType} {EntityId}", context);
    }

    /// <summary>
    /// 记录安全日志
    /// </summary>
    public static void LogSecurity(this UniversalLogger logger, string eventType, string userId, string ipAddress,
        bool success, string details = null)
    {
        var context = new Dictionary<string, object>
        {
            ["EventType"] = eventType,
            ["UserId"] = userId,
            ["IpAddress"] = ipAddress,
            ["Success"] = success,
            ["Category"] = "Security"
        };

        if (!string.IsNullOrEmpty(details))
        {
            context["Details"] = details;
        }

        var level = success ? LogEventLevel.Information : LogEventLevel.Warning;
        logger.LogWithContext(level, "安全事件: {EventType} 用户 {UserId} 来自 {IpAddress} {Result}",
            context,
            null);
    }
}

// ===== 使用扩展方法的示例 =====
public class ExtensionUsageExample
{
    private readonly UniversalLogger _logger;

    public ExtensionUsageExample()
    {
        _logger = new UniversalLogger();
    }

    public async Task ProcessUserOrder(int userId, int orderId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 记录业务日志
            _logger.LogBusiness("ProcessOrder", "Order", orderId, new Dictionary<string, object>
            {
                ["UserId"] = userId,
                ["Timestamp"] = DateTime.UtcNow
            });

            // 模拟业务处理
            await Task.Delay(1000);

            stopwatch.Stop();

            // 记录性能日志
            _logger.LogPerformance("ProcessUserOrder", stopwatch.Elapsed, new Dictionary<string, object>
            {
                ["UserId"] = userId,
                ["OrderId"] = orderId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理订单时发生错误: OrderId={OrderId}, UserId={UserId}", orderId, userId);
            throw;
        }
    }

    public void HandleUserLogin(string userId, string ipAddress, bool success)
    {
        _logger.LogSecurity("Login", userId, ipAddress, success,
            success ? "登录成功" : "登录失败");
    }
}
