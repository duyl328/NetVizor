using Common.Configuration;

namespace Common;

public sealed class AppConfig
{
    // 使用 Lazy<T> 实现线程安全的懒加载单例
    private static readonly Lazy<AppConfig> _instance = new Lazy<AppConfig>(() => new AppConfig());

    // 提供全局访问点
    public static AppConfig Instance => _instance.Value;

    // 私有构造函数，防止外部实例化
    private AppConfig()
    {
        var model = ConfigReader.LoadConfig();
        ConfigModel = model;
    }

    /// <summary>
    ///     全局配置
    /// </summary>
    public AppConfigModel ConfigModel { get; }

    /// <summary>
    /// WebSocket 端口
    /// </summary>
    public int WebSocketPort { get; set; }

    /// <summary>
    /// WebSocket 连接地址
    /// </summary>
    public string? WebSocketPath { get; set; }

    /// <summary>
    /// 订阅字符串
    /// </summary>
    public static readonly string ApplicationInfoSubscribe = "ApplicationInfo";

    public static readonly string ProcessInfoSubscribe = "ProcessInfo";
    public static readonly string AppDetailInfoSubscribe = "AppDetailInfo";
}