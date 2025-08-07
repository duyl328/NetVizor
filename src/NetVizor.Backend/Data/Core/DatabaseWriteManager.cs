using System.Collections.Concurrent;
using System.Threading.Channels;
using Common.Logger;
using Data.Models;
using Data.Services;
using Dapper;

namespace Data.Core;

/// <summary>
/// 数据库写入操作类型
/// </summary>
public enum DbOperationType
{
    /// <summary>
    /// App 信息
    /// </summary>
    SaveAppInfo,
    SaveAppNetwork,
    SaveGlobalNetwork,

    /// <summary>
    /// 总流量数据
    /// </summary>
    AggregateData,

    /// <summary>
    /// 清理老数据
    /// </summary>
    CleanupOldData
}

/// <summary>
/// 数据库操作请求
/// </summary>
public class DbOperationRequest
{
    public DbOperationType Type { get; set; }
    public object Data { get; set; }
    public TaskCompletionSource<bool> CompletionSource { get; set; }
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 专用的数据库写入线程管理器
/// 所有数据库写入操作都通过这个管理器进行，保证SQLite的最佳性能
/// </summary>
public class DatabaseWriteManager : IDisposable
{
    private readonly Channel<DbOperationRequest> _operationChannel;
    private readonly ChannelWriter<DbOperationRequest> _writer;
    private readonly ChannelReader<DbOperationRequest> _reader;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _processingTask;
    private readonly IDataService _dataService;

    // 批处理缓存
    private readonly List<AppNetwork> _appNetworkBatch = new();
    private readonly List<GlobalNetwork> _globalNetworkBatch = new();
    private readonly SemaphoreSlim _batchSemaphore = new(1, 1);

    // 性能计数器
    private long _totalOperations = 0;
    private long _successfulOperations = 0;
    private DateTime _lastStatsReport = DateTime.UtcNow;

    public DatabaseWriteManager(IDataService dataService)
    {
        _dataService = dataService;

        // 创建高性能的Channel用于操作队列
        var options = new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _operationChannel = Channel.CreateBounded<DbOperationRequest>(options);
        _writer = _operationChannel.Writer;
        _reader = _operationChannel.Reader;

        _cancellationTokenSource = new CancellationTokenSource();

        // 启动写入处理线程
        _processingTask = Task.Run(ProcessOperationsAsync, _cancellationTokenSource.Token);

        Log.Information("数据库写入线程管理器已启动");
    }

    /// <summary>
    /// 异步保存应用信息
    /// </summary>
    public async Task<bool> SaveAppInfoAsync(AppInfo appInfo)
    {
        var request = new DbOperationRequest
        {
            Type = DbOperationType.SaveAppInfo,
            Data = appInfo,
            CompletionSource = new TaskCompletionSource<bool>()
        };

        if (await _writer.WaitToWriteAsync(_cancellationTokenSource.Token))
        {
            await _writer.WriteAsync(request, _cancellationTokenSource.Token);
            return await request.CompletionSource.Task;
        }

        return false;
    }

    /// <summary>
    /// 异步保存应用网络记录（支持批处理）
    /// </summary>
    public async Task<bool> SaveAppNetworkAsync(AppNetwork appNetwork)
    {
        await _batchSemaphore.WaitAsync();
        try
        {
            _appNetworkBatch.Add(appNetwork);

            // 当批处理达到一定数量或时间间隔时，触发批量写入
            if (_appNetworkBatch.Count >= 50)
            {
                await FlushAppNetworkBatchAsync();
            }
        }
        finally
        {
            _batchSemaphore.Release();
        }

        return true;
    }

    /// <summary>
    /// 异步保存全局网络记录（支持批处理）
    /// </summary>
    public async Task<bool> SaveGlobalNetworkAsync(GlobalNetwork globalNetwork)
    {
        await _batchSemaphore.WaitAsync();
        try
        {
            _globalNetworkBatch.Add(globalNetwork);

            // 当批处理达到一定数量时，触发批量写入
            if (_globalNetworkBatch.Count >= 20)
            {
                await FlushGlobalNetworkBatchAsync();
            }
        }
        finally
        {
            _batchSemaphore.Release();
        }

        return true;
    }

    /// <summary>
    /// 强制刷新所有批处理缓存
    /// </summary>
    public async Task FlushAllBatchesAsync()
    {
        await _batchSemaphore.WaitAsync();
        try
        {
            await FlushAppNetworkBatchAsync();
            await FlushGlobalNetworkBatchAsync();
        }
        finally
        {
            _batchSemaphore.Release();
        }
    }

    /// <summary>
    /// 触发数据聚合操作
    /// </summary>
    public async Task<bool> TriggerDataAggregationAsync()
    {
        var request = new DbOperationRequest
        {
            Type = DbOperationType.AggregateData,
            Data = DateTime.UtcNow,
            CompletionSource = new TaskCompletionSource<bool>()
        };

        if (await _writer.WaitToWriteAsync(_cancellationTokenSource.Token))
        {
            await _writer.WriteAsync(request, _cancellationTokenSource.Token);
            return await request.CompletionSource.Task;
        }

        return false;
    }

    /// <summary>
    /// 触发数据清理操作
    /// </summary>
    public async Task<bool> TriggerDataCleanupAsync()
    {
        var request = new DbOperationRequest
        {
            Type = DbOperationType.CleanupOldData,
            Data = DateTime.UtcNow,
            CompletionSource = new TaskCompletionSource<bool>()
        };

        if (await _writer.WaitToWriteAsync(_cancellationTokenSource.Token))
        {
            await _writer.WriteAsync(request, _cancellationTokenSource.Token);
            return await request.CompletionSource.Task;
        }

        return false;
    }

    /// <summary>
    /// 主要的操作处理循环
    /// </summary>
    private async Task ProcessOperationsAsync()
    {
        Log.Information("数据库写入处理线程已启动");

        try
        {
            await foreach (var request in _reader.ReadAllAsync(_cancellationTokenSource.Token))
            {
                Interlocked.Increment(ref _totalOperations);

                try
                {
                    bool success = await ProcessSingleOperationAsync(request);

                    if (success)
                    {
                        Interlocked.Increment(ref _successfulOperations);
                    }

                    request.CompletionSource.SetResult(success);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"处理数据库操作时发生错误: {request.Type}");
                    request.CompletionSource.SetException(ex);
                }

                // 定期报告统计信息
                if (DateTime.UtcNow - _lastStatsReport > TimeSpan.FromMinutes(5))
                {
                    ReportStatistics();
                    _lastStatsReport = DateTime.UtcNow;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log.Information("数据库写入线程已取消");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据库写入线程发生未处理异常");
        }
    }

    /// <summary>
    /// 处理单个数据库操作
    /// </summary>
    private async Task<bool> ProcessSingleOperationAsync(DbOperationRequest request)
    {
        switch (request.Type)
        {
            case DbOperationType.SaveAppInfo:
                var appInfo = (AppInfo)request.Data;
                await _dataService.AppInfos.SaveAppAsync(appInfo);
                return true;

            case DbOperationType.AggregateData:
                return await PerformDataAggregationAsync();

            case DbOperationType.CleanupOldData:
                return await PerformDataCleanupAsync();

            default:
                Log.Warning($"未知的数据库操作类型: {request.Type}");
                return false;
        }
    }

    /// <summary>
    /// 刷新应用网络批处理
    /// </summary>
    private async Task FlushAppNetworkBatchAsync()
    {
        if (_appNetworkBatch.Count == 0) return;

        try
        {
            var batch = _appNetworkBatch.ToList();
            _appNetworkBatch.Clear();

            foreach (var item in batch)
            {
                await _dataService.Networks.SaveAppNetworkAsync(item);
            }

            Log.Debug($"批量写入 {batch.Count} 条应用网络记录");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "批量写入应用网络记录失败");
        }
    }

    /// <summary>
    /// 刷新全局网络批处理
    /// </summary>
    private async Task FlushGlobalNetworkBatchAsync()
    {
        if (_globalNetworkBatch.Count == 0) return;

        try
        {
            var batch = _globalNetworkBatch.ToList();
            _globalNetworkBatch.Clear();

            foreach (var item in batch)
            {
                await _dataService.Networks.SaveGlobalNetworkAsync(item);
            }

            Log.Debug($"批量写入 {batch.Count} 条全局网络记录");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "批量写入全局网络记录失败");
        }
    }

    /// <summary>
    /// 执行数据聚合
    /// </summary>
    private async Task<bool> PerformDataAggregationAsync()
    {
        try
        {
            Log.Information("开始执行数据聚合任务");

            // 先刷新所有批处理数据
            await FlushAllBatchesAsync();

            var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var oneDayAgo = currentTimestamp - 86400; // 1天前
            var oneWeekAgo = currentTimestamp - 604800; // 1周前
            var oneMonthAgo = currentTimestamp - 2592000; // 1个月前 (30天)
            var threeDaysAgo = currentTimestamp - 259200; // 3天前

            // 聚合应用网络数据
            await AggregateAppNetworkDataAsync(oneDayAgo, oneWeekAgo, oneMonthAgo);

            // 聚合全局网络数据
            await AggregateGlobalNetworkDataAsync(threeDaysAgo, oneWeekAgo);

            Log.Information("数据聚合任务完成");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据聚合任务失败");
            return false;
        }
    }

    /// <summary>
    /// 聚合应用网络数据
    /// </summary>
    private async Task AggregateAppNetworkDataAsync(long oneDayAgo, long oneWeekAgo, long oneMonthAgo)
    {
        try
        {
            Log.Information($"开始聚合应用网络数据 - 时间戳: 1天前={oneDayAgo}, 1周前={oneWeekAgo}, 1月前={oneMonthAgo}");

            // 获取所有应用ID
            var connection = _dataService.Networks.GetType()
                .GetField("_context",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_dataService.Networks);
            var contextConnection =
                connection?.GetType().GetProperty("Connection")?.GetValue(connection) as System.Data.IDbConnection;

            if (contextConnection == null)
            {
                Log.Warning("无法获取数据库连接，跳过应用网络数据聚合");
                return;
            }

            // 先查看总数据量
            var totalRecords = await contextConnection.QuerySingleAsync<int>("SELECT COUNT(*) FROM AppNetwork");
            Log.Information($"AppNetwork表总记录数: {totalRecords}");

            // 获取需要聚合的应用ID列表 - 修改查询条件以包含更多数据进行测试
            var appIds = await contextConnection.QueryAsync<string>(
                "SELECT DISTINCT AppId FROM AppNetwork", new { });

            Log.Information($"找到 {appIds.Count()} 个需要聚合的应用");

            if (!appIds.Any())
            {
                Log.Warning("没有找到任何应用数据，跳过聚合");
                return;
            }

            foreach (var appId in appIds)
            {
                Log.Information($"开始处理应用: {appId}");

                // 第一步：从原始数据聚合到小时数据
                Log.Information($"为应用 {appId} 聚合小时数据");
                await AggregateAppNetworkHourlyAsync(appId);

                // 第二步：从小时数据聚合到天数据（超过1天的数据）
                Log.Information($"为应用 {appId} 聚合天数据");
                await AggregateAppNetworkDailyAsync(appId, oneDayAgo);

                // 第三步：从天数据聚合到周数据（超过1周的数据）
                Log.Information($"为应用 {appId} 聚合周数据");
                await AggregateAppNetworkWeeklyAsync(appId, oneWeekAgo);

                // 第四步：从周数据聚合到月数据（超过1月的数据）
                Log.Information($"为应用 {appId} 聚合月数据");
                await AggregateAppNetworkMonthlyAsync(appId, oneMonthAgo);
            }

            Log.Information($"应用网络数据聚合完成，处理了 {appIds.Count()} 个应用");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "聚合应用网络数据失败");
        }
    }

    /// <summary>
    /// 聚合全局网络数据
    /// </summary>
    private async Task AggregateGlobalNetworkDataAsync(long threeDaysAgo, long oneWeekAgo)
    {
        try
        {
            Log.Information("开始聚合全局网络数据");

            var connection = _dataService.Networks.GetType()
                .GetField("_context",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_dataService.Networks);
            var contextConnection =
                connection?.GetType().GetProperty("Connection")?.GetValue(connection) as System.Data.IDbConnection;

            if (contextConnection == null)
            {
                Log.Warning("无法获取数据库连接，跳过全局网络数据聚合");
                return;
            }

            // 先查看总数据量
            var totalRecords = await contextConnection.QuerySingleAsync<int>("SELECT COUNT(*) FROM GlobalNetwork");
            Log.Information($"GlobalNetwork表总记录数: {totalRecords}");

            // 获取需要聚合的网卡GUID列表
            var networkCardGuids = await contextConnection.QueryAsync<string>(
                "SELECT DISTINCT NetworkCardGuid FROM GlobalNetwork", new { });

            Log.Information($"找到 {networkCardGuids.Count()} 个需要聚合的网卡");

            if (!networkCardGuids.Any())
            {
                Log.Warning("没有找到任何全局网络数据，跳过聚合");
                return;
            }

            foreach (var networkCardGuid in networkCardGuids)
            {
                Log.Information($"开始处理网卡: {networkCardGuid}");

                // 第一步：从原始数据聚合到小时数据
                Log.Information($"为网卡 {networkCardGuid} 聚合小时数据");
                await AggregateGlobalNetworkHourlyAsync(networkCardGuid);

                // 第二步：从小时数据聚合到天数据
                Log.Information($"为网卡 {networkCardGuid} 聚合天数据");
                await AggregateGlobalNetworkDailyAsync(networkCardGuid, threeDaysAgo);

                // 第三步：从天数据聚合到周数据
                Log.Information($"为网卡 {networkCardGuid} 聚合周数据");
                await AggregateGlobalNetworkWeeklyAsync(networkCardGuid, oneWeekAgo);
            }

            Log.Information($"全局网络数据聚合完成，处理了 {networkCardGuids.Count()} 个网卡");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "聚合全局网络数据失败");
        }
    }

    /// <summary>
    /// 按小时聚合应用网络数据
    /// </summary>
    private async Task AggregateAppNetworkHourlyAsync(string appId)
    {
        try
        {
            var hourSeconds = 3600;
            Log.Information($"聚合应用 {appId} 的小时数据");

            var connection = _dataService.Networks.GetType()
                .GetField("_context",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_dataService.Networks);
            var contextConnection =
                connection?.GetType().GetProperty("Connection")?.GetValue(connection) as System.Data.IDbConnection;

            if (contextConnection == null)
            {
                Log.Warning("无法获取数据库连接");
                return;
            }

            string sql = @"
                SELECT 
                    (Timestamp / @HourSeconds) * @HourSeconds as HourTimestamp,
                    SUM(UploadBytes) as TotalUpload,
                    SUM(DownloadBytes) as TotalDownload,
                    COUNT(*) as ConnectionCount,
                    COUNT(DISTINCT RemoteIP) as UniqueRemoteIPs,
                    COUNT(DISTINCT RemotePort) as UniqueRemotePorts
                FROM AppNetwork 
                WHERE AppId = @AppId
                GROUP BY (Timestamp / @HourSeconds)
                ORDER BY HourTimestamp";

            var parameters = new { AppId = appId, HourSeconds = hourSeconds };
            var aggregatedData = await contextConnection.QueryAsync(sql, parameters);

            Log.Information($"查询到 {aggregatedData.Count()} 条小时聚合数据");

            int savedCount = 0;
            int skippedCount = 0;

            foreach (dynamic data in aggregatedData)
            {
                var hourTimestamp = (long)data.HourTimestamp;
                var totalUpload = (long)data.TotalUpload;
                var totalDownload = (long)data.TotalDownload;
                var connectionCount = (int)data.ConnectionCount;
                var uniqueIPs = (int)data.UniqueRemoteIPs;
                var uniquePorts = (int)data.UniqueRemotePorts;

                bool exists = await _dataService.Networks.AppNetworkHourlyExistsAsync(appId, hourTimestamp);

                if (!exists)
                {
                    var hourlyData = new AppNetworkHourly
                    {
                        AppId = appId,
                        HourTimestamp = hourTimestamp,
                        TotalUploadBytes = totalUpload,
                        TotalDownloadBytes = totalDownload,
                        ConnectionCount = connectionCount,
                        UniqueRemoteIPs = uniqueIPs,
                        UniqueRemotePorts = uniquePorts,
                        CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    await _dataService.Networks.SaveAppNetworkHourlyAsync(hourlyData);
                    savedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }

            Log.Information($"应用 {appId} 的小时聚合完成，保存了 {savedCount} 条新数据，跳过了 {skippedCount} 条已存在数据");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"聚合应用 {appId} 的小时数据失败");
        }
    }

    /// <summary>
    /// 按天聚合应用网络数据（从小时数据聚合）
    /// </summary>
    private async Task AggregateAppNetworkDailyAsync(string appId, long oneDayAgo)
    {
        try
        {
            Log.Information($"聚合应用 {appId} 的天数据，从小时数据聚合");

            var connection = _dataService.Networks.GetType()
                .GetField("_context",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_dataService.Networks);
            var contextConnection =
                connection?.GetType().GetProperty("Connection")?.GetValue(connection) as System.Data.IDbConnection;

            if (contextConnection == null) return;

            var daySeconds = 86400;
            string sql = @"
                SELECT 
                    (HourTimestamp / @DaySeconds) * @DaySeconds as DayTimestamp,
                    SUM(TotalUploadBytes) as TotalUpload,
                    SUM(TotalDownloadBytes) as TotalDownload,
                    SUM(ConnectionCount) as ConnectionCount,
                    MAX(UniqueRemoteIPs) as UniqueRemoteIPs,
                    MAX(UniqueRemotePorts) as UniqueRemotePorts
                FROM AppNetworkHourly 
                WHERE AppId = @AppId AND HourTimestamp < @OneDayAgo
                GROUP BY (HourTimestamp / @DaySeconds)
                ORDER BY DayTimestamp";

            var parameters = new { AppId = appId, OneDayAgo = oneDayAgo, DaySeconds = daySeconds };
            var aggregatedData = await contextConnection.QueryAsync(sql, parameters);

            int savedCount = 0;
            foreach (dynamic data in aggregatedData)
            {
                var dayTimestamp = (long)data.DayTimestamp;

                bool exists = await _dataService.Networks.AppNetworkDailyExistsAsync(appId, dayTimestamp);
                if (!exists)
                {
                    var dailyData = new AppNetworkDaily
                    {
                        AppId = appId,
                        DayTimestamp = dayTimestamp,
                        TotalUploadBytes = (long)data.TotalUpload,
                        TotalDownloadBytes = (long)data.TotalDownload,
                        ConnectionCount = (int)data.ConnectionCount,
                        UniqueRemoteIPs = (int)data.UniqueRemoteIPs,
                        UniqueRemotePorts = (int)data.UniqueRemotePorts,
                        CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    await _dataService.Networks.SaveAppNetworkDailyAsync(dailyData);
                    savedCount++;
                }
            }

            Log.Information($"应用 {appId} 的天聚合完成，保存了 {savedCount} 条新数据");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"聚合应用 {appId} 的天数据失败");
        }
    }

    /// <summary>
    /// 按周聚合应用网络数据（从天数据聚合）
    /// </summary>
    private async Task AggregateAppNetworkWeeklyAsync(string appId, long oneWeekAgo)
    {
        try
        {
            Log.Information($"聚合应用 {appId} 的周数据，从天数据聚合");

            var connection = _dataService.Networks.GetType()
                .GetField("_context",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_dataService.Networks);
            var contextConnection =
                connection?.GetType().GetProperty("Connection")?.GetValue(connection) as System.Data.IDbConnection;

            if (contextConnection == null) return;

            var weekSeconds = 604800;
            string sql = @"
                SELECT 
                    (DayTimestamp / @WeekSeconds) * @WeekSeconds as WeekTimestamp,
                    SUM(TotalUploadBytes) as TotalUpload,
                    SUM(TotalDownloadBytes) as TotalDownload,
                    SUM(ConnectionCount) as ConnectionCount,
                    MAX(UniqueRemoteIPs) as UniqueRemoteIPs,
                    MAX(UniqueRemotePorts) as UniqueRemotePorts
                FROM AppNetworkDaily 
                WHERE AppId = @AppId AND DayTimestamp < @OneWeekAgo
                GROUP BY (DayTimestamp / @WeekSeconds)
                ORDER BY WeekTimestamp";

            var parameters = new { AppId = appId, OneWeekAgo = oneWeekAgo, WeekSeconds = weekSeconds };
            var aggregatedData = await contextConnection.QueryAsync(sql, parameters);

            int savedCount = 0;
            foreach (dynamic data in aggregatedData)
            {
                var weekTimestamp = (long)data.WeekTimestamp;

                bool exists = await _dataService.Networks.AppNetworkWeeklyExistsAsync(appId, weekTimestamp);
                if (!exists)
                {
                    var weeklyData = new AppNetworkWeekly
                    {
                        AppId = appId,
                        WeekTimestamp = weekTimestamp,
                        TotalUploadBytes = (long)data.TotalUpload,
                        TotalDownloadBytes = (long)data.TotalDownload,
                        ConnectionCount = (int)data.ConnectionCount,
                        UniqueRemoteIPs = (int)data.UniqueRemoteIPs,
                        UniqueRemotePorts = (int)data.UniqueRemotePorts,
                        CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    await _dataService.Networks.SaveAppNetworkWeeklyAsync(weeklyData);
                    savedCount++;
                }
            }

            Log.Information($"应用 {appId} 的周聚合完成，保存了 {savedCount} 条新数据");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"聚合应用 {appId} 的周数据失败");
        }
    }

    /// <summary>
    /// 按月聚合应用网络数据（从周数据聚合）
    /// </summary>
    private async Task AggregateAppNetworkMonthlyAsync(string appId, long oneMonthAgo)
    {
        try
        {
            Log.Information($"聚合应用 {appId} 的月数据，从周数据聚合");

            var connection = _dataService.Networks.GetType()
                .GetField("_context",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_dataService.Networks);
            var contextConnection =
                connection?.GetType().GetProperty("Connection")?.GetValue(connection) as System.Data.IDbConnection;

            if (contextConnection == null) return;

            var monthSeconds = 2592000;
            string sql = @"
                SELECT 
                    (WeekTimestamp / @MonthSeconds) * @MonthSeconds as MonthTimestamp,
                    SUM(TotalUploadBytes) as TotalUpload,
                    SUM(TotalDownloadBytes) as TotalDownload,
                    SUM(ConnectionCount) as ConnectionCount,
                    MAX(UniqueRemoteIPs) as UniqueRemoteIPs,
                    MAX(UniqueRemotePorts) as UniqueRemotePorts
                FROM AppNetworkWeekly 
                WHERE AppId = @AppId AND WeekTimestamp < @OneMonthAgo
                GROUP BY (WeekTimestamp / @MonthSeconds)
                ORDER BY MonthTimestamp";

            var parameters = new { AppId = appId, OneMonthAgo = oneMonthAgo, MonthSeconds = monthSeconds };
            var aggregatedData = await contextConnection.QueryAsync(sql, parameters);

            int savedCount = 0;
            foreach (dynamic data in aggregatedData)
            {
                var monthTimestamp = (long)data.MonthTimestamp;

                bool exists = await _dataService.Networks.AppNetworkMonthlyExistsAsync(appId, monthTimestamp);
                if (!exists)
                {
                    var monthlyData = new AppNetworkMonthly
                    {
                        AppId = appId,
                        MonthTimestamp = monthTimestamp,
                        TotalUploadBytes = (long)data.TotalUpload,
                        TotalDownloadBytes = (long)data.TotalDownload,
                        ConnectionCount = (int)data.ConnectionCount,
                        UniqueRemoteIPs = (int)data.UniqueRemoteIPs,
                        UniqueRemotePorts = (int)data.UniqueRemotePorts,
                        CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    await _dataService.Networks.SaveAppNetworkMonthlyAsync(monthlyData);
                    savedCount++;
                }
            }

            Log.Information($"应用 {appId} 的月聚合完成，保存了 {savedCount} 条新数据");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"聚合应用 {appId} 的月数据失败");
        }
    }

    /// <summary>
    /// 按小时聚合全局网络数据
    /// </summary>
    private async Task AggregateGlobalNetworkHourlyAsync(string networkCardGuid)
    {
        try
        {
            var hourSeconds = 3600;
            Log.Information($"聚合网卡 {networkCardGuid} 的小时数据");

            var connection = _dataService.Networks.GetType()
                .GetField("_context",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_dataService.Networks);
            var contextConnection =
                connection?.GetType().GetProperty("Connection")?.GetValue(connection) as System.Data.IDbConnection;

            if (contextConnection == null)
            {
                Log.Warning("无法获取数据库连接");
                return;
            }

            string sql = @"
                SELECT 
                    (Timestep / @HourSeconds) * @HourSeconds as HourTimestamp,
                    SUM(Upload) as TotalUpload,
                    SUM(Download) as TotalDownload,
                    AVG(Upload) as AvgUpload,
                    AVG(Download) as AvgDownload,
                    MAX(Upload) as MaxUpload,
                    MAX(Download) as MaxDownload,
                    COUNT(*) as RecordCount
                FROM GlobalNetwork 
                WHERE NetworkCardGuid = @NetworkCardGuid
                GROUP BY (Timestep / @HourSeconds)
                ORDER BY HourTimestamp";

            var parameters = new { NetworkCardGuid = networkCardGuid, HourSeconds = hourSeconds };
            var aggregatedData = await contextConnection.QueryAsync(sql, parameters);

            Log.Information($"查询到 {aggregatedData.Count()} 条小时聚合数据");

            int savedCount = 0;
            int skippedCount = 0;

            foreach (dynamic data in aggregatedData)
            {
                var hourTimestamp = (long)data.HourTimestamp;
                var totalUpload = (long)data.TotalUpload;
                var totalDownload = (long)data.TotalDownload;
                var avgUpload = (long)data.AvgUpload;
                var avgDownload = (long)data.AvgDownload;
                var maxUpload = (long)data.MaxUpload;
                var maxDownload = (long)data.MaxDownload;
                var recordCount = (int)data.RecordCount;

                bool exists =
                    await _dataService.Networks.GlobalNetworkHourlyExistsAsync(networkCardGuid, hourTimestamp);

                if (!exists)
                {
                    var hourlyData = new GlobalNetworkHourly
                    {
                        NetworkCardGuid = networkCardGuid,
                        HourTimestamp = hourTimestamp,
                        TotalUpload = totalUpload,
                        TotalDownload = totalDownload,
                        AvgUpload = avgUpload,
                        AvgDownload = avgDownload,
                        MaxUpload = maxUpload,
                        MaxDownload = maxDownload,
                        RecordCount = recordCount,
                        CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    await _dataService.Networks.SaveGlobalNetworkHourlyAsync(hourlyData);
                    savedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }

            Log.Information($"网卡 {networkCardGuid} 的小时聚合完成，保存了 {savedCount} 条新数据，跳过了 {skippedCount} 条已存在数据");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"聚合网卡 {networkCardGuid} 的小时数据失败");
        }
    }

    /// <summary>
    /// 按天聚合全局网络数据（从小时数据聚合）
    /// </summary>
    private async Task AggregateGlobalNetworkDailyAsync(string networkCardGuid, long threeDaysAgo)
    {
        try
        {
            Log.Information($"聚合网卡 {networkCardGuid} 的天数据，从小时数据聚合");

            var connection = _dataService.Networks.GetType()
                .GetField("_context",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_dataService.Networks);
            var contextConnection =
                connection?.GetType().GetProperty("Connection")?.GetValue(connection) as System.Data.IDbConnection;

            if (contextConnection == null) return;

            var daySeconds = 86400;
            string sql = @"
                SELECT 
                    (HourTimestamp / @DaySeconds) * @DaySeconds as DayTimestamp,
                    SUM(TotalUpload) as TotalUpload,
                    SUM(TotalDownload) as TotalDownload,
                    AVG(AvgUpload) as AvgUpload,
                    AVG(AvgDownload) as AvgDownload,
                    MAX(MaxUpload) as MaxUpload,
                    MAX(MaxDownload) as MaxDownload,
                    SUM(RecordCount) as RecordCount
                FROM GlobalNetworkHourly 
                WHERE NetworkCardGuid = @NetworkCardGuid AND HourTimestamp < @ThreeDaysAgo
                GROUP BY (HourTimestamp / @DaySeconds)
                ORDER BY DayTimestamp";

            var parameters = new
                { NetworkCardGuid = networkCardGuid, ThreeDaysAgo = threeDaysAgo, DaySeconds = daySeconds };
            var aggregatedData = await contextConnection.QueryAsync(sql, parameters);

            int savedCount = 0;
            foreach (dynamic data in aggregatedData)
            {
                var dayTimestamp = (long)data.DayTimestamp;

                bool exists = await _dataService.Networks.GlobalNetworkDailyExistsAsync(networkCardGuid, dayTimestamp);
                if (!exists)
                {
                    var dailyData = new GlobalNetworkDaily
                    {
                        NetworkCardGuid = networkCardGuid,
                        DayTimestamp = dayTimestamp,
                        TotalUpload = (long)data.TotalUpload,
                        TotalDownload = (long)data.TotalDownload,
                        AvgUpload = (long)data.AvgUpload,
                        AvgDownload = (long)data.AvgDownload,
                        MaxUpload = (long)data.MaxUpload,
                        MaxDownload = (long)data.MaxDownload,
                        RecordCount = (int)data.RecordCount,
                        CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    await _dataService.Networks.SaveGlobalNetworkDailyAsync(dailyData);
                    savedCount++;
                }
            }

            Log.Information($"网卡 {networkCardGuid} 的天聚合完成，保存了 {savedCount} 条新数据");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"聚合网卡 {networkCardGuid} 的天数据失败");
        }
    }

    /// <summary>
    /// 按周聚合全局网络数据（从天数据聚合）
    /// </summary>
    private async Task AggregateGlobalNetworkWeeklyAsync(string networkCardGuid, long oneWeekAgo)
    {
        try
        {
            Log.Information($"聚合网卡 {networkCardGuid} 的周数据，从天数据聚合");

            var connection = _dataService.Networks.GetType()
                .GetField("_context",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_dataService.Networks);
            var contextConnection =
                connection?.GetType().GetProperty("Connection")?.GetValue(connection) as System.Data.IDbConnection;

            if (contextConnection == null) return;

            var weekSeconds = 604800;
            string sql = @"
                SELECT 
                    (DayTimestamp / @WeekSeconds) * @WeekSeconds as WeekTimestamp,
                    SUM(TotalUpload) as TotalUpload,
                    SUM(TotalDownload) as TotalDownload,
                    AVG(AvgUpload) as AvgUpload,
                    AVG(AvgDownload) as AvgDownload,
                    MAX(MaxUpload) as MaxUpload,
                    MAX(MaxDownload) as MaxDownload,
                    SUM(RecordCount) as RecordCount
                FROM GlobalNetworkDaily 
                WHERE NetworkCardGuid = @NetworkCardGuid AND DayTimestamp < @OneWeekAgo
                GROUP BY (DayTimestamp / @WeekSeconds)
                ORDER BY WeekTimestamp";

            var parameters = new
                { NetworkCardGuid = networkCardGuid, OneWeekAgo = oneWeekAgo, WeekSeconds = weekSeconds };
            var aggregatedData = await contextConnection.QueryAsync(sql, parameters);

            int savedCount = 0;
            foreach (dynamic data in aggregatedData)
            {
                var weekTimestamp = (long)data.WeekTimestamp;

                bool exists =
                    await _dataService.Networks.GlobalNetworkWeeklyExistsAsync(networkCardGuid, weekTimestamp);
                if (!exists)
                {
                    var weeklyData = new GlobalNetworkWeekly
                    {
                        NetworkCardGuid = networkCardGuid,
                        WeekTimestamp = weekTimestamp,
                        TotalUpload = (long)data.TotalUpload,
                        TotalDownload = (long)data.TotalDownload,
                        AvgUpload = (long)data.AvgUpload,
                        AvgDownload = (long)data.AvgDownload,
                        MaxUpload = (long)data.MaxUpload,
                        MaxDownload = (long)data.MaxDownload,
                        RecordCount = (int)data.RecordCount,
                        CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    await _dataService.Networks.SaveGlobalNetworkWeeklyAsync(weeklyData);
                    savedCount++;
                }
            }

            Log.Information($"网卡 {networkCardGuid} 的周聚合完成，保存了 {savedCount} 条新数据");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"聚合网卡 {networkCardGuid} 的周数据失败");
        }
    }


    /// <summary>
    /// 执行数据清理
    /// </summary>
    private async Task<bool> PerformDataCleanupAsync()
    {
        try
        {
            Log.Information("开始执行数据清理任务");

            var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var oneDayAgo = currentTimestamp - 86400; // 1天前
            var oneWeekAgo = currentTimestamp - 604800; // 1周前
            var oneMonthAgo = currentTimestamp - 2592000; // 1个月前 (30天)
            var threeDaysAgo = currentTimestamp - 259200; // 3天前

            // 清理应用网络数据
            await CleanupAppNetworkDataAsync(oneDayAgo, oneWeekAgo, oneMonthAgo);

            // 清理全局网络数据
            await CleanupGlobalNetworkDataAsync(threeDaysAgo, oneWeekAgo);

            Log.Information("数据清理任务完成");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据清理任务失败");
            return false;
        }
    }

    /// <summary>
    /// 清理应用网络数据
    /// </summary>
    private async Task CleanupAppNetworkDataAsync(long oneDayAgo, long oneWeekAgo, long oneMonthAgo)
    {
        try
        {
            Log.Information("开始清理应用网络数据");

            // 删除超过1天的原始数据（已聚合到天级数据）
            var deletedOldData = await _dataService.Networks.DeleteOldAppNetworkDataAsync(oneDayAgo);
            Log.Information($"删除了 {deletedOldData} 条超过1天的应用网络原始数据");

            // 删除超过1周的天级聚合数据（已聚合到周级数据）
            var deletedWeeklyData = await _dataService.Networks.DeleteAppNetworkDailyBeforeAsync(oneWeekAgo);
            Log.Information($"删除了 {deletedWeeklyData} 条超过1周的应用网络天级聚合数据");

            // 删除超过1个月的周级聚合数据（已聚合到月级数据）
            var deletedMonthlyData = await _dataService.Networks.DeleteAppNetworkWeeklyBeforeAsync(oneMonthAgo);
            Log.Information($"删除了 {deletedMonthlyData} 条超过1个月的应用网络周级聚合数据");

            Log.Information("应用网络数据清理完成");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "清理应用网络数据失败");
        }
    }

    /// <summary>
    /// 清理全局网络数据
    /// </summary>
    private async Task CleanupGlobalNetworkDataAsync(long threeDaysAgo, long oneWeekAgo)
    {
        try
        {
            Log.Information("开始清理全局网络数据");

            // 删除超过3天的原始数据（已聚合到周级数据）
            var deletedOldData = await _dataService.Networks.DeleteOldGlobalNetworkDataAsync(threeDaysAgo);
            Log.Information($"删除了 {deletedOldData} 条超过3天的全局网络原始数据");

            // 删除超过1周的周级聚合数据（已聚合到月级数据）
            var deletedMonthlyData = await _dataService.Networks.DeleteGlobalNetworkWeeklyBeforeAsync(oneWeekAgo);
            Log.Information($"删除了 {deletedMonthlyData} 条超过1周的全局网络周级聚合数据");

            Log.Information("全局网络数据清理完成");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "清理全局网络数据失败");
        }
    }


    /// <summary>
    /// 报告统计信息
    /// </summary>
    private void ReportStatistics()
    {
        var total = Interlocked.Read(ref _totalOperations);
        var successful = Interlocked.Read(ref _successfulOperations);
        var successRate = total > 0 ? (double)successful / total * 100 : 0;

        Log.Information($"数据库写入统计: 总操作数={total}, 成功数={successful}, 成功率={successRate:F1}%");
    }

    /// <summary>
    /// 检查应用信息是否存在
    /// </summary>
    public async Task<bool> AppExistsAsync(string appId)
    {
        var existingApp = await _dataService.AppInfos.GetAppByAppIdAsync(appId);
        return existingApp != null;
    }

    public void Dispose()
    {
        Log.Information("正在关闭数据库写入线程管理器...");

        // 先刷新所有批处理数据
        try
        {
            FlushAllBatchesAsync().Wait(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            Log.Warning("关闭时刷新批处理数据失败", ex);
        }

        // 关闭Channel
        _writer.TryComplete();

        // 取消处理线程
        _cancellationTokenSource.Cancel();

        // 等待处理线程完成
        try
        {
            _processingTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            Log.Warning("等待数据库写入线程退出超时", ex);
        }

        _cancellationTokenSource.Dispose();
        _batchSemaphore.Dispose();

        ReportStatistics();
        Log.Information("数据库写入线程管理器已关闭");
    }
}