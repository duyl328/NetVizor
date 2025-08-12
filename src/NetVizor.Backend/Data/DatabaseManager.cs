using Data.Models;
using Data.Services;

namespace Data;

/// <summary>
/// 数据库管理器，提供应用程序数据访问的统一入口
/// </summary>
public static class DatabaseManager
{
    private static IDataService? _dataService;
    private static readonly object _lock = new object();

    /// <summary>
    /// 初始化数据库服务
    /// </summary>
    /// <param name="databasePath">数据库文件路径</param>
    public static async Task<IDataService> InitializeAsync(string? databasePath = null)
    {
        if (_dataService != null)
            return _dataService;

        lock (_lock)
        {
            if (_dataService != null)
                return _dataService;

            // 如果未指定路径，使用默认路径
            databasePath ??= GetDefaultDatabasePath();

            _dataService = new DataService(databasePath);
        }

        await _dataService.InitializeAsync();
        return _dataService;
    }

    /// <summary>
    /// 获取数据服务实例
    /// </summary>
    public static IDataService Instance
    {
        get
        {
            if (_dataService == null)
                throw new InvalidOperationException(
                    "Database service has not been initialized. Call InitializeAsync first.");
            return _dataService;
        }
    }

    /// <summary>
    /// 获取用户设置（用于应用启动时读取窗口位置等配置）
    /// </summary>
    public static async Task<AppSetting> GetUserSettingsAsync()
    {
        return await Instance.GetOrCreateDefaultSettingAsync();
    }

    /// <summary>
    /// 保存用户设置
    /// </summary>
    public static async Task SaveUserSettingsAsync(AppSetting settings)
    {
        await Instance.AppSettings.SaveSettingAsync(settings);
    }

    /// <summary>
    /// 检查数据库健康状态
    /// </summary>
    public static async Task<bool> IsHealthyAsync()
    {
        if (_dataService == null) return false;
        return await _dataService.IsHealthyAsync();
    }

    /// <summary>
    /// 获取默认数据库路径
    /// </summary>
    private static string GetDefaultDatabasePath()
    {
        // 获取运行文件所在目录
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var dbFolder = Path.Combine(exeDirectory, "db");

        if (!Directory.Exists(dbFolder))
            Directory.CreateDirectory(dbFolder);

        return Path.Combine(dbFolder, "netvizor.db");
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public static void Dispose()
    {
        if (_dataService is IDisposable disposable)
        {
            disposable.Dispose();
            _dataService = null;
        }
    }
}