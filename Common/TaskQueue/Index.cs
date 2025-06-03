// namespace Common.TaskQueue;
//
// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Channels;
// using System.Threading.Tasks;
//
// // 1. 任务接口定义
// public interface ITaskItem
// {
//     string TaskId { get; }
//     TaskPriority Priority { get; }
//     DateTime CreatedAt { get; }
//
//     /// <summary>
//     ///  异步执行方法
//     /// </summary>
//     /// <param name="cancellationToken"></param>
//     /// <returns></returns>
//     Task ExecuteAsync(CancellationToken cancellationToken = default);
// }
//
// public enum TaskPriority
// {
//     Low = 0,
//     Normal = 1,
//     High = 2,
//     Critical = 3
// }
//
// // 2. 基础任务实现
// public abstract class BaseTask : ITaskItem
// {
//     public string TaskId { get; } = Guid.NewGuid().ToString();
//     public TaskPriority Priority { get; protected set; } = TaskPriority.Normal;
//     public DateTime CreatedAt { get; } = DateTime.UtcNow;
//
//     public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);
// }
//
// // 3. ETW网络连接任务示例
// public class NetworkConnectionTask : BaseTask
// {
//     private readonly NetworkConnectionData _data;
//     private readonly INetworkDataStore _dataStore;
//
//     public NetworkConnectionTask(NetworkConnectionData data, INetworkDataStore dataStore)
//     {
//         _data = data;
//         _dataStore = dataStore;
//         Priority = TaskPriority.High; // 网络事件优先级较高
//     }
//
//     public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
//     {
//         // 处理网络连接数据
//         await _dataStore.UpdateNetworkConnectionAsync(_data);
//     }
// }
//
// // 4. 数据库写入任务示例
// public class DatabaseWriteTask : BaseTask
// {
//     private readonly object _data;
//     private readonly Func<object, CancellationToken, Task> _writeAction;
//
//     public DatabaseWriteTask(object data, Func<object, CancellationToken, Task> writeAction)
//     {
//         _data = data;
//         _writeAction = writeAction;
//         Priority = TaskPriority.Normal;
//     }
//
//     public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
//     {
//         await _writeAction(_data, cancellationToken);
//     }
// }
//
// // 5. 任务队列配置
// public class TaskQueueOptions
// {
//     /// <summary>
//     /// 默认并发数 = CPU核心数
//     /// </summary>
//     public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
//
//     /// <summary>
//     /// 队列最大容量
//     /// </summary>
//     public int MaxQueueSize { get; set; } = 10000;
//
//     /// <summary>
//     /// 单个任务超时时间
//     /// </summary>
//     public TimeSpan TaskTimeout { get; set; } = TimeSpan.FromMinutes(5);
//
//     /// <summary>
//     /// 是否启用优先级处理
//     /// </summary>
//     public bool EnablePriorityProcessing { get; set; } = true;
// }
//
// // 6. 核心任务队列实现
// public class TaskQueue : ITaskQueue, IDisposable
// {
//     /// <summary>
//     /// 高性能队列通道
//     /// </summary>
//     private readonly Channel<ITaskItem> _taskChannel;
//
//     private readonly ChannelWriter<ITaskItem> _writer;
//     private readonly ChannelReader<ITaskItem> _reader;
//     private readonly TaskQueueOptions _options;
//
//     /// <summary>
//     /// 并发控制信号量
//     /// </summary>
//     private readonly SemaphoreSlim _concurrencySemaphore;
//
//     /// <summary>
//     /// 正在运行的任务字典
//     /// </summary>
//     private readonly ConcurrentDictionary<string, TaskInfo> _runningTasks;
//
//     /// <summary>
//     /// 取消令牌源
//     /// </summary>
//     private readonly CancellationTokenSource _cancellationTokenSource;
//
//     /// <summary>
//     /// 工作线程数组
//     /// </summary>
//     private readonly Task[] _workerTasks;
//
//     private bool _disposed = false;
//
//     public TaskQueue(TaskQueueOptions options = null)
//     {
//         _options = options ?? new TaskQueueOptions();
//
//         var channelOptions = new BoundedChannelOptions(_options.MaxQueueSize)
//         {
//             FullMode = BoundedChannelFullMode.Wait,  // 队列满时等待，而不是丢弃
//             SingleReader = false,                    // 多个读取者（工作线程）
//             SingleWriter = false                     // 多个写入者（生产者）
//         };
//
//         _taskChannel = Channel.CreateBounded<ITaskItem>(channelOptions);
//         _writer = _taskChannel.Writer;
//         _reader = _taskChannel.Reader;
//
//         _concurrencySemaphore = new SemaphoreSlim(_options.MaxConcurrency, _options.MaxConcurrency);
//         _runningTasks = new ConcurrentDictionary<string, TaskInfo>();
//         _cancellationTokenSource = new CancellationTokenSource();
//
//         // 启动工作线程
//         _workerTasks = new Task[_options.MaxConcurrency];
//         for (int i = 0; i < _options.MaxConcurrency; i++)
//         {
//             _workerTasks[i] = Task.Run(() => ProcessTasksAsync(_cancellationTokenSource.Token));
//         }
//     }
//
//     /// <summary>
//     /// 任务入队方法
//     /// </summary>
//     /// <param name="task"></param>
//     /// <param name="cancellationToken"></param>
//     /// <returns></returns>
//     public async Task<bool> EnqueueAsync(ITaskItem task, CancellationToken cancellationToken = default)
//     {
//         if (_disposed)
//             return false;
//
//         try
//         {
//             // 异步写入，支持取消
//             await _writer.WriteAsync(task, cancellationToken);
//             return true;
//         }
//         catch (Exception)
//         {
//             return false;
//         }
//     }
//
//     public TaskStatistics GetStatistics()
//     {
//         return new TaskStatistics
//         {
//             QueuedTasks = _taskChannel.Reader.Count,
//             RunningTasks = _runningTasks.Count,
//             AvailableWorkers = _concurrencySemaphore.CurrentCount
//         };
//     }
//
//     public async Task StopAsync()
//     {
//         _writer.Complete();
//         _cancellationTokenSource.Cancel();
//
//         try
//         {
//             await Task.WhenAll(_workerTasks);
//         }
//         catch (OperationCanceledException)
//         {
//             // 预期的取消异常
//         }
//     }
//
//     protected async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         var tasks = new List<Task>();
//
//         // 创建多个工作任务
//         for (int i = 0; i < _options.MaxConcurrency; i++)
//         {
//             tasks.Add(ProcessTasksAsync(stoppingToken));
//         }
//
//         await Task.WhenAll(tasks);
//     }
//
//     /// <summary>
//     /// 异步任务处理
//     /// </summary>
//     /// <param name="cancellationToken"></param>
//     private async Task ProcessTasksAsync(CancellationToken cancellationToken)
//     {
//         try
//         {
//             // 异步枚举，从Channel读取任务
//             await foreach (var task in _reader.ReadAllAsync(cancellationToken))
//             {
//                 // 1. 获取信号量，控制并发数
//                 await _concurrencySemaphore.WaitAsync(cancellationToken);
//
//                 // 2. 记录任务信息
//                 var taskInfo = new TaskInfo
//                 {
//                     Task = task,
//                     StartTime = DateTime.UtcNow
//                 };
//
//                 _runningTasks.TryAdd(task.TaskId, taskInfo);
//
//                 // 异步执行任务，不阻塞队列处理
//                 _ = Task.Run(async () =>
//                 {
//                     try
//                     {
//                         // 创建带超时的取消令牌
//                         using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
//                         cts.CancelAfter(_options.TaskTimeout);
//
//                         await task.ExecuteAsync(cts.Token);
//                     }
//                     catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
//                     {
//                         // 任务被取消
//                     }
//                     catch (Exception)
//                     {
//                         // 任务执行失败，静默处理
//                     }
//                     finally
//                     {
//                         _runningTasks.TryRemove(task.TaskId, out _);
//                         _concurrencySemaphore.Release();
//                     }
//                 }, cancellationToken);
//             }
//         }
//         catch (OperationCanceledException)
//         {
//             // 工作线程被取消
//         }
//     }
//
//     public void Dispose()
//     {
//         if (_disposed)
//             return;
//
//         _disposed = true;
//         _writer.Complete();
//         _cancellationTokenSource.Cancel();
//
//         try
//         {
//             Task.WaitAll(_workerTasks, TimeSpan.FromSeconds(5));
//         }
//         catch (Exception)
//         {
//             // 忽略关闭时的异常
//         }
//
//         _concurrencySemaphore?.Dispose();
//         _cancellationTokenSource?.Dispose();
//     }
// }
//
// // 7. 接口定义
// public interface ITaskQueue
// {
//     Task<bool> EnqueueAsync(ITaskItem task, CancellationToken cancellationToken = default);
//     TaskStatistics GetStatistics();
//     Task StopAsync();
// }
//
// // 8. 辅助类
// public class TaskInfo
// {
//     public ITaskItem Task { get; set; }
//     public DateTime StartTime { get; set; }
// }
//
// public class TaskStatistics
// {
//     public int QueuedTasks { get; set; }
//     public int RunningTasks { get; set; }
//     public int AvailableWorkers { get; set; }
// }
//
// // 9. 网络数据存储接口（单例数据存储）
// public interface INetworkDataStore
// {
//     Task UpdateNetworkConnectionAsync(NetworkConnectionData data);
//     Task<IEnumerable<NetworkConnectionData>> GetRecentConnectionsAsync(TimeSpan timeSpan);
//     Task<NetworkStatistics> GetStatisticsAsync();
// }
//
// // 10. 网络数据存储单例实现
// public class NetworkDataStore : INetworkDataStore
// {
//     private readonly ConcurrentQueue<NetworkConnectionData> _recentConnections;      // 最近连接队列
//     private readonly ConcurrentDictionary<string, ConnectionStatistics> _connectionStats; // 连接统计
//     private readonly ReaderWriterLockSlim _lock;                                    // 读写锁
//     private readonly Timer _cleanupTimer;                                           // 清理定时器
//     public NetworkDataStore()
//     {
//         _recentConnections = new ConcurrentQueue<NetworkConnectionData>();
//         _connectionStats = new ConcurrentDictionary<string, ConnectionStatistics>();
//         _lock = new ReaderWriterLockSlim();
//
//         // 定期清理过期数据
//         _cleanupTimer = new Timer(CleanupExpiredData, null,
//             TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
//     }
//
//     public async Task UpdateNetworkConnectionAsync(NetworkConnectionData data)
//     {
//         await Task.Run(() =>
//         {
//             _recentConnections.Enqueue(data);
//
//             // 更新统计信息
//             var key = $"{data.ProcessName}:{data.LocalPort}";
//             _connectionStats.AddOrUpdate(key,
//                 new ConnectionStatistics { Count = 1, LastSeen = DateTime.UtcNow },
//                 (k, v) => new ConnectionStatistics { Count = v.Count + 1, LastSeen = DateTime.UtcNow });
//         });
//     }
//
//     public async Task<IEnumerable<NetworkConnectionData>> GetRecentConnectionsAsync(TimeSpan timeSpan)
//     {
//         return await Task.FromResult(
//             _recentConnections
//                 .Where(c => DateTime.UtcNow - c.Timestamp <= timeSpan)
//                 .ToList());
//     }
//
//     public async Task<NetworkStatistics> GetStatisticsAsync()
//     {
//         return await Task.FromResult(new NetworkStatistics
//         {
//             TotalConnections = _recentConnections.Count,
//             ActiveProcesses = _connectionStats.Count,
//             LastUpdate = DateTime.UtcNow
//         });
//     }
//
//     private void CleanupExpiredData(object state)
//     {
//         var cutoff = DateTime.UtcNow.AddHours(-1);
//
//         // 清理过期连接数据
//         while (_recentConnections.TryPeek(out var connection) &&
//                connection.Timestamp < cutoff)
//         {
//             _recentConnections.TryDequeue(out _);
//         }
//     }
// }
//
// // 11. 数据模型
// public class NetworkConnectionData
// {
//     public string ProcessName { get; set; }
//     public int ProcessId { get; set; }
//     public string LocalAddress { get; set; }
//     public int LocalPort { get; set; }
//     public string RemoteAddress { get; set; }
//     public int RemotePort { get; set; }
//     public DateTime Timestamp { get; set; } = DateTime.UtcNow;
//     public string Protocol { get; set; }
// }
//
// public class ConnectionStatistics
// {
//     public int Count { get; set; }
//     public DateTime LastSeen { get; set; }
// }
//
// public class NetworkStatistics
// {
//     public int TotalConnections { get; set; }
//     public int ActiveProcesses { get; set; }
//     public DateTime LastUpdate { get; set; }
// }
//
// // 12. 简单使用示例
// public class TaskQueueManager
// {
//     private readonly TaskQueue _taskQueue;
//     private readonly NetworkDataStore _dataStore;
//
//     public TaskQueueManager()
//     {
//         var options = new TaskQueueOptions
//         {
//             MaxConcurrency = Environment.ProcessorCount,
//             MaxQueueSize = 5000
//         };
//
//         _taskQueue = new TaskQueue(options);
//         _dataStore = new NetworkDataStore();
//     }
//
//     // ETW事件处理
//     public async Task OnNetworkConnectionEvent(NetworkConnectionData connectionData)
//     {
//         var task = new NetworkConnectionTask(connectionData, _dataStore);
//         await _taskQueue.EnqueueAsync(task);
//     }
//
//     // 数据库写入
//     public async Task SaveToDatabase(object data)
//     {
//         var dbTask = new DatabaseWriteTask(data, async (d, ct) =>
//         {
//             // 实际的数据库写入逻辑
//             await WriteToDatabase(d);
//         });
//
//         await _taskQueue.EnqueueAsync(dbTask);
//     }
//
//     // 获取统计信息
//     public TaskStatistics GetQueueStatistics()
//     {
//         return _taskQueue.GetStatistics();
//     }
//
//     // 获取网络数据
//     public async Task<IEnumerable<NetworkConnectionData>> GetRecentConnections(TimeSpan timeSpan)
//     {
//         return await _dataStore.GetRecentConnectionsAsync(timeSpan);
//     }
//
//     private async Task WriteToDatabase(object data)
//     {
//         // 数据库写入实现
//         await Task.Delay(100); // 模拟数据库操作
//     }
//
//     public async Task StopAsync()
//     {
//         await _taskQueue.StopAsync();
//         _taskQueue.Dispose();
//     }
// }
