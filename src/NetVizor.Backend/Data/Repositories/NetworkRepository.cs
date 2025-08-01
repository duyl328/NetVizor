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
        const string sql = @"
            SELECT * FROM AppNetwork 
            WHERE AppId = @AppId AND Timestamp >= @StartTime AND Timestamp <= @EndTime
            ORDER BY Timestamp DESC";

        return await _context.Connection.QueryAsync<AppNetwork>(sql,
            new { AppId = appId, StartTime = startTime, EndTime = endTime });
    }

    #endregion

    #region 全局网络查询

    public async Task<IEnumerable<GlobalNetwork>> GetGlobalNetworkHistoryAsync(string networkCardGuid, int hours = 24)
    {
        var startTime = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeSeconds();

        const string sql = @"
            SELECT * FROM GlobalNetwork 
            WHERE NetworkCardGuid = @NetworkCardGuid AND Timestep >= @StartTime
            ORDER BY Timestep DESC";

        return await _context.Connection.QueryAsync<GlobalNetwork>(sql,
            new { NetworkCardGuid = networkCardGuid, StartTime = startTime });
    }

    public async Task<IEnumerable<GlobalNetwork>> GetGlobalNetworkByTimeRangeAsync(string networkCardGuid,
        long startTime, long endTime)
    {
        const string sql = @"
            SELECT * FROM GlobalNetwork 
            WHERE NetworkCardGuid = @NetworkCardGuid AND Timestep >= @StartTime AND Timestep <= @EndTime
            ORDER BY Timestep DESC";

        return await _context.Connection.QueryAsync<GlobalNetwork>(sql,
            new { NetworkCardGuid = networkCardGuid, StartTime = startTime, EndTime = endTime });
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

    public async Task<IEnumerable<GlobalNetworkSummary>> GetGlobalNetworkByHourAsync(string networkCardGuid,
        int hours = 24)
    {
        var startTime = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeSeconds();

        const string sql = @"
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

        return await _context.Connection.QueryAsync<GlobalNetworkSummary>(sql,
            new { NetworkCardGuid = networkCardGuid, StartTime = startTime });
    }

    public async Task<IEnumerable<GlobalNetworkSummary>> GetGlobalNetworkByDayAsync(string networkCardGuid,
        int days = 7)
    {
        var startTime = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeSeconds();

        const string sql = @"
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

        return await _context.Connection.QueryAsync<GlobalNetworkSummary>(sql,
            new { NetworkCardGuid = networkCardGuid, StartTime = startTime });
    }

    public async Task<IEnumerable<GlobalNetworkSummary>> GetGlobalNetworkByMonthAsync(string networkCardGuid,
        int months = 12)
    {
        var startTime = DateTimeOffset.UtcNow.AddMonths(-months).ToUnixTimeSeconds();

        const string sql = @"
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

        return await _context.Connection.QueryAsync<GlobalNetworkSummary>(sql,
            new { NetworkCardGuid = networkCardGuid, StartTime = startTime });
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