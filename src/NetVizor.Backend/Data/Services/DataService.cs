using Data.Core;
using Data.Models;
using Data.Repositories;

namespace Data.Services;

public interface IDataService
{
    Task InitializeAsync();
    Task<AppSetting> GetOrCreateDefaultSettingAsync();
    IAppSettingRepository AppSettings { get; }
    IAppInfoRepository AppInfos { get; }
    INetworkRepository Networks { get; }
    DatabaseWriteManager WriteManager { get; }
    NetworkDataCollectionService CollectionService { get; }
    Task<bool> IsHealthyAsync();
}

public class DataService : IDataService, IDisposable
{
    private readonly NetVizorDbContext _context;
    private readonly Lazy<IAppSettingRepository> _appSettingRepository;
    private readonly Lazy<IAppInfoRepository> _appInfoRepository;
    private readonly Lazy<INetworkRepository> _networkRepository;
    private readonly DatabaseWriteManager _writeManager;
    private readonly NetworkDataCollectionService _collectionService;

    public DataService(string databasePath)
    {
        _context = new NetVizorDbContext(databasePath);
        _appSettingRepository = new Lazy<IAppSettingRepository>(() => new AppSettingRepository(_context));
        _appInfoRepository = new Lazy<IAppInfoRepository>(() => new AppInfoRepository(_context));
        _networkRepository = new Lazy<INetworkRepository>(() => new NetworkRepository(_context));

        // 创建写入管理器和数据收集服务
        _writeManager = new DatabaseWriteManager(this);
        _collectionService = new NetworkDataCollectionService(_writeManager);
    }

    public IAppSettingRepository AppSettings => _appSettingRepository.Value;
    public IAppInfoRepository AppInfos => _appInfoRepository.Value;
    public INetworkRepository Networks => _networkRepository.Value;
    public DatabaseWriteManager WriteManager => _writeManager;
    public NetworkDataCollectionService CollectionService => _collectionService;

    public async Task InitializeAsync()
    {
        await _context.InitializeDatabaseAsync();
    }

    public async Task<AppSetting> GetOrCreateDefaultSettingAsync()
    {
        var setting = await AppSettings.GetCurrentSettingAsync();

        if (setting == null)
        {
            // 创建默认设置 - 与 SettingsWindow.xaml 中的默认值保持一致
            setting = new AppSetting
            {
                WindowX = 0, // 0表示使用默认位置
                WindowY = 0,
                IsClickThrough = false,
                IsPositionLocked = false,
                SnapToScreen = false,
                ShowDetailedInfo = false,
                IsTopmost = true, // 默认置顶
                TextColor = "#333333", // 与SettingsWindow.xaml一致
                BackgroundColor = "#F0FFFFFF", // 与SettingsWindow.xaml一致
                Opacity = 94, // 与SettingsWindow.xaml一致
                SpeedUnit = 3, // Auto - 与SettingsWindow.xaml一致
                LayoutDirection = 1, // 纵向 - 与SettingsWindow.xaml一致
                ShowUnit = true,
                DoubleClickAction = 0, // None
                RunAsAdmin = false,
                AutoStart = false,
                ShowNetworkTopList = false, // 默认关闭网速Top榜
                NetworkTopListCount = 3, // 默认显示3个
                UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await AppSettings.SaveSettingAsync(setting);
        }

        return setting;
    }

    public async Task<bool> IsHealthyAsync()
    {
        return await _context.IsHealthyAsync();
    }

    public void Dispose()
    {
        _collectionService?.Dispose();
        _writeManager?.Dispose();
        _context?.Dispose();
    }
}