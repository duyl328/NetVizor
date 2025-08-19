using System.Data;
using Data.Models;
using Dapper;

namespace Data.Repositories;

public interface INetworkRepository
{
    // 基础CRUD操作
    Task<int> SaveAppNetworkAsync(AppNetwork network);
    Task<int> SaveGlobalNetworkAsync(GlobalNetwork network);

    // 应用网络查询
    Task<IEnumerable<AppNetwork>> GetAppNetworkHistoryAsync(string appId, int limit = 100);
    Task<IEnumerable<AppNetwork>> GetAppNetworkByTimeRangeAsync(string appId, long startTime, long endTime);

    // 全局网络查询
    Task<IEnumerable<GlobalNetwork>> GetGlobalNetworkHistoryAsync(string networkCardGuid, int hours = 24);

    Task<IEnumerable<GlobalNetwork>> GetGlobalNetworkByTimeRangeAsync(string networkCardGuid, long startTime,
        long endTime);

    // 统计查询
    Task<long> GetTotalUploadAsync(string appId = null, long startTime = 0, long endTime = 0);
    Task<long> GetTotalDownloadAsync(string appId = null, long startTime = 0, long endTime = 0);

    // 按时间单位聚合查询
    Task<IEnumerable<AppNetworkSummary>> GetAppNetworkByHourAsync(string appId, int hours = 24);
    Task<IEnumerable<AppNetworkSummary>> GetAppNetworkByDayAsync(string appId, int days = 7);
    Task<IEnumerable<AppNetworkSummary>> GetAppNetworkByWeekAsync(string appId, int weeks = 4);
    Task<IEnumerable<AppNetworkSummary>> GetAppNetworkByMonthAsync(string appId, int months = 12);

    // 全局网络按时间单位聚合查询
    Task<IEnumerable<GlobalNetworkSummary>> GetGlobalNetworkByMinuteAsync(string networkCardGuid, int minutes = 60);
    Task<IEnumerable<GlobalNetworkSummary>> GetGlobalNetworkByHourAsync(string networkCardGuid, int hours = 24);
    Task<IEnumerable<GlobalNetworkSummary>> GetGlobalNetworkByDayAsync(string networkCardGuid, int days = 7);
    Task<IEnumerable<GlobalNetworkSummary>> GetGlobalNetworkByMonthAsync(string networkCardGuid, int months = 12);

    // 高级查询
    Task<IEnumerable<AppNetworkTopInfo>> GetTopAppsByTrafficAsync(int limit = 10, int days = 7);
    Task<IEnumerable<PortTrafficInfo>> GetTopPortsByAppAsync(string appId, int limit = 10, int days = 7);
    Task<IEnumerable<IpTrafficInfo>> GetTopIpsByAppAsync(string appId, int limit = 10, int days = 7);

    // 数据维护
    Task<int> DeleteOldAppNetworkDataAsync(long beforeTimestamp);
    Task<int> DeleteOldGlobalNetworkDataAsync(long beforeTimestamp);

    // 应用网络聚合数据操作
    Task<int> SaveAppNetworkHourlyAsync(AppNetworkHourly hourly);
    Task<int> SaveAppNetworkDailyAsync(AppNetworkDaily daily);
    Task<int> SaveAppNetworkWeeklyAsync(AppNetworkWeekly weekly);
    Task<int> SaveAppNetworkMonthlyAsync(AppNetworkMonthly monthly);

    // 全局网络聚合数据操作
    Task<int> SaveGlobalNetworkMinutelyAsync(GlobalNetworkMinutely minutely);
    Task<int> SaveGlobalNetworkHourlyAsync(GlobalNetworkHourly hourly);
    Task<int> SaveGlobalNetworkDailyAsync(GlobalNetworkDaily daily);
    Task<int> SaveGlobalNetworkWeeklyAsync(GlobalNetworkWeekly weekly);
    Task<int> SaveGlobalNetworkMonthlyAsync(GlobalNetworkMonthly monthly);

    // 数据存在性检查
    Task<bool> AppNetworkHourlyExistsAsync(string appId, long hourTimestamp);
    Task<bool> AppNetworkDailyExistsAsync(string appId, long dayTimestamp);
    Task<bool> AppNetworkWeeklyExistsAsync(string appId, long weekTimestamp);
    Task<bool> AppNetworkMonthlyExistsAsync(string appId, long monthTimestamp);

    Task<bool> GlobalNetworkMinutelyExistsAsync(string networkCardGuid, long minuteTimestamp);
    Task<bool> GlobalNetworkHourlyExistsAsync(string networkCardGuid, long hourTimestamp);
    Task<bool> GlobalNetworkDailyExistsAsync(string networkCardGuid, long dayTimestamp);
    Task<bool> GlobalNetworkWeeklyExistsAsync(string networkCardGuid, long weekTimestamp);
    Task<bool> GlobalNetworkMonthlyExistsAsync(string networkCardGuid, long monthTimestamp);

    // 聚合数据清理
    Task<int> DeleteAppNetworkHourlyBeforeAsync(long beforeTimestamp);
    Task<int> DeleteAppNetworkDailyBeforeAsync(long beforeTimestamp);
    Task<int> DeleteAppNetworkWeeklyBeforeAsync(long beforeTimestamp);

    Task<int> DeleteGlobalNetworkMinutelyBeforeAsync(long beforeTimestamp);
    Task<int> DeleteGlobalNetworkHourlyBeforeAsync(long beforeTimestamp);
    Task<int> DeleteGlobalNetworkDailyBeforeAsync(long beforeTimestamp);
    Task<int> DeleteGlobalNetworkWeeklyBeforeAsync(long beforeTimestamp);
}

public class NetworkRepository : INetworkRepository
{
    private readonly NetVizorDbContext _context;

    public NetworkRepository(NetVizorDbContext context)
    {
        _context = context;
    }

    #region 基础CRUD操作

    public async Task<int> SaveAppNetworkAsync(AppNetwork network)
    {
        const string sql = @"
            INSERT INTO AppNetwork (Timestamp, LocalIP, LocalPort, RemoteIP, RemotePort, Protocol, UploadBytes, DownloadBytes, AppId)
            VALUES (@Timestamp, @LocalIP, @LocalPort, @RemoteIP, @RemotePort, @Protocol, @UploadBytes, @DownloadBytes, @AppId)";

        return await _context.Connection.ExecuteAsync(sql, network);
    }

    public async Task<int> SaveGlobalNetworkAsync(GlobalNetwork network)
    {
        const string sql = @"
            INSERT INTO GlobalNetwork (Timestep, Upload, Download, NetworkCardGuid)
            VALUES (@Timestep, @Upload, @Download, @NetworkCardGuid)";

        return await _context.Connection.ExecuteAsync(sql, network);
    }

    #endregion

    #region 应用网络查询

    public async Task<IEnumerable<AppNetwork>> GetAppNetworkHistoryAsync(string appId, int limit = 100)
    {
        const string sql = @"
            SELECT * FROM AppNetwork 
            WHERE AppId = @AppId 
            ORDER BY Timestamp DESC 
            LIMIT @Limit";

        return await _context.Connection.QueryAsync<AppNetwork>(sql, new { AppId = appId, Limit = limit });
    }

    public async Task<IEnumerable<AppNetwork>> GetAppNetworkByTimeRangeAsync(string appId, long startTime, long endTime)
    {
        // 清理 appId，去掉多余的空白和引号
        if (!string.IsNullOrEmpty(appId))
        {
            appId = appId.Trim().Trim('"');
        }
        const string sql = @"
        SELECT * FROM AppNetwork 
        WHERE AppId = @AppId
          AND CAST(Timestamp AS INTEGER) >= @StartTime
          AND CAST(Timestamp AS INTEGER) <= @EndTime
        ORDER BY CAST(Timestamp AS INTEGER) DESC";

        var parameters = new DynamicParameters();
        parameters.Add("@AppId", appId, DbType.String);  // 强制 TEXT 匹配
        parameters.Add("@StartTime", startTime, DbType.Int64);
        parameters.Add("@EndTime", endTime, DbType.Int64);

        var appNetworks = await _context.Connection.QueryAsync<AppNetwork>(sql, parameters);

        Console.WriteLine($"[Result] Found {appNetworks.Count()} rows.");
        return appNetworks;
    }

    #endregion

    #region 全局网络查询

    public async Task<IEnumerable<GlobalNetwork>> GetGlobalNetworkHistoryAsync(string networkCardGuid, int hours = 24)
    {
        var startTime = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeSeconds();

        string sql;
        object parameters;

        if (string.IsNullOrEmpty(networkCardGuid))
        {
            // 获取所有网卡数据
            sql = @"
                SELECT * FROM GlobalNetwork 
                WHERE Timestep >= @StartTime
                ORDER BY Timestep DESC";
            parameters = new { StartTime = startTime };
        }
        else
        {
            // 获取指定网卡数据
            sql = @"
                SELECT * FROM GlobalNetwork 
                WHERE NetworkCardGuid = @NetworkCardGuid AND Timestep >= @StartTime
                ORDER BY Timestep DESC";
            parameters = new { NetworkCardGuid = networkCardGuid, StartTime = startTime };
        }

        return await _context.Connection.QueryAsync<GlobalNetwork>(sql, parameters);
    }

    public async Task<IEnumerable<GlobalNetwork>> GetGlobalNetworkByTimeRangeAsync(string networkCardGuid,
        long startTime, long endTime)
    {
        string sql;
        object parameters;

        if (string.IsNullOrEmpty(networkCardGuid))
        {
            // 获取所有网卡数据
            sql = @"
                SELECT * FROM GlobalNetwork 
                WHERE Timestep >= @StartTime AND Timestep <= @EndTime
                ORDER BY Timestep DESC";
            parameters = new { StartTime = startTime, EndTime = endTime };
        }
        else
        {
            // 获取指定网卡数据
            sql = @"
                SELECT * FROM GlobalNetwork 
                WHERE NetworkCardGuid = @NetworkCardGuid AND Timestep >= @StartTime AND Timestep <= @EndTime
                ORDER BY Timestep DESC";
            parameters = new { NetworkCardGuid = networkCardGuid, StartTime = startTime, EndTime = endTime };
        }

        return await _context.Connection.QueryAsync<GlobalNetwork>(sql, parameters);
    }

    #endregion

    #region 统计查询

    public async Task<long> GetTotalUploadAsync(string appId = null, long startTime = 0, long endTime = 0)
    {
        var sql = "SELECT COALESCE(SUM(UploadBytes), 0) FROM AppNetwork WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(appId))
        {
            sql += " AND AppId = @AppId";
            parameters.Add("AppId", appId);
        }

        if (startTime > 0)
        {
            sql += " AND Timestamp >= @StartTime";
            parameters.Add("StartTime", startTime);
        }

        if (endTime > 0)
        {
            sql += " AND Timestamp <= @EndTime";
            parameters.Add("EndTime", endTime);
        }

        return await _context.Connection.QuerySingleAsync<long>(sql, parameters);
    }

    public async Task<long> GetTotalDownloadAsync(string appId = null, long startTime = 0, long endTime = 0)
    {
        var sql = "SELECT COALESCE(SUM(DownloadBytes), 0) FROM AppNetwork WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(appId))
        {
            sql += " AND AppId = @AppId";
            parameters.Add("AppId", appId);
        }

        if (startTime > 0)
        {
            sql += " AND Timestamp >= @StartTime";
            parameters.Add("StartTime", startTime);
        }

        if (endTime > 0)
        {
            sql += " AND Timestamp <= @EndTime";
            parameters.Add("EndTime", endTime);
        }

        return await _context.Connection.QuerySingleAsync<long>(sql, parameters);
    }

    #endregion

    #region 按时间单位聚合查询

    public async Task<IEnumerable<AppNetworkSummary>> GetAppNetworkByHourAsync(string appId, int hours = 24)
    {
        var startTime = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeSeconds();

        const string sql = @"
            SELECT 
                (Timestamp / 3600) * 3600 as HourTimestamp,
                SUM(UploadBytes) as TotalUpload,
                SUM(DownloadBytes) as TotalDownload,
                COUNT(*) as ConnectionCount
            FROM AppNetwork 
            WHERE AppId = @AppId AND Timestamp >= @StartTime
            GROUP BY (Timestamp / 3600)
            ORDER BY HourTimestamp DESC";

        return await _context.Connection.QueryAsync<AppNetworkSummary>(sql,
            new { AppId = appId, StartTime = startTime });
    }

    public async Task<IEnumerable<AppNetworkSummary>> GetAppNetworkByDayAsync(string appId, int days = 7)
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeSeconds();

        const string sql = @"
            SELECT 
                (Timestamp / 86400) * 86400 as DayTimestamp,
                SUM(UploadBytes) as TotalUpload,
                SUM(DownloadBytes) as TotalDownload,
                COUNT(*) as ConnectionCount
            FROM AppNetwork 
            WHERE AppId = @AppId AND Timestamp >= @StartTime
            GROUP BY (Timestamp / 86400)
            ORDER BY DayTimestamp DESC";

        return await _context.Connection.QueryAsync<AppNetworkSummary>(sql,
            new { AppId = appId, StartTime = startTime });
    }

    public async Task<IEnumerable<AppNetworkSummary>> GetAppNetworkByWeekAsync(string appId, int weeks = 4)
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-weeks * 7).ToUnixTimeSeconds();

        const string sql = @"
            SELECT 
                (Timestamp / 604800) * 604800 as WeekTimestamp,
                SUM(UploadBytes) as TotalUpload,
                SUM(DownloadBytes) as TotalDownload,
                COUNT(*) as ConnectionCount
            FROM AppNetwork 
            WHERE AppId = @AppId AND Timestamp >= @StartTime
            GROUP BY (Timestamp / 604800)
            ORDER BY WeekTimestamp DESC";

        return await _context.Connection.QueryAsync<AppNetworkSummary>(sql,
            new { AppId = appId, StartTime = startTime });
    }

    public async Task<IEnumerable<AppNetworkSummary>> GetAppNetworkByMonthAsync(string appId, int months = 12)
    {
        var startTime = DateTimeOffset.UtcNow.AddMonths(-months).ToUnixTimeSeconds();

        const string sql = @"
            SELECT 
                (Timestamp / 2592000) * 2592000 as MonthTimestamp,
                SUM(UploadBytes) as TotalUpload,
                SUM(DownloadBytes) as TotalDownload,
                COUNT(*) as ConnectionCount
            FROM AppNetwork 
            WHERE AppId = @AppId AND Timestamp >= @StartTime
            GROUP BY (Timestamp / 2592000)
            ORDER BY MonthTimestamp DESC";

        return await _context.Connection.QueryAsync<AppNetworkSummary>(sql,
            new { AppId = appId, StartTime = startTime });
    }

    #endregion

    #region 全局网络按时间单位聚合查询

    public async Task<IEnumerable<GlobalNetworkSummary>> GetGlobalNetworkByMinuteAsync(string networkCardGuid, int minutes = 60)
    {
        var startTime = DateTimeOffset.UtcNow.AddMinutes(-minutes).ToUnixTimeSeconds();

        string sql;
        object parameters;

        if (string.IsNullOrEmpty(networkCardGuid))
        {
            // 获取所有网卡数据
            sql = @"
                SELECT 
                    MinuteTimestamp,
                    SUM(TotalUpload) as TotalUpload,
                    SUM(TotalDownload) as TotalDownload,
                    AVG(AvgUpload) as AvgUpload,
                    AVG(AvgDownload) as AvgDownload,
                    MAX(MaxUpload) as MaxUpload,
                    MAX(MaxDownload) as MaxDownload
                FROM GlobalNetworkMinutely 
                WHERE MinuteTimestamp >= @StartTime
                GROUP BY MinuteTimestamp
                ORDER BY MinuteTimestamp DESC";
            parameters = new { StartTime = startTime };
        }
        else
        {
            // 获取指定网卡数据
            sql = @"
                SELECT 
                    MinuteTimestamp,
                    TotalUpload,
                    TotalDownload,
                    AvgUpload,
                    AvgDownload,
                    MaxUpload,
                    MaxDownload
                FROM GlobalNetworkMinutely 
                WHERE NetworkCardGuid = @NetworkCardGuid AND MinuteTimestamp >= @StartTime
                ORDER BY MinuteTimestamp DESC";
            parameters = new { NetworkCardGuid = networkCardGuid, StartTime = startTime };
        }

        return await _context.Connection.QueryAsync<GlobalNetworkSummary>(sql, parameters);
    }

    public async Task<IEnumerable<GlobalNetworkSummary>> GetGlobalNetworkByHourAsync(string networkCardGuid,
        int hours = 24)
    {
        var startTime = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeSeconds();

        string sql;
        object parameters;

        if (string.IsNullOrEmpty(networkCardGuid))
        {
            // 获取所有网卡数据
            sql = @"
                SELECT 
                    (Timestep / 3600) * 3600 as HourTimestamp,
                    SUM(Upload) as TotalUpload,
                    SUM(Download) as TotalDownload,
                    AVG(Upload) as AvgUpload,
                    AVG(Download) as AvgDownload,
                    MAX(Upload) as MaxUpload,
                    MAX(Download) as MaxDownload
                FROM GlobalNetwork 
                WHERE Timestep >= @StartTime
                GROUP BY (Timestep / 3600)
                ORDER BY HourTimestamp DESC";
            parameters = new { StartTime = startTime };
        }
        else
        {
            // 获取指定网卡数据
            sql = @"
                SELECT 
                    (Timestep / 3600) * 3600 as HourTimestamp,
                    SUM(Upload) as TotalUpload,
                    SUM(Download) as TotalDownload,
                    AVG(Upload) as AvgUpload,
                    AVG(Download) as AvgDownload,
                    MAX(Upload) as MaxUpload,
                    MAX(Download) as MaxDownload
                FROM GlobalNetwork 
                WHERE NetworkCardGuid = @NetworkCardGuid AND Timestep >= @StartTime
                GROUP BY (Timestep / 3600)
                ORDER BY HourTimestamp DESC";
            parameters = new { NetworkCardGuid = networkCardGuid, StartTime = startTime };
        }

        return await _context.Connection.QueryAsync<GlobalNetworkSummary>(sql, parameters);
    }

    public async Task<IEnumerable<GlobalNetworkSummary>> GetGlobalNetworkByDayAsync(string networkCardGuid,
        int days = 7)
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeSeconds();

        string sql;
        object parameters;

        if (string.IsNullOrEmpty(networkCardGuid))
        {
            // 获取所有网卡数据
            sql = @"
                SELECT 
                    (Timestep / 86400) * 86400 as DayTimestamp,
                    SUM(Upload) as TotalUpload,
                    SUM(Download) as TotalDownload,
                    AVG(Upload) as AvgUpload,
                    AVG(Download) as AvgDownload,
                    MAX(Upload) as MaxUpload,
                    MAX(Download) as MaxDownload
                FROM GlobalNetwork 
                WHERE Timestep >= @StartTime
                GROUP BY (Timestep / 86400)
                ORDER BY DayTimestamp DESC";
            parameters = new { StartTime = startTime };
        }
        else
        {
            // 获取指定网卡数据
            sql = @"
                SELECT 
                    (Timestep / 86400) * 86400 as DayTimestamp,
                    SUM(Upload) as TotalUpload,
                    SUM(Download) as TotalDownload,
                    AVG(Upload) as AvgUpload,
                    AVG(Download) as AvgDownload,
                    MAX(Upload) as MaxUpload,
                    MAX(Download) as MaxDownload
                FROM GlobalNetwork 
                WHERE NetworkCardGuid = @NetworkCardGuid AND Timestep >= @StartTime
                GROUP BY (Timestep / 86400)
                ORDER BY DayTimestamp DESC";
            parameters = new { NetworkCardGuid = networkCardGuid, StartTime = startTime };
        }

        return await _context.Connection.QueryAsync<GlobalNetworkSummary>(sql, parameters);
    }

    public async Task<IEnumerable<GlobalNetworkSummary>> GetGlobalNetworkByMonthAsync(string networkCardGuid,
        int months = 12)
    {
        var startTime = DateTimeOffset.UtcNow.AddMonths(-months).ToUnixTimeSeconds();

        string sql;
        object parameters;

        if (string.IsNullOrEmpty(networkCardGuid))
        {
            // 获取所有网卡数据
            sql = @"
                SELECT 
                    (Timestep / 2592000) * 2592000 as MonthTimestamp,
                    SUM(Upload) as TotalUpload,
                    SUM(Download) as TotalDownload,
                    AVG(Upload) as AvgUpload,
                    AVG(Download) as AvgDownload,
                    MAX(Upload) as MaxUpload,
                    MAX(Download) as MaxDownload
                FROM GlobalNetwork 
                WHERE Timestep >= @StartTime
                GROUP BY (Timestep / 2592000)
                ORDER BY MonthTimestamp DESC";
            parameters = new { StartTime = startTime };
        }
        else
        {
            // 获取指定网卡数据
            sql = @"
                SELECT 
                    (Timestep / 2592000) * 2592000 as MonthTimestamp,
                    SUM(Upload) as TotalUpload,
                    SUM(Download) as TotalDownload,
                    AVG(Upload) as AvgUpload,
                    AVG(Download) as AvgDownload,
                    MAX(Upload) as MaxUpload,
                    MAX(Download) as MaxDownload
                FROM GlobalNetwork 
                WHERE NetworkCardGuid = @NetworkCardGuid AND Timestep >= @StartTime
                GROUP BY (Timestep / 2592000)
                ORDER BY MonthTimestamp DESC";
            parameters = new { NetworkCardGuid = networkCardGuid, StartTime = startTime };
        }

        return await _context.Connection.QueryAsync<GlobalNetworkSummary>(sql, parameters);
    }

    #endregion

    #region 高级查询

    public async Task<IEnumerable<AppNetworkTopInfo>> GetTopAppsByTrafficAsync(int limit = 10, int days = 7)
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeSeconds();

        const string sql = @"
            SELECT 
                a.AppId,
                ai.Name as AppName,
                ai.Path as AppPath,
                SUM(a.UploadBytes) as TotalUpload,
                SUM(a.DownloadBytes) as TotalDownload,
                SUM(a.UploadBytes + a.DownloadBytes) as TotalTraffic,
                COUNT(*) as ConnectionCount
            FROM AppNetwork a
            LEFT JOIN AppInfo ai ON a.AppId = ai.AppId
            WHERE a.Timestamp >= @StartTime
            GROUP BY a.AppId
            ORDER BY TotalTraffic DESC
            LIMIT @Limit";

        return await _context.Connection.QueryAsync<AppNetworkTopInfo>(sql,
            new { StartTime = startTime, Limit = limit });
    }

    public async Task<IEnumerable<PortTrafficInfo>> GetTopPortsByAppAsync(string appId, int limit = 10, int days = 7)
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeSeconds();

        const string sql = @"
            SELECT 
                RemotePort as Port,
                SUM(UploadBytes) as TotalUpload,
                SUM(DownloadBytes) as TotalDownload,
                SUM(UploadBytes + DownloadBytes) as TotalTraffic,
                COUNT(*) as ConnectionCount
            FROM AppNetwork 
            WHERE AppId = @AppId AND Timestamp >= @StartTime
            GROUP BY RemotePort
            ORDER BY TotalTraffic DESC
            LIMIT @Limit";

        return await _context.Connection.QueryAsync<PortTrafficInfo>(sql,
            new { AppId = appId, StartTime = startTime, Limit = limit });
    }

    public async Task<IEnumerable<IpTrafficInfo>> GetTopIpsByAppAsync(string appId, int limit = 10, int days = 7)
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeSeconds();

        const string sql = @"
            SELECT 
                RemoteIP as IpAddress,
                SUM(UploadBytes) as TotalUpload,
                SUM(DownloadBytes) as TotalDownload,
                SUM(UploadBytes + DownloadBytes) as TotalTraffic,
                COUNT(*) as ConnectionCount
            FROM AppNetwork 
            WHERE AppId = @AppId AND Timestamp >= @StartTime
            GROUP BY RemoteIP
            ORDER BY TotalTraffic DESC
            LIMIT @Limit";

        return await _context.Connection.QueryAsync<IpTrafficInfo>(sql,
            new { AppId = appId, StartTime = startTime, Limit = limit });
    }

    #endregion

    #region 数据维护

    public async Task<int> DeleteOldAppNetworkDataAsync(long beforeTimestamp)
    {
        const string sql = "DELETE FROM AppNetwork WHERE Timestamp < @BeforeTimestamp";
        return await _context.Connection.ExecuteAsync(sql, new { BeforeTimestamp = beforeTimestamp });
    }

    public async Task<int> DeleteOldGlobalNetworkDataAsync(long beforeTimestamp)
    {
        const string sql = "DELETE FROM GlobalNetwork WHERE Timestep < @BeforeTimestamp";
        return await _context.Connection.ExecuteAsync(sql, new { BeforeTimestamp = beforeTimestamp });
    }

    #endregion

    #region 应用网络聚合数据操作

    public async Task<int> SaveAppNetworkHourlyAsync(AppNetworkHourly hourly)
    {
        const string sql = @"
            INSERT INTO AppNetworkHourly (AppId, HourTimestamp, TotalUploadBytes, TotalDownloadBytes, ConnectionCount, UniqueRemoteIPs, UniqueRemotePorts, CreatedTimestamp)
            VALUES (@AppId, @HourTimestamp, @TotalUploadBytes, @TotalDownloadBytes, @ConnectionCount, @UniqueRemoteIPs, @UniqueRemotePorts, @CreatedTimestamp)";

        return await _context.Connection.ExecuteAsync(sql, hourly);
    }

    public async Task<int> SaveAppNetworkDailyAsync(AppNetworkDaily daily)
    {
        const string sql = @"
            INSERT INTO AppNetworkDaily (AppId, DayTimestamp, TotalUploadBytes, TotalDownloadBytes, ConnectionCount, UniqueRemoteIPs, UniqueRemotePorts, CreatedTimestamp)
            VALUES (@AppId, @DayTimestamp, @TotalUploadBytes, @TotalDownloadBytes, @ConnectionCount, @UniqueRemoteIPs, @UniqueRemotePorts, @CreatedTimestamp)";

        return await _context.Connection.ExecuteAsync(sql, daily);
    }

    public async Task<int> SaveAppNetworkWeeklyAsync(AppNetworkWeekly weekly)
    {
        const string sql = @"
            INSERT INTO AppNetworkWeekly (AppId, WeekTimestamp, TotalUploadBytes, TotalDownloadBytes, ConnectionCount, UniqueRemoteIPs, UniqueRemotePorts, CreatedTimestamp)
            VALUES (@AppId, @WeekTimestamp, @TotalUploadBytes, @TotalDownloadBytes, @ConnectionCount, @UniqueRemoteIPs, @UniqueRemotePorts, @CreatedTimestamp)";

        return await _context.Connection.ExecuteAsync(sql, weekly);
    }

    public async Task<int> SaveAppNetworkMonthlyAsync(AppNetworkMonthly monthly)
    {
        const string sql = @"
            INSERT INTO AppNetworkMonthly (AppId, MonthTimestamp, TotalUploadBytes, TotalDownloadBytes, ConnectionCount, UniqueRemoteIPs, UniqueRemotePorts, CreatedTimestamp)
            VALUES (@AppId, @MonthTimestamp, @TotalUploadBytes, @TotalDownloadBytes, @ConnectionCount, @UniqueRemoteIPs, @UniqueRemotePorts, @CreatedTimestamp)";

        return await _context.Connection.ExecuteAsync(sql, monthly);
    }

    #endregion

    #region 全局网络聚合数据操作

    public async Task<int> SaveGlobalNetworkMinutelyAsync(GlobalNetworkMinutely minutely)
    {
        const string sql = @"
            INSERT INTO GlobalNetworkMinutely (NetworkCardGuid, MinuteTimestamp, TotalUpload, TotalDownload, AvgUpload, AvgDownload, MaxUpload, MaxDownload, RecordCount, CreatedTimestamp)
            VALUES (@NetworkCardGuid, @MinuteTimestamp, @TotalUpload, @TotalDownload, @AvgUpload, @AvgDownload, @MaxUpload, @MaxDownload, @RecordCount, @CreatedTimestamp)";

        return await _context.Connection.ExecuteAsync(sql, minutely);
    }

    public async Task<int> SaveGlobalNetworkHourlyAsync(GlobalNetworkHourly hourly)
    {
        const string sql = @"
            INSERT INTO GlobalNetworkHourly (NetworkCardGuid, HourTimestamp, TotalUpload, TotalDownload, AvgUpload, AvgDownload, MaxUpload, MaxDownload, RecordCount, CreatedTimestamp)
            VALUES (@NetworkCardGuid, @HourTimestamp, @TotalUpload, @TotalDownload, @AvgUpload, @AvgDownload, @MaxUpload, @MaxDownload, @RecordCount, @CreatedTimestamp)";

        return await _context.Connection.ExecuteAsync(sql, hourly);
    }

    public async Task<int> SaveGlobalNetworkDailyAsync(GlobalNetworkDaily daily)
    {
        const string sql = @"
            INSERT INTO GlobalNetworkDaily (NetworkCardGuid, DayTimestamp, TotalUpload, TotalDownload, AvgUpload, AvgDownload, MaxUpload, MaxDownload, RecordCount, CreatedTimestamp)
            VALUES (@NetworkCardGuid, @DayTimestamp, @TotalUpload, @TotalDownload, @AvgUpload, @AvgDownload, @MaxUpload, @MaxDownload, @RecordCount, @CreatedTimestamp)";

        return await _context.Connection.ExecuteAsync(sql, daily);
    }

    public async Task<int> SaveGlobalNetworkWeeklyAsync(GlobalNetworkWeekly weekly)
    {
        const string sql = @"
            INSERT INTO GlobalNetworkWeekly (NetworkCardGuid, WeekTimestamp, TotalUpload, TotalDownload, AvgUpload, AvgDownload, MaxUpload, MaxDownload, RecordCount, CreatedTimestamp)
            VALUES (@NetworkCardGuid, @WeekTimestamp, @TotalUpload, @TotalDownload, @AvgUpload, @AvgDownload, @MaxUpload, @MaxDownload, @RecordCount, @CreatedTimestamp)";

        return await _context.Connection.ExecuteAsync(sql, weekly);
    }

    public async Task<int> SaveGlobalNetworkMonthlyAsync(GlobalNetworkMonthly monthly)
    {
        const string sql = @"
            INSERT INTO GlobalNetworkMonthly (NetworkCardGuid, MonthTimestamp, TotalUpload, TotalDownload, AvgUpload, AvgDownload, MaxUpload, MaxDownload, RecordCount, CreatedTimestamp)
            VALUES (@NetworkCardGuid, @MonthTimestamp, @TotalUpload, @TotalDownload, @AvgUpload, @AvgDownload, @MaxUpload, @MaxDownload, @RecordCount, @CreatedTimestamp)";

        return await _context.Connection.ExecuteAsync(sql, monthly);
    }

    #endregion

    #region 数据存在性检查

    public async Task<bool> AppNetworkHourlyExistsAsync(string appId, long hourTimestamp)
    {
        const string sql =
            "SELECT COUNT(1) FROM AppNetworkHourly WHERE AppId = @AppId AND HourTimestamp = @HourTimestamp";
        var count = await _context.Connection.QuerySingleAsync<int>(sql,
            new { AppId = appId, HourTimestamp = hourTimestamp });
        return count > 0;
    }

    public async Task<bool> AppNetworkDailyExistsAsync(string appId, long dayTimestamp)
    {
        const string sql = "SELECT COUNT(1) FROM AppNetworkDaily WHERE AppId = @AppId AND DayTimestamp = @DayTimestamp";
        var count = await _context.Connection.QuerySingleAsync<int>(sql,
            new { AppId = appId, DayTimestamp = dayTimestamp });
        return count > 0;
    }

    public async Task<bool> AppNetworkWeeklyExistsAsync(string appId, long weekTimestamp)
    {
        const string sql =
            "SELECT COUNT(1) FROM AppNetworkWeekly WHERE AppId = @AppId AND WeekTimestamp = @WeekTimestamp";
        var count = await _context.Connection.QuerySingleAsync<int>(sql,
            new { AppId = appId, WeekTimestamp = weekTimestamp });
        return count > 0;
    }

    public async Task<bool> AppNetworkMonthlyExistsAsync(string appId, long monthTimestamp)
    {
        const string sql =
            "SELECT COUNT(1) FROM AppNetworkMonthly WHERE AppId = @AppId AND MonthTimestamp = @MonthTimestamp";
        var count = await _context.Connection.QuerySingleAsync<int>(sql,
            new { AppId = appId, MonthTimestamp = monthTimestamp });
        return count > 0;
    }

    public async Task<bool> GlobalNetworkMinutelyExistsAsync(string networkCardGuid, long minuteTimestamp)
    {
        const string sql =
            "SELECT COUNT(1) FROM GlobalNetworkMinutely WHERE NetworkCardGuid = @NetworkCardGuid AND MinuteTimestamp = @MinuteTimestamp";
        var count = await _context.Connection.QuerySingleAsync<int>(sql,
            new { NetworkCardGuid = networkCardGuid, MinuteTimestamp = minuteTimestamp });
        return count > 0;
    }

    public async Task<bool> GlobalNetworkHourlyExistsAsync(string networkCardGuid, long hourTimestamp)
    {
        const string sql =
            "SELECT COUNT(1) FROM GlobalNetworkHourly WHERE NetworkCardGuid = @NetworkCardGuid AND HourTimestamp = @HourTimestamp";
        var count = await _context.Connection.QuerySingleAsync<int>(sql,
            new { NetworkCardGuid = networkCardGuid, HourTimestamp = hourTimestamp });
        return count > 0;
    }

    public async Task<bool> GlobalNetworkDailyExistsAsync(string networkCardGuid, long dayTimestamp)
    {
        const string sql =
            "SELECT COUNT(1) FROM GlobalNetworkDaily WHERE NetworkCardGuid = @NetworkCardGuid AND DayTimestamp = @DayTimestamp";
        var count = await _context.Connection.QuerySingleAsync<int>(sql,
            new { NetworkCardGuid = networkCardGuid, DayTimestamp = dayTimestamp });
        return count > 0;
    }

    public async Task<bool> GlobalNetworkWeeklyExistsAsync(string networkCardGuid, long weekTimestamp)
    {
        const string sql =
            "SELECT COUNT(1) FROM GlobalNetworkWeekly WHERE NetworkCardGuid = @NetworkCardGuid AND WeekTimestamp = @WeekTimestamp";
        var count = await _context.Connection.QuerySingleAsync<int>(sql,
            new { NetworkCardGuid = networkCardGuid, WeekTimestamp = weekTimestamp });
        return count > 0;
    }

    public async Task<bool> GlobalNetworkMonthlyExistsAsync(string networkCardGuid, long monthTimestamp)
    {
        const string sql =
            "SELECT COUNT(1) FROM GlobalNetworkMonthly WHERE NetworkCardGuid = @NetworkCardGuid AND MonthTimestamp = @MonthTimestamp";
        var count = await _context.Connection.QuerySingleAsync<int>(sql,
            new { NetworkCardGuid = networkCardGuid, MonthTimestamp = monthTimestamp });
        return count > 0;
    }

    #endregion

    #region 聚合数据清理

    public async Task<int> DeleteAppNetworkHourlyBeforeAsync(long beforeTimestamp)
    {
        const string sql = "DELETE FROM AppNetworkHourly WHERE HourTimestamp < @BeforeTimestamp";
        return await _context.Connection.ExecuteAsync(sql, new { BeforeTimestamp = beforeTimestamp });
    }

    public async Task<int> DeleteAppNetworkDailyBeforeAsync(long beforeTimestamp)
    {
        const string sql = "DELETE FROM AppNetworkDaily WHERE DayTimestamp < @BeforeTimestamp";
        return await _context.Connection.ExecuteAsync(sql, new { BeforeTimestamp = beforeTimestamp });
    }

    public async Task<int> DeleteAppNetworkWeeklyBeforeAsync(long beforeTimestamp)
    {
        const string sql = "DELETE FROM AppNetworkWeekly WHERE WeekTimestamp < @BeforeTimestamp";
        return await _context.Connection.ExecuteAsync(sql, new { BeforeTimestamp = beforeTimestamp });
    }

    public async Task<int> DeleteGlobalNetworkMinutelyBeforeAsync(long beforeTimestamp)
    {
        const string sql = "DELETE FROM GlobalNetworkMinutely WHERE MinuteTimestamp < @BeforeTimestamp";
        return await _context.Connection.ExecuteAsync(sql, new { BeforeTimestamp = beforeTimestamp });
    }

    public async Task<int> DeleteGlobalNetworkHourlyBeforeAsync(long beforeTimestamp)
    {
        const string sql = "DELETE FROM GlobalNetworkHourly WHERE HourTimestamp < @BeforeTimestamp";
        return await _context.Connection.ExecuteAsync(sql, new { BeforeTimestamp = beforeTimestamp });
    }

    public async Task<int> DeleteGlobalNetworkDailyBeforeAsync(long beforeTimestamp)
    {
        const string sql = "DELETE FROM GlobalNetworkDaily WHERE DayTimestamp < @BeforeTimestamp";
        return await _context.Connection.ExecuteAsync(sql, new { BeforeTimestamp = beforeTimestamp });
    }

    public async Task<int> DeleteGlobalNetworkWeeklyBeforeAsync(long beforeTimestamp)
    {
        const string sql = "DELETE FROM GlobalNetworkWeekly WHERE WeekTimestamp < @BeforeTimestamp";
        return await _context.Connection.ExecuteAsync(sql, new { BeforeTimestamp = beforeTimestamp });
    }

    #endregion
}

#region 查询结果模型

public class AppNetworkSummary
{
    public long HourTimestamp { get; set; }
    public long DayTimestamp { get; set; }
    public long WeekTimestamp { get; set; }
    public long MonthTimestamp { get; set; }
    public long TotalUpload { get; set; }
    public long TotalDownload { get; set; }
    public int ConnectionCount { get; set; }
}

public class GlobalNetworkSummary
{
    public long MinuteTimestamp { get; set; }
    public long HourTimestamp { get; set; }
    public long DayTimestamp { get; set; }
    public long MonthTimestamp { get; set; }
    public long TotalUpload { get; set; }
    public long TotalDownload { get; set; }
    public long AvgUpload { get; set; }
    public long AvgDownload { get; set; }
    public long MaxUpload { get; set; }
    public long MaxDownload { get; set; }
}

public class AppNetworkTopInfo
{
    public string AppId { get; set; }
    public string AppName { get; set; }
    public string AppPath { get; set; }
    public long TotalUpload { get; set; }
    public long TotalDownload { get; set; }
    public long TotalTraffic { get; set; }
    public int ConnectionCount { get; set; }
}

public class PortTrafficInfo
{
    public int Port { get; set; }
    public long TotalUpload { get; set; }
    public long TotalDownload { get; set; }
    public long TotalTraffic { get; set; }
    public int ConnectionCount { get; set; }
}

public class IpTrafficInfo
{
    public string IpAddress { get; set; }
    public long TotalUpload { get; set; }
    public long TotalDownload { get; set; }
    public long TotalTraffic { get; set; }
    public int ConnectionCount { get; set; }
}

#endregion
