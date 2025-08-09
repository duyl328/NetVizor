using Data.Models;
using Dapper;

namespace Data.Repositories;

public interface IAppSettingRepository
{
    Task<AppSetting?> GetCurrentSettingAsync();
    Task<int> SaveSettingAsync(AppSetting setting);
    Task<int> UpdateSettingAsync(AppSetting setting);
}

public class AppSettingRepository : IAppSettingRepository
{
    private readonly NetVizorDbContext _context;

    public AppSettingRepository(NetVizorDbContext context)
    {
        _context = context;
    }

    public async Task<AppSetting?> GetCurrentSettingAsync()
    {
        const string sql = "SELECT * FROM AppSetting ORDER BY UpdateTime DESC LIMIT 1";
        return await _context.Connection.QueryFirstOrDefaultAsync<AppSetting>(sql);
    }

    public async Task<int> SaveSettingAsync(AppSetting setting)
    {
        setting.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        const string sql = @"
            INSERT INTO AppSetting (
                WindowX, WindowY, IsClickThrough, IsPositionLocked, SnapToScreen, ShowDetailedInfo, 
                IsTopmost, TextColor, BackgroundColor, Opacity, SpeedUnit, LayoutDirection, 
                ShowUnit, DoubleClickAction, RunAsAdmin, AutoStart, ShowNetworkTopList, NetworkTopListCount, UpdateTime
            )
            VALUES (
                @WindowX, @WindowY, @IsClickThrough, @IsPositionLocked, @SnapToScreen, @ShowDetailedInfo,
                @IsTopmost, @TextColor, @BackgroundColor, @Opacity, @SpeedUnit, @LayoutDirection,
                @ShowUnit, @DoubleClickAction, @RunAsAdmin, @AutoStart, @ShowNetworkTopList, @NetworkTopListCount, @UpdateTime
            )";

        var result = await _context.Connection.ExecuteAsync(sql, setting);

        // 获取插入的ID
        const string getIdSql = "SELECT last_insert_rowid()";
        var id = await _context.Connection.QuerySingleAsync<int>(getIdSql);
        setting.Id = id;

        return result;
    }

    public async Task<int> UpdateSettingAsync(AppSetting setting)
    {
        setting.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        const string sql = @"
            UPDATE AppSetting 
            SET WindowX = @WindowX, WindowY = @WindowY, IsClickThrough = @IsClickThrough, 
                IsPositionLocked = @IsPositionLocked, SnapToScreen = @SnapToScreen, 
                ShowDetailedInfo = @ShowDetailedInfo, IsTopmost = @IsTopmost, 
                TextColor = @TextColor, BackgroundColor = @BackgroundColor, Opacity = @Opacity,
                SpeedUnit = @SpeedUnit, LayoutDirection = @LayoutDirection, ShowUnit = @ShowUnit,
                DoubleClickAction = @DoubleClickAction, RunAsAdmin = @RunAsAdmin, 
                AutoStart = @AutoStart, ShowNetworkTopList = @ShowNetworkTopList, NetworkTopListCount = @NetworkTopListCount, UpdateTime = @UpdateTime
            WHERE Id = @Id";

        return await _context.Connection.ExecuteAsync(sql, setting);
    }
}