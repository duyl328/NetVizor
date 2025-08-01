using System.Collections.Concurrent;
using System.Threading.Channels;
using Common.Logger;
using Data.Models;
using Data.Services;

namespace Data.Core;

/// <summary>
/// 数据库写入操作类型
/// </summary>
public enum DbOperationType
{
    SaveAppInfo,
    SaveAppNetwork,
    SaveGlobalNetwork,
    AggregateData,
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

            // TODO: 实现数据聚合逻辑
            // 这里会实现按小时、天的数据聚合

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
    /// 执行数据清理
    /// </summary>
    private async Task<bool> PerformDataCleanupAsync()
    {
        try
        {
            Log.Information("开始执行数据清理任务");

            // TODO: 实现数据清理逻辑
            // 删除过期的原始数据

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