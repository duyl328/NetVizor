using Microsoft.Extensions.Configuration;

namespace Common.Configuration;

public static class ConfigLoader
{
    private static readonly object _lock = new();
    private static AppConfig _config;

    public static AppConfig Config
    {
        get
        {
            if (_config == null)
            {
                lock (_lock)
                {
                    if (_config == null)
                    {
                        var builder = new ConfigurationBuilder()
                            .SetBasePath(AppContext.BaseDirectory)
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                        IConfiguration configuration = builder.Build();

                        // _config = new AppConfig();
                        configuration.GetSection("AppSettings").Bind(_config);
                    }
                }
            }
            return _config;
        }
    }
}
