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
        AppName = model.AppName ?? "MyApp";
        Version = model.Version ?? "1.0.0";
        MaxConnection = model.MaxConnection > 0 ? model.MaxConnection : 10;

        // 初始化配置（可以从文件或环境变量读取）
        AppName = "MyApp";
        Version = "1.0.0";
        MaxConnection = 10;
    }

    // 配置项：只读属性
    public string AppName { get; }
    public string Version { get; }
    public int MaxConnection { get; }

    // 可扩展其他读取配置的方法
}
