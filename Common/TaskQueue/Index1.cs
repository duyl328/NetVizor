using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

// 1. 任务接口定义
public interface ITaskItem
{
    string TaskId { get; }
    TaskPriority Priority { get; }
    DateTime CreatedAt { get; }
    int SequenceNumber { get; set; } // 用于严格排序
    string GroupId { get; } // 用于分组排序
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

public enum TaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3,
    Urgent = 4 // 最高优先级，用于插队
}

// 2. 基础任务实现
public abstract class BaseTask : ITaskItem
{
    public string TaskId { get; } = Guid.NewGuid().ToString();
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public int SequenceNumber { get; set; } = 0;
    public string GroupId { get; set; } = null;

    public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);
}

// 3. 支持分组的任务基类
public abstract class GroupedTask : BaseTask
{
    protected GroupedTask(string? groupId)
    {
        GroupId = groupId ?? throw new ArgumentNullException(nameof(groupId));
    }
}

// 4. 网络连接任务（支持分组）
public class NetworkConnectionTask : GroupedTask
{
    private readonly NetworkConnectionData _data;
    private readonly INetworkDataStore _dataStore;

    public NetworkConnectionTask(NetworkConnectionData data, INetworkDataStore dataStore, string? groupId = null)
        : base(groupId ?? $"network_{data.ProcessName}")
    {
        _data = data;
        _dataStore = dataStore;
        Priority = TaskPriority.High;
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await _dataStore.UpdateNetworkConnectionAsync(_data);
    }
}

// 5. 数据库写入任务
public class DatabaseWriteTask : BaseTask
{
    private readonly object _data;
    private readonly Func<object, CancellationToken, Task> _writeAction;

    public DatabaseWriteTask(object data, Func<object, CancellationToken, Task> writeAction,
        TaskPriority priority = TaskPriority.Normal)
    {
        _data = data;
        _writeAction = writeAction;
        Priority = priority;
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await _writeAction(_data, cancellationToken);
    }
}

// 6. 任务队列配置
public class TaskQueueOptions
{
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public int MaxQueueSize { get; set; } = 10000;
    public TimeSpan TaskTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnablePriorityProcessing { get; set; } = true;
    public bool EnableGroupedExecution { get; set; } = true; // 是否启用分组执行
    public TimeSpan StatisticsUpdateInterval { get; set; } = TimeSpan.FromSeconds(1);
}

// 7. 任务统计信息
public class TaskStatistics
{
    public int QueuedTasks { get; set; }
    public int RunningTasks { get; set; }
    public int AvailableWorkers { get; set; }
    public long TotalEnqueued { get; set; } // 总入队任务数
    public long TotalCompleted { get; set; } // 总完成任务数
    public long TotalFailed { get; set; } // 总失败任务数
    public double AverageExecutionTime { get; set; } // 平均执行时间（毫秒）
    public Dictionary<TaskPriority, int> QueuedByPriority { get; set; } = new();
    public Dictionary<string, int> QueuedByGroup { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

// 8. 任务信息
public class TaskInfo
{
    public ITaskItem Task { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    public Exception Exception { get; set; }

    public TimeSpan ExecutionTime => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.UtcNow - StartTime;
}

// 9. 优先级任务比较器
public class TaskPriorityComparer : IComparer<ITaskItem>
{
    public int Compare(ITaskItem x, ITaskItem y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;

        // 1. 先按优先级排序（优先级高的在前）
        var priorityComparison = y.Priority.CompareTo(x.Priority);
        if (priorityComparison != 0) return priorityComparison;

        // 2. 同优先级按序列号排序
        if (x.SequenceNumber != 0 || y.SequenceNumber != 0)
        {
            return x.SequenceNumber.CompareTo(y.SequenceNumber);
        }

        // 3. 最后按创建时间排序（FIFO）
        return x.CreatedAt.CompareTo(y.CreatedAt);
    }
}

// 10. 增强的任务队列实现
public class EnhancedTaskQueue : ITaskQueue, IDisposable
{
    private readonly PriorityQueue<ITaskItem, ITaskItem> _priorityQueue;
    private readonly TaskQueueOptions _options;
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly ConcurrentDictionary<string, TaskInfo> _runningTasks;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _groupSemaphores;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task[] _workerTasks;
    private readonly ReaderWriterLockSlim _queueLock;
    private readonly Timer _statisticsTimer;

    // 统计数据
    private long _totalEnqueued = 0;
    private long _totalCompleted = 0;
    private long _totalFailed = 0;
    private readonly ConcurrentQueue<double> _executionTimes = new();

    /// <summary>
    /// 当前序列号
    /// </summary>
    private int _currentSequenceNumber = 0;

    private bool _disposed = false;
    private readonly ManualResetEventSlim _newTaskEvent;

    public EnhancedTaskQueue(TaskQueueOptions? options = null)
    {
        _options = options ?? new TaskQueueOptions();

        _priorityQueue = new PriorityQueue<ITaskItem, ITaskItem>(new TaskPriorityComparer());
        _concurrencySemaphore = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);
        _runningTasks = new ConcurrentDictionary<string, TaskInfo>();
        _groupSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        _cancellationTokenSource = new CancellationTokenSource();
        _queueLock = new ReaderWriterLockSlim();
        _newTaskEvent = new ManualResetEventSlim(false);

        // 启动统计更新定时器
        _statisticsTimer = new Timer(UpdateStatistics, null,
            _options.StatisticsUpdateInterval, _options.StatisticsUpdateInterval);

        // 启动工作线程
        _workerTasks = new Task[_options.MaxConcurrency];
        for (int i = 0; i < _options.MaxConcurrency; i++)
        {
            _workerTasks[i] = Task.Run(() => ProcessTasksAsync(_cancellationTokenSource.Token));
        }
    }

    public async Task<bool> EnqueueAsync(ITaskItem task, CancellationToken cancellationToken = default)
    {
        if (_disposed) return false;

        try
        {
            _queueLock.EnterWriteLock();

            // 检查队列大小
            if (_priorityQueue.Count >= _options.MaxQueueSize)
            {
                return false;
            }

            // 如果没有设置序列号，自动分配
            if (task.SequenceNumber == 0)
            {
                task.SequenceNumber = Interlocked.Increment(ref _currentSequenceNumber);
            }

            _priorityQueue.Enqueue(task, task);
            Interlocked.Increment(ref _totalEnqueued);

            _newTaskEvent.Set(); // 通知工作线程有新任务
            return true;
        }
        finally
        {
            _queueLock.ExitWriteLock();
        }
    }

    public async Task<bool> EnqueueUrgentAsync(ITaskItem task, CancellationToken cancellationToken = default)
    {
        // 设置为最高优先级并立即处理
        if (task is BaseTask baseTask)
        {
            baseTask.Priority = TaskPriority.Urgent;
        }

        return await EnqueueAsync(task, cancellationToken);
    }

    public async Task<bool> EnqueueSequentialAsync(IEnumerable<ITaskItem> tasks, string? groupId = null)
    {
        if (_disposed) return false;

        var taskList = tasks.ToList();
        if (!taskList.Any()) return true;

        var actualGroupId = groupId ?? Guid.NewGuid().ToString();

        try
        {
            _queueLock.EnterWriteLock();

            // 检查队列容量
            if (_priorityQueue.Count + taskList.Count > _options.MaxQueueSize)
            {
                return false;
            }

            var sequenceStart = Interlocked.Add(ref _currentSequenceNumber, taskList.Count) - taskList.Count + 1;

            for (int i = 0; i < taskList.Count; i++)
            {
                var task = taskList[i];
                task.SequenceNumber = sequenceStart + i;

                // 设置分组ID
                if (task is BaseTask baseTask && string.IsNullOrEmpty(baseTask.GroupId))
                {
                    baseTask.GroupId = actualGroupId;
                }

                _priorityQueue.Enqueue(task, task);
                Interlocked.Increment(ref _totalEnqueued);
            }

            _newTaskEvent.Set();
            return true;
        }
        finally
        {
            _queueLock.ExitWriteLock();
        }
    }

    public TaskStatistics GetStatistics()
    {
        var stats = new TaskStatistics
        {
            RunningTasks = _runningTasks.Count,
            AvailableWorkers = _concurrencySemaphore.CurrentCount,
            TotalEnqueued = Interlocked.Read(ref _totalEnqueued),
            TotalCompleted = Interlocked.Read(ref _totalCompleted),
            TotalFailed = Interlocked.Read(ref _totalFailed),
            LastUpdated = DateTime.UtcNow
        };

        // 计算平均执行时间
        if (_executionTimes.Count > 0)
        {
            stats.AverageExecutionTime = _executionTimes.Average();
        }

        // 获取队列统计
        _queueLock.EnterReadLock();
        try
        {
            stats.QueuedTasks = _priorityQueue.Count;

            // 按优先级和分组统计（这里简化处理，实际中可能需要遍历队列）
            var tempQueue = new List<ITaskItem>();
            while (_priorityQueue.Count > 0)
            {
                var task = _priorityQueue.Dequeue();
                tempQueue.Add(task);

                // 统计优先级
                if (!stats.QueuedByPriority.ContainsKey(task.Priority))
                    stats.QueuedByPriority[task.Priority] = 0;
                stats.QueuedByPriority[task.Priority]++;

                // 统计分组
                if (!string.IsNullOrEmpty(task.GroupId))
                {
                    stats.QueuedByGroup.TryAdd(task.GroupId, 0);
                    stats.QueuedByGroup[task.GroupId]++;
                }
            }

            // 重新入队
            foreach (var task in tempQueue)
            {
                _priorityQueue.Enqueue(task, task);
            }
        }
        finally
        {
            _queueLock.ExitReadLock();
        }

        return stats;
    }

    public List<ITaskItem> GetQueuedTasks()
    {
        _queueLock.EnterReadLock();
        try
        {
            var result = new List<ITaskItem>();
            var tempQueue = new List<ITaskItem>();

            while (_priorityQueue.Count > 0)
            {
                var task = _priorityQueue.Dequeue();
                tempQueue.Add(task);
                result.Add(task);
            }

            // 重新入队
            foreach (var task in tempQueue)
            {
                _priorityQueue.Enqueue(task, task);
            }

            return result;
        }
        finally
        {
            _queueLock.ExitReadLock();
        }
    }

    public List<TaskInfo> GetRunningTasks()
    {
        return _runningTasks.Values.ToList();
    }

    private async Task ProcessTasksAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ITaskItem? task = null;

            // 从优先级队列获取任务
            _queueLock.EnterWriteLock();
            try
            {
                if (_priorityQueue.Count > 0)
                {
                    task = _priorityQueue.Dequeue();
                }
            }
            finally
            {
                _queueLock.ExitWriteLock();
            }

            if (task == null)
            {
                // 没有任务，等待新任务信号
                try
                {
                    _newTaskEvent.Wait(1000, cancellationToken); // 最多等待1秒
                    _newTaskEvent.Reset();
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                continue;
            }

            // 控制并发数
            await _concurrencySemaphore.WaitAsync(cancellationToken);

            // 处理分组任务的串行执行
            SemaphoreSlim? groupSemaphore = null;
            if (_options.EnableGroupedExecution && !string.IsNullOrEmpty(task.GroupId))
            {
                groupSemaphore = _groupSemaphores.GetOrAdd(task.GroupId, _ => new SemaphoreSlim(1, 1));
                await groupSemaphore.WaitAsync(cancellationToken);
            }

            var taskInfo = new TaskInfo
            {
                Task = task,
                StartTime = DateTime.UtcNow
            };

            _runningTasks.TryAdd(task.TaskId, taskInfo);

            // 异步执行任务
            _ = Task.Run(async () =>
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(_options.TaskTimeout);

                    await task.ExecuteAsync(cts.Token);

                    taskInfo.IsCompleted = true;
                    taskInfo.EndTime = DateTime.UtcNow;
                    Interlocked.Increment(ref _totalCompleted);

                    // 记录执行时间
                    var executionTime = taskInfo.ExecutionTime.TotalMilliseconds;
                    _executionTimes.Enqueue(executionTime);

                    // 限制执行时间队列大小
                    while (_executionTimes.Count > 1000)
                    {
                        _executionTimes.TryDequeue(out _);
                    }
                }
                catch (Exception ex)
                {
                    taskInfo.IsFailed = true;
                    taskInfo.Exception = ex;
                    taskInfo.EndTime = DateTime.UtcNow;
                    Interlocked.Increment(ref _totalFailed);
                }
                finally
                {
                    _runningTasks.TryRemove(task.TaskId, out _);
                    _concurrencySemaphore.Release();
                    groupSemaphore?.Release();
                }
            }, cancellationToken);
        }
    }

    private void UpdateStatistics(object state)
    {
        // 定期更新统计信息的逻辑
        // 这里可以添加更复杂的统计计算
    }

    public async Task StopAsync()
    {
        if (_disposed) return;

        _cancellationTokenSource.Cancel();
        _newTaskEvent.Set(); // 唤醒等待的工作线程

        try
        {
            await Task.WhenAll(_workerTasks);
        }
        catch (OperationCanceledException)
        {
            // 预期的取消异常
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _cancellationTokenSource.Cancel();
        _newTaskEvent.Set();

        try
        {
            Task.WaitAll(_workerTasks, TimeSpan.FromSeconds(5));
        }
        catch (Exception)
        {
            // 忽略关闭时的异常
        }

        _concurrencySemaphore?.Dispose();
        _cancellationTokenSource?.Dispose();
        _queueLock?.Dispose();
        _newTaskEvent?.Dispose();
        _statisticsTimer?.Dispose();

        // 清理分组信号量
        foreach (var semaphore in _groupSemaphores.Values)
        {
            semaphore.Dispose();
        }
    }
}

// 11. 增强的接口定义
public interface ITaskQueue
{
    Task<bool> EnqueueAsync(ITaskItem task, CancellationToken cancellationToken = default);
    Task<bool> EnqueueUrgentAsync(ITaskItem task, CancellationToken cancellationToken = default);
    Task<bool> EnqueueSequentialAsync(IEnumerable<ITaskItem> tasks, string groupId = null);
    TaskStatistics GetStatistics();
    List<ITaskItem> GetQueuedTasks();
    List<TaskInfo> GetRunningTasks();
    Task StopAsync();
}

// 12. 网络数据存储接口和实现（保持原有实现）
public interface INetworkDataStore
{
    Task UpdateNetworkConnectionAsync(NetworkConnectionData data);
    Task<IEnumerable<NetworkConnectionData>> GetRecentConnectionsAsync(TimeSpan timeSpan);
    Task<NetworkStatistics> GetStatisticsAsync();
}

public class NetworkDataStore : INetworkDataStore
{
    private readonly ConcurrentQueue<NetworkConnectionData> _recentConnections;
    private readonly ConcurrentDictionary<string, ConnectionStatistics> _connectionStats;
    private readonly Timer _cleanupTimer;

    public NetworkDataStore()
    {
        _recentConnections = new ConcurrentQueue<NetworkConnectionData>();
        _connectionStats = new ConcurrentDictionary<string, ConnectionStatistics>();

        _cleanupTimer = new Timer(CleanupExpiredData, null,
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task UpdateNetworkConnectionAsync(NetworkConnectionData data)
    {
        await Task.Run(() =>
        {
            _recentConnections.Enqueue(data);

            var key = $"{data.ProcessName}:{data.LocalPort}";
            _connectionStats.AddOrUpdate(key,
                new ConnectionStatistics { Count = 1, LastSeen = DateTime.UtcNow },
                (k, v) => new ConnectionStatistics { Count = v.Count + 1, LastSeen = DateTime.UtcNow });
        });
    }

    public async Task<IEnumerable<NetworkConnectionData>> GetRecentConnectionsAsync(TimeSpan timeSpan)
    {
        return await Task.FromResult(
            _recentConnections
                .Where(c => DateTime.UtcNow - c.Timestamp <= timeSpan)
                .ToList());
    }

    public async Task<NetworkStatistics> GetStatisticsAsync()
    {
        return await Task.FromResult(new NetworkStatistics
        {
            TotalConnections = _recentConnections.Count,
            ActiveProcesses = _connectionStats.Count,
            LastUpdate = DateTime.UtcNow
        });
    }

    private void CleanupExpiredData(object state)
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);

        while (_recentConnections.TryPeek(out var connection) &&
               connection.Timestamp < cutoff)
        {
            _recentConnections.TryDequeue(out _);
        }
    }
}

// 13. 数据模型
public class NetworkConnectionData
{
    public string ProcessName { get; set; }
    public int ProcessId { get; set; }
    public string LocalAddress { get; set; }
    public int LocalPort { get; set; }
    public string RemoteAddress { get; set; }
    public int RemotePort { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Protocol { get; set; }
}

public class ConnectionStatistics
{
    public int Count { get; set; }
    public DateTime LastSeen { get; set; }
}

public class NetworkStatistics
{
    public int TotalConnections { get; set; }
    public int ActiveProcesses { get; set; }
    public DateTime LastUpdate { get; set; }
}

// 14. 增强的任务队列管理器
public class EnhancedTaskQueueManager
{
    private readonly EnhancedTaskQueue _taskQueue;
    private readonly NetworkDataStore _dataStore;

    public EnhancedTaskQueueManager(TaskQueueOptions options = null)
    {
        var defaultOptions = new TaskQueueOptions
        {
            MaxConcurrency = Environment.ProcessorCount,
            MaxQueueSize = 5000,
            EnableGroupedExecution = true,
            EnablePriorityProcessing = true
        };

        _taskQueue = new EnhancedTaskQueue(options ?? defaultOptions);
        _dataStore = new NetworkDataStore();
    }

    // 普通任务入队
    public async Task<bool> EnqueueNetworkTask(NetworkConnectionData data)
    {
        var task = new NetworkConnectionTask(data, _dataStore);
        return await _taskQueue.EnqueueAsync(task);
    }

    // 紧急任务插队
    public async Task<bool> EnqueueUrgentNetworkTask(NetworkConnectionData data)
    {
        var task = new NetworkConnectionTask(data, _dataStore);
        return await _taskQueue.EnqueueUrgentAsync(task);
    }

    // 批量有序任务
    public async Task<bool> EnqueueSequentialDatabaseWrites(IEnumerable<object> dataList)
    {
        var tasks = dataList.Select(data => new DatabaseWriteTask(data,
            async (d, ct) => await WriteToDatabase(d),
            TaskPriority.Normal)).Cast<ITaskItem>();

        return await _taskQueue.EnqueueSequentialAsync(tasks, "database_batch");
    }

    // 获取详细统计信息
    public TaskStatistics GetDetailedStatistics()
    {
        return _taskQueue.GetStatistics();
    }

    // 获取队列中的任务列表
    public List<ITaskItem> GetPendingTasks()
    {
        return _taskQueue.GetQueuedTasks();
    }

    // 获取正在执行的任务
    public List<TaskInfo> GetActiveTasks()
    {
        return _taskQueue.GetRunningTasks();
    }

    // 获取网络数据
    public async Task<IEnumerable<NetworkConnectionData>> GetRecentConnections(TimeSpan timeSpan)
    {
        return await _dataStore.GetRecentConnectionsAsync(timeSpan);
    }

    private async Task WriteToDatabase(object data)
    {
        // 模拟数据库写入
        await Task.Delay(100);
    }

    public async Task StopAsync()
    {
        await _taskQueue.StopAsync();
        _taskQueue.Dispose();
    }
}
// 保留一个任务队列工具函数；
// 提供公共阿德网络处理模板；
// 尝试获取所有的网络连接信息；
// 如何模拟低性能设设备的软件运行情况？
// 提供公共的网络处理任务类，并将其投入使用；
// 提供全局的公共的，唯一的信息类（提供软件开启状态；网络连接；等其他信息） 将网络信息及其他信息进程整合
//     页面展示提供多种状态：（仅联网软件；已打开软件）
