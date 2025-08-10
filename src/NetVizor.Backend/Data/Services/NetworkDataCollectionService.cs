using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Application.Utils;
using Common.Logger;
using Common.Utils;
using Data.Core;
using Data.Models;
using Infrastructure.utils;
using Utils.ETW.Etw;

namespace Data.Services;

/// <summary>
/// 网络数据收集服务
/// 负责全局网速监控和ETW网络数据收集
/// </summary>
public class NetworkDataCollectionService : IDisposable
{
    private readonly DatabaseWriteManager _writeManager;
    private readonly Timer _globalNetworkTimer;
    private readonly Timer _etwNetworkTimer;
    private readonly Timer _networkInterfaceUpdateTimer;
    private readonly Timer _batchFlushTimer;
    private readonly Timer _dataCleanupTimer;

    // 网卡信息缓存
    private readonly ConcurrentDictionary<string, BasicNetworkMonitor.BasicNetworkInterface> _networkInterfaces = new();
    private DateTime _lastNetworkInterfaceUpdate = DateTime.MinValue;

    // ETW数据缓存（只保留最活跃的前100条）
    private readonly ConcurrentDictionary<string, AppNetworkData> _activeConnections = new();

    // AppId生成工具 - 缓存原始AppId
    private readonly ConcurrentDictionary<int, string> _processOriginalAppIdCache = new();

    public NetworkDataCollectionService(DatabaseWriteManager writeManager)
    {
        _writeManager = writeManager;

        // 每5秒收集一次全局网络数据
        _globalNetworkTimer = new Timer(CollectGlobalNetworkData, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        // 每分钟收集一次ETW网络数据（需要管理员权限）
        _etwNetworkTimer = new Timer(CollectEtwNetworkData, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        // 每分钟更新一次网卡列表
        _networkInterfaceUpdateTimer = new Timer(UpdateNetworkInterfaces, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        // 每30秒强制刷新一次批处理缓存
        _batchFlushTimer = new Timer(async _ => await _writeManager.FlushAllBatchesAsync(), null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        // 每小时检测清理并合并旧数据
        _dataCleanupTimer = new Timer(TriggerDataCleanupAndAggregation, null,
            TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

        Log.Information("网络数据收集服务已启动");
    }

    /// <summary>
    /// 收集全局网络数据
    /// </summary>
    private async void CollectGlobalNetworkData(object state)
    {
        try
        {
            var connectedInterfaces = _networkInterfaces.Values
                .Where(i => i.IsConnected)
                .ToList();

            if (connectedInterfaces.Count == 0)
            {
                Log.Debug("没有找到活跃的网络接口");
                return;
            }

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (var networkInterface in connectedInterfaces)
            {
                try
                {
                    var speed = BasicNetworkMonitor.CalculateSpeedById(networkInterface.Id);

                    if (speed.DownloadSpeed > 0 || speed.UploadSpeed > 0)
                    {
                        var globalNetwork = new GlobalNetwork
                        {
                            Timestep = timestamp,
                            Upload = (long)speed.UploadSpeed,
                            Download = (long)speed.DownloadSpeed,
                            NetworkCardGuid = networkInterface.Id
                        };

                        await _writeManager.SaveGlobalNetworkAsync(globalNetwork);

                        Log.Debug(
                            $"记录网卡 {networkInterface.Name} 速度: 上传={speed.UploadSpeedText}, 下载={speed.DownloadSpeedText}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"收集网卡 {networkInterface.Name} 数据时出错");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "收集全局网络数据时发生错误");
        }
    }

    /// <summary>
    /// 收集ETW网络数据（需要管理员权限）
    /// </summary>
    private async void CollectEtwNetworkData(object state)
    {
        try
        {
            // 检查是否有管理员权限
            if (!SysHelper.IsAdministrator())
            {
                Log.Warning("ETW网络监控需要管理员权限，跳过收集");
                return;
            }

            var snapshot = GlobalNetworkMonitor.Instance.GetSnapshot();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            Log.Information($"开始收集ETW网络数据，共找到 {snapshot.Applications.Count} 个应用程序");

            foreach (var app in snapshot.Applications)
            {
                try
                {
                    // 生成或获取AppId
                    var (appId, originalAppId) = await GetOrGenerateAppIdAsync(app.ProcessId, app.ProgramInfo);

                    if (string.IsNullOrEmpty(appId))
                    {
                        continue;
                    }

                    // 保存应用信息到数据库（如果不存在）
                    if (app.ProgramInfo != null)
                    {
                        // 检查应用信息是否已存在
                        var appExists = await _writeManager.AppExistsAsync(appId);

                        if (!appExists)
                        {
                            var appInfo = new AppInfo
                            {
                                AppId = appId,
                                OriginalAppId = originalAppId,
                                Name = app.ProgramInfo.ProcessName ?? "Unknown",
                                Path = app.ProgramInfo.MainModulePath ?? "",
                                Version = app.ProgramInfo.Version ?? "",
                                Company = app.ProgramInfo.CompanyName ?? "",
                                Base64Icon = app.ProgramInfo.IconBase64 ?? "",
                                Hash = app.ProgramInfo.FileDescription ?? ""
                            };

                            await _writeManager.SaveAppInfoAsync(appInfo);
                        }
                    }

                    // 只保留最活跃的前100个连接
                    var activeConnections = app.Connections
                        .Where(c => c.IsActive && (c.BytesSent > 0 || c.BytesReceived > 0))
                        .OrderByDescending(c => c.BytesSent + c.BytesReceived)
                        .Take(100)
                        .ToList();

                    foreach (var connection in activeConnections)
                    {
                        var appNetwork = new AppNetwork
                        {
                            Timestamp = timestamp,
                            LocalIP = connection.LocalIp,
                            LocalPort = connection.LocalPort,
                            RemoteIP = connection.RemoteIp,
                            RemotePort = connection.RemotePort,
                            Protocol = connection.Protocol,
                            UploadBytes = connection.BytesSent,
                            DownloadBytes = connection.BytesReceived,
                            AppId = appId
                        };

                        await _writeManager.SaveAppNetworkAsync(appNetwork);
                    }

                    if (activeConnections.Count > 0)
                    {
                        Log.Debug($"记录应用 {app.ProgramInfo?.ProcessName} 的 {activeConnections.Count} 个网络连接");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"处理应用 {app.ProcessId} 的网络数据时出错", ex);
                }
            }

            Log.Information($"ETW网络数据收集完成，处理了 {snapshot.Applications.Count} 个应用程序");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "收集ETW网络数据时发生错误");
        }
    }

    /// <summary>
    /// 更新网络接口列表
    /// </summary>
    private void UpdateNetworkInterfaces(object state)
    {
        try
        {
            var interfaces = BasicNetworkMonitor.GetConnectedNetworkInterfaces();

            _networkInterfaces.Clear();

            foreach (var networkInterface in interfaces)
            {
                _networkInterfaces.TryAdd(networkInterface.Id, networkInterface);
            }

            _lastNetworkInterfaceUpdate = DateTime.UtcNow;

            // Log.Information($"更新网络接口列表，共找到 {interfaces.Count} 个活跃接口");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新网络接口列表时发生错误");
        }
    }

    /// <summary>
    /// 获取或生成应用程序ID
    /// </summary>
    /// <returns>返回元组(HashAppId, OriginalAppId)</returns>
    private async Task<(string appId, string originalAppId)> GetOrGenerateAppIdAsync(int processId, Infrastructure.Models.ProgramInfo programInfo)
    {
        // 先检查缓存
        if (_processOriginalAppIdCache.TryGetValue(processId, out var cachedOriginalAppId))
        {
            var cachedHashAppId = GenerateHashAppId(cachedOriginalAppId);
            return (cachedHashAppId, cachedOriginalAppId);
        }

        try
        {
            // 如果没有程序信息，尝试从系统获取
            if (programInfo == null)
            {
                programInfo = SysInfoUtils.InspectProcess(processId);
            }

            if (programInfo == null)
            {
                Log.Warning($"无法获取进程 {processId} 的信息");
                return (null, null);
            }

            // 生成原始AppId: 路径 + 文件签名发布者 + 进程名
            var path = programInfo.MainModulePath ?? "";
            var publisher = programInfo.CompanyName ?? "";
            var processName = programInfo.ProcessName ?? "Unknown";

            var originalAppId = $"{path}|{publisher}|{processName}";
            
            // 生成Hash版本的AppId（SHA256前16位）
            var hashAppId = GenerateHashAppId(originalAppId);

            // 缓存原始AppId
            _processOriginalAppIdCache.TryAdd(processId, originalAppId);

            Log.Debug($"生成AppId: {hashAppId} (原始: {originalAppId.Substring(0, Math.Min(50, originalAppId.Length))}...)");
            
            return (hashAppId, originalAppId);
        }
        catch (Exception ex)
        {
            Log.Warning($"生成进程 {processId} 的AppId时出错", ex);
            return (null, null);
        }
    }

    /// <summary>
    /// 获取收集统计信息
    /// </summary>
    public NetworkCollectionStats GetStats()
    {
        return new NetworkCollectionStats
        {
            ActiveNetworkInterfaces = _networkInterfaces.Count,
            LastNetworkInterfaceUpdate = _lastNetworkInterfaceUpdate,
            CachedProcessCount = _processOriginalAppIdCache.Count,
            ActiveConnectionsCount = _activeConnections.Count
        };
    }

    /// <summary>
    /// 触发数据清理和聚合任务
    /// </summary>
    private async void TriggerDataCleanupAndAggregation(object state)
    {
        try
        {
            Log.Information("Start 开始执行定时数据清理和聚合任务");

            // 先触发数据聚合
            Log.Information("触发数据聚合任务");
            var aggregationResult = await _writeManager.TriggerDataAggregationAsync();

            if (aggregationResult)
            {
                Log.Information("数据聚合任务完成，开始触发数据清理任务");

                // 聚合成功后进行数据清理
                var cleanupResult = await _writeManager.TriggerDataCleanupAsync();

                if (cleanupResult)
                {
                    Log.Information("数据清理任务完成");
                }
                else
                {
                    Log.Warning("数据清理任务执行失败");
                }
            }
            else
            {
                Log.Warning("数据聚合任务执行失败，跳过清理任务");
            }

            Log.Information("定时数据清理和聚合任务执行完成");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "定时数据清理和聚合任务执行时发生错误");
        }
    }

    /// <summary>
    /// 生成Hash版本的AppId
    /// </summary>
    /// <param name="originalAppId">原始AppId</param>
    /// <returns>Hash版本的AppId（SHA256前16位）</returns>
    private static string GenerateHashAppId(string originalAppId)
    {
        if (string.IsNullOrEmpty(originalAppId))
        {
            return "unknown";
        }

        try
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(originalAppId));
            
            // 取前8字节（16位十六进制字符）
            var hashHex = Convert.ToHexString(hashBytes[..8]).ToLowerInvariant();
            
            return hashHex;
        }
        catch
        {
            // 如果Hash生成失败，使用原始AppId的哈希码
            return Math.Abs(originalAppId.GetHashCode()).ToString("x8");
        }
    }

    public void Dispose()
    {
        Log.Information("正在关闭网络数据收集服务...");

        _globalNetworkTimer?.Dispose();
        _etwNetworkTimer?.Dispose();
        _networkInterfaceUpdateTimer?.Dispose();
        _batchFlushTimer?.Dispose();
        _dataCleanupTimer?.Dispose();

        // 强制刷新所有批处理数据
        try
        {
            _writeManager.FlushAllBatchesAsync().Wait(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            Log.Warning("关闭时刷新批处理数据失败", ex);
        }

        Log.Information("网络数据收集服务已关闭");
    }
}

/// <summary>
/// 应用网络数据（用于缓存）
/// </summary>
public class AppNetworkData
{
    public string AppId { get; set; }
    public long TotalUpload { get; set; }
    public long TotalDownload { get; set; }
    public DateTime LastUpdate { get; set; }
    public int ConnectionCount { get; set; }
}

/// <summary>
/// 网络收集统计信息
/// </summary>
public class NetworkCollectionStats
{
    public int ActiveNetworkInterfaces { get; set; }
    public DateTime LastNetworkInterfaceUpdate { get; set; }
    public int CachedProcessCount { get; set; }
    public int ActiveConnectionsCount { get; set; }
}
