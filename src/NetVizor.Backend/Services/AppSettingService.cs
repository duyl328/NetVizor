using Data.Models;
using Data.Repositories;

namespace Services;

public interface IAppSettingService
{
    Task<AppSetting> GetSettingAsync();
    Task SaveSettingAsync(AppSetting setting);
    Task UpdateSettingAsync(AppSetting setting);
    Task InitializeDefaultSettingAsync();
}

public class AppSettingService : IAppSettingService
{
    private readonly IAppSettingRepository _repository;
    private AppSetting? _cachedSetting;
    private readonly object _lock = new object();

    public AppSettingService(IAppSettingRepository repository)
    {
        _repository = repository;
    }

    public async Task<AppSetting> GetSettingAsync()
    {
        if (_cachedSetting != null)
            return _cachedSetting;

        lock (_lock)
        {
            if (_cachedSetting != null)
                return _cachedSetting;
        }

        var setting = await _repository.GetCurrentSettingAsync();
        if (setting == null)
        {
            await InitializeDefaultSettingAsync();
            setting = await _repository.GetCurrentSettingAsync();
        }

        lock (_lock)
        {
            _cachedSetting = setting;
        }

        return setting ?? new AppSetting();
    }

    public async Task SaveSettingAsync(AppSetting setting)
    {
        await _repository.SaveSettingAsync(setting);
        
        lock (_lock)
        {
            _cachedSetting = setting;
        }
    }

    public async Task UpdateSettingAsync(AppSetting setting)
    {
        await _repository.UpdateSettingAsync(setting);
        
        lock (_lock)
        {
            _cachedSetting = setting;
        }
    }

    public async Task InitializeDefaultSettingAsync()
    {
        var defaultSetting = new AppSetting
        {
            WindowX = 0,
            WindowY = 0,
            IsClickThrough = false,
            IsPositionLocked = false,
            SnapToScreen = false,
            ShowDetailedInfo = false,
            IsTopmost = false,
            TextColor = "#FFFFFF",
            BackgroundColor = "#000000",
            Opacity = 100,
            SpeedUnit = 1, // KB/s
            LayoutDirection = 0, // 横向
            ShowUnit = true,
            DoubleClickAction = 0, // None
            RunAsAdmin = false,
            AutoStart = false,
            ShowSpeedRanking = false,
            SpeedRankingCount = 3
        };

        await _repository.SaveSettingAsync(defaultSetting);
        
        lock (_lock)
        {
            _cachedSetting = defaultSetting;
        }
    }

    public void InvalidateCache()
    {
        lock (_lock)
        {
            _cachedSetting = null;
        }
    }
}