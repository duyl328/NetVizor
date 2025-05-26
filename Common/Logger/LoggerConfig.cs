using Serilog.Events;

namespace Common.Logger;


/// <summary>
/// 日志配置类
/// </summary>
public class LoggerConfig
{
    /// <summary>
    /// 是否启用控制台输出
    /// </summary>
    public bool EnableConsole { get; set; } = true;

    /// <summary>
    /// 是否启用文件输出
    /// </summary>
    public bool EnableFile { get; set; } = true;

    /// <summary>
    /// 日志文件路径
    /// </summary>
    public string LogPath { get; set; } = "logs";

    /// <summary>
    /// 单个日志文件最大大小(MB)
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 1;

    /// <summary>
    /// 日志保留天数
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// 压缩天数阈值(几天前的日志开始压缩)
    /// </summary>
    public int CompressDaysThreshold { get; set; } = 3;

    /// <summary>
    /// 最小日志级别
    /// </summary>
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

    /// <summary>
    /// 日志格式模板
    /// </summary>
    public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// 是否启用JSON格式
    /// </summary>
    public bool UseJsonFormat { get; set; } = false;

    /// <summary>
    /// 压缩检查间隔(小时)
    /// </summary>
    public int CompressCheckIntervalHours { get; set; } = 24;
}
