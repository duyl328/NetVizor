using System.Data;
using System.Data.SQLite;
using Common.Logger;
using Dapper;

namespace Data;

public class NetVizorDbContext : IDisposable
{
    private readonly string _connectionString;
    private IDbConnection? _connection;

    public NetVizorDbContext(string databasePath)
    {
        _connectionString = $"Data Source={databasePath};Version=3;";
    }

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _connection = new SQLiteConnection(_connectionString);
                _connection.Open();

                // 启用WAL模式以支持多读
                _connection.Execute("PRAGMA journal_mode=WAL;");
                _connection.Execute("PRAGMA synchronous=NORMAL;");
                _connection.Execute("PRAGMA cache_size=10000;");
                _connection.Execute("PRAGMA temp_store=memory;");
                _connection.Execute("PRAGMA auto_vacuum = INCREMENTAL;");
            }

            return _connection;
        }
    }

    public async Task InitializeDatabaseAsync()
    {
        await CreateTablesAsync();
        await MigrateDatabaseAsync();
    }

    private async Task CreateTablesAsync()
    {
        var connection = Connection;

        // 创建AppInfo表 - 使用自增Id作为主键，AppId作为普通字段
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS AppInfo (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AppId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Path TEXT NOT NULL,
                Version TEXT,
                Company TEXT,
                Base64Icon TEXT,
                Hash TEXT,
                InsertTime INTEGER NOT NULL,
                UpdateTime INTEGER NOT NULL,
                DeleteTime INTEGER DEFAULT 0
            )");

        // 创建AppSetting表 - 完整结构
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS AppSetting (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                WindowX INTEGER DEFAULT 0,
                WindowY INTEGER DEFAULT 0,
                IsClickThrough INTEGER DEFAULT 0,
                IsPositionLocked INTEGER DEFAULT 0,
                SnapToScreen INTEGER DEFAULT 0,
                ShowDetailedInfo INTEGER DEFAULT 0,
                IsTopmost INTEGER DEFAULT 1,
                TextColor TEXT DEFAULT '#333333',
                BackgroundColor TEXT DEFAULT '#F0FFFFFF',
                Opacity INTEGER DEFAULT 94,
                SpeedUnit INTEGER DEFAULT 3,
                LayoutDirection INTEGER DEFAULT 1,
                ShowUnit INTEGER DEFAULT 1,
                DoubleClickAction INTEGER DEFAULT 0,
                RunAsAdmin INTEGER DEFAULT 0,
                AutoStart INTEGER DEFAULT 0,
                ShowNetworkTopList INTEGER DEFAULT 0,
                NetworkTopListCount INTEGER DEFAULT 3,
                UpdateTime INTEGER NOT NULL
            )");

        // 创建AppNetwork表 - 详细的网络记录，移除外键约束
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS AppNetwork (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp INTEGER NOT NULL,
                LocalIP TEXT NOT NULL,
                LocalPort INTEGER NOT NULL,
                RemoteIP TEXT NOT NULL,
                RemotePort INTEGER NOT NULL,
                Protocol TEXT NOT NULL,
                UploadBytes INTEGER DEFAULT 0,
                DownloadBytes INTEGER DEFAULT 0,
                AppId TEXT NOT NULL
            )");

        // 创建GlobalNetwork表
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS GlobalNetwork (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestep INTEGER NOT NULL,
                Upload INTEGER NOT NULL,
                Download INTEGER NOT NULL,
                NetworkCardGuid TEXT NOT NULL
            )");

        // 创建应用网络聚合数据表 - 按小时聚合
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS AppNetworkHourly (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AppId TEXT NOT NULL,
                HourTimestamp INTEGER NOT NULL,
                TotalUploadBytes INTEGER DEFAULT 0,
                TotalDownloadBytes INTEGER DEFAULT 0,
                ConnectionCount INTEGER DEFAULT 0,
                UniqueRemoteIPs INTEGER DEFAULT 0,
                UniqueRemotePorts INTEGER DEFAULT 0,
                CreatedTimestamp INTEGER NOT NULL,
                UNIQUE(AppId, HourTimestamp)
            )");

        // 创建应用网络聚合数据表 - 按天聚合
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS AppNetworkDaily (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AppId TEXT NOT NULL,
                DayTimestamp INTEGER NOT NULL,
                TotalUploadBytes INTEGER DEFAULT 0,
                TotalDownloadBytes INTEGER DEFAULT 0,
                ConnectionCount INTEGER DEFAULT 0,
                UniqueRemoteIPs INTEGER DEFAULT 0,
                UniqueRemotePorts INTEGER DEFAULT 0,
                CreatedTimestamp INTEGER NOT NULL,
                UNIQUE(AppId, DayTimestamp)
            )");

        // 创建应用网络聚合数据表 - 按周聚合
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS AppNetworkWeekly (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AppId TEXT NOT NULL,
                WeekTimestamp INTEGER NOT NULL,
                TotalUploadBytes INTEGER DEFAULT 0,
                TotalDownloadBytes INTEGER DEFAULT 0,
                ConnectionCount INTEGER DEFAULT 0,
                UniqueRemoteIPs INTEGER DEFAULT 0,
                UniqueRemotePorts INTEGER DEFAULT 0,
                CreatedTimestamp INTEGER NOT NULL,
                UNIQUE(AppId, WeekTimestamp)
            )");

        // 创建应用网络聚合数据表 - 按月聚合
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS AppNetworkMonthly (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AppId TEXT NOT NULL,
                MonthTimestamp INTEGER NOT NULL,
                TotalUploadBytes INTEGER DEFAULT 0,
                TotalDownloadBytes INTEGER DEFAULT 0,
                ConnectionCount INTEGER DEFAULT 0,
                UniqueRemoteIPs INTEGER DEFAULT 0,
                UniqueRemotePorts INTEGER DEFAULT 0,
                CreatedTimestamp INTEGER NOT NULL,
                UNIQUE(AppId, MonthTimestamp)
            )");

        // 创建全局网络聚合数据表 - 按小时聚合
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS GlobalNetworkHourly (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NetworkCardGuid TEXT NOT NULL,
                HourTimestamp INTEGER NOT NULL,
                TotalUpload INTEGER DEFAULT 0,
                TotalDownload INTEGER DEFAULT 0,
                AvgUpload INTEGER DEFAULT 0,
                AvgDownload INTEGER DEFAULT 0,
                MaxUpload INTEGER DEFAULT 0,
                MaxDownload INTEGER DEFAULT 0,
                RecordCount INTEGER DEFAULT 0,
                CreatedTimestamp INTEGER NOT NULL,
                UNIQUE(NetworkCardGuid, HourTimestamp)
            )");

        // 创建全局网络聚合数据表 - 按天聚合
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS GlobalNetworkDaily (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NetworkCardGuid TEXT NOT NULL,
                DayTimestamp INTEGER NOT NULL,
                TotalUpload INTEGER DEFAULT 0,
                TotalDownload INTEGER DEFAULT 0,
                AvgUpload INTEGER DEFAULT 0,
                AvgDownload INTEGER DEFAULT 0,
                MaxUpload INTEGER DEFAULT 0,
                MaxDownload INTEGER DEFAULT 0,
                RecordCount INTEGER DEFAULT 0,
                CreatedTimestamp INTEGER NOT NULL,
                UNIQUE(NetworkCardGuid, DayTimestamp)
            )");

        // 创建全局网络聚合数据表 - 按周聚合
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS GlobalNetworkWeekly (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NetworkCardGuid TEXT NOT NULL,
                WeekTimestamp INTEGER NOT NULL,
                TotalUpload INTEGER DEFAULT 0,
                TotalDownload INTEGER DEFAULT 0,
                AvgUpload INTEGER DEFAULT 0,
                AvgDownload INTEGER DEFAULT 0,
                MaxUpload INTEGER DEFAULT 0,
                MaxDownload INTEGER DEFAULT 0,
                RecordCount INTEGER DEFAULT 0,
                CreatedTimestamp INTEGER NOT NULL,
                UNIQUE(NetworkCardGuid, WeekTimestamp)
            )");

        // 创建全局网络聚合数据表 - 按月聚合
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS GlobalNetworkMonthly (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NetworkCardGuid TEXT NOT NULL,
                MonthTimestamp INTEGER NOT NULL,
                TotalUpload INTEGER DEFAULT 0,
                TotalDownload INTEGER DEFAULT 0,
                AvgUpload INTEGER DEFAULT 0,
                AvgDownload INTEGER DEFAULT 0,
                MaxUpload INTEGER DEFAULT 0,
                MaxDownload INTEGER DEFAULT 0,
                RecordCount INTEGER DEFAULT 0,
                CreatedTimestamp INTEGER NOT NULL,
                UNIQUE(NetworkCardGuid, MonthTimestamp)
            )");

        // 创建索引以提高查询性能
        await connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_appinfo_appid ON AppInfo(AppId)");
        await connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_appnetwork_appid ON AppNetwork(AppId)");
        await connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_appnetwork_timestamp ON AppNetwork(Timestamp)");
        await connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_appnetwork_remoteip ON AppNetwork(RemoteIP)");
        await connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_appnetwork_remoteport ON AppNetwork(RemotePort)");

        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS idx_globalnetwork_timestep ON GlobalNetwork(Timestep)");
        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS idx_globalnetwork_guid ON GlobalNetwork(NetworkCardGuid)");

        // 应用网络聚合表索引
        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS idx_appnetwork_hourly_timestamp ON AppNetworkHourly(HourTimestamp)");
        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS idx_appnetwork_daily_timestamp ON AppNetworkDaily(DayTimestamp)");
        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS idx_appnetwork_weekly_timestamp ON AppNetworkWeekly(WeekTimestamp)");
        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS idx_appnetwork_monthly_timestamp ON AppNetworkMonthly(MonthTimestamp)");

        // 全局网络聚合表索引
        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS idx_globalnetwork_hourly_timestamp ON GlobalNetworkHourly(HourTimestamp)");
        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS idx_globalnetwork_daily_timestamp ON GlobalNetworkDaily(DayTimestamp)");
        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS idx_globalnetwork_weekly_timestamp ON GlobalNetworkWeekly(WeekTimestamp)");
        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS idx_globalnetwork_monthly_timestamp ON GlobalNetworkMonthly(MonthTimestamp)");
    }

    private async Task MigrateDatabaseAsync()
    {
        var connection = Connection;

        try
        {
            // 检查是否需要迁移AppSetting表
            var columns = await GetTableColumnsAsync("AppSetting");
            var needsMigration = columns.Contains("WindowWidth") || columns.Contains("WindowHeight") ||
                                 columns.Contains("Theme") || columns.Contains("Language") ||
                                 columns.Contains("MonitoringEnabled") ||
                                 !columns.Contains("IsClickThrough") || !columns.Contains("TextColor") ||
                                 !columns.Contains("ShowNetworkTopList") || !columns.Contains("NetworkTopListCount");

            if (needsMigration)
            {
                Log.Information("检测到AppSetting表需要迁移，开始迁移...");

                // 备份数据
                var existingSettings = await connection.QueryAsync(@"
                    SELECT Id, WindowX, WindowY, AutoStart, UpdateTime FROM AppSetting");

                // 删除旧表
                await connection.ExecuteAsync("DROP TABLE AppSetting");

                // 创建新表
                await connection.ExecuteAsync(@"
                    CREATE TABLE AppSetting (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        WindowX INTEGER DEFAULT 0,
                        WindowY INTEGER DEFAULT 0,
                        IsClickThrough INTEGER DEFAULT 0,
                        IsPositionLocked INTEGER DEFAULT 0,
                        SnapToScreen INTEGER DEFAULT 0,
                        ShowDetailedInfo INTEGER DEFAULT 0,
                        IsTopmost INTEGER DEFAULT 1,
                        TextColor TEXT DEFAULT '#333333',
                        BackgroundColor TEXT DEFAULT '#F0FFFFFF',
                        Opacity INTEGER DEFAULT 94,
                        SpeedUnit INTEGER DEFAULT 3,
                        LayoutDirection INTEGER DEFAULT 1,
                        ShowUnit INTEGER DEFAULT 1,
                        DoubleClickAction INTEGER DEFAULT 0,
                        RunAsAdmin INTEGER DEFAULT 0,
                        AutoStart INTEGER DEFAULT 0,
                        ShowNetworkTopList INTEGER DEFAULT 0,
                        NetworkTopListCount INTEGER DEFAULT 3,
                        UpdateTime INTEGER NOT NULL
                    )");

                // 恢复数据
                foreach (var setting in existingSettings)
                {
                    await connection.ExecuteAsync(@"
                        INSERT INTO AppSetting (WindowX, WindowY, AutoStart, UpdateTime)
                        VALUES (@WindowX, @WindowY, @AutoStart, @UpdateTime)",
                        new
                        {
                            WindowX = (int)((dynamic)setting).WindowX,
                            WindowY = (int)((dynamic)setting).WindowY,
                            AutoStart = (int)((dynamic)setting).AutoStart,
                            UpdateTime = (long)((dynamic)setting).UpdateTime
                        });
                }

                Log.Information($"AppSetting表迁移完成，恢复了 {existingSettings.Count()} 条记录");
            }
        }
        catch (Exception ex)
        {
            Log.Warning("数据库迁移过程中发生错误，继续使用现有结构", ex);
        }
    }

    private async Task<List<string>> GetTableColumnsAsync(string tableName)
    {
        try
        {
            var connection = Connection;
            var result = await connection.QueryAsync<dynamic>($"PRAGMA table_info({tableName})");
            var columnNames = new List<string>();
            foreach (var row in result)
            {
                columnNames.Add(row.name.ToString());
            }

            return columnNames;
        }
        catch
        {
            return new List<string>();
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await Connection.ExecuteScalarAsync("SELECT 1");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}