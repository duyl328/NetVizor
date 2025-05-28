using Common.Logger;
using Microsoft.Extensions.Configuration;

namespace Common.Configuration;

public static class ConfigReader
{
    public static AppConfigModel LoadConfig()
    {
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

        IConfiguration config = builder.Build();

        var model = new AppConfigModel();
        config.GetSection("AppSettings").Bind(model);
        return model;
    }
}

// 可单独定义模型类用于绑定
public class AppConfigModel
{
    public string AppName { get; set; }
    public string Version { get; set; }
    public int MaxConnection { get; set; }
    public LoggerConfig Logging { get; set; }
}
