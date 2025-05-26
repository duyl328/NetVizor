using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Timers;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Common.Logger;

/// <summary>
/// 通用日志记录器
/// </summary>
public class UniversalLogger : IDisposable
{
    private readonly Serilog.Core.Logger _logger;
    private readonly LoggerConfig _config;
    private readonly System.Timers.Timer _maintenanceTimer;
    private bool _disposed = false;

    public UniversalLogger(LoggerConfig config = null)
    {
        _config = config ?? new LoggerConfig();
        _logger = CreateLogger();

        // 启动维护定时器
        _maintenanceTimer =
            new System.Timers.Timer(TimeSpan.FromHours(_config.CompressCheckIntervalHours).TotalMilliseconds);
        _maintenanceTimer.Elapsed += async (s, e) => await PerformMaintenanceAsync();
        _maintenanceTimer.Start();

        // 启动时执行一次维护
        Task.Run(async () => await PerformMaintenanceAsync());
    }

    /// <summary>
    /// 从配置文件创建日志记录器
    /// </summary>
    public static UniversalLogger FromConfiguration(IConfiguration configuration, string sectionName = "Logging")
    {
        var config = new LoggerConfig();
        configuration.GetSection(sectionName).Bind(config);
        return new UniversalLogger(config);
    }

    /// <summary>
    /// 从JSON配置文件创建日志记录器
    /// </summary>
    public static UniversalLogger FromJsonFile(string configPath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false)
            .Build();
        return FromConfiguration(configuration);
    }

    private Serilog.Core.Logger CreateLogger()
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(_config.MinimumLevel)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("ProcessId", Environment.ProcessId)
            .Enrich.WithProperty("ThreadId", Environment.CurrentManagedThreadId);

        // 配置控制台输出
        if (_config.EnableConsole)
        {
            loggerConfig = loggerConfig.WriteTo.Console(
                outputTemplate: _config.OutputTemplate,
                restrictedToMinimumLevel: _config.MinimumLevel);
        }

        // 配置文件输出
        if (_config.EnableFile)
        {
            Directory.CreateDirectory(_config.LogPath);

            string logFileName = Path.Combine(_config.LogPath, "log-.txt");

            if (_config.UseJsonFormat)
            {
                loggerConfig = loggerConfig.WriteTo.File(
                    formatter: new JsonFormatter(),
                    path: logFileName,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: _config.MaxFileSizeMB * 1024 * 1024,
                    retainedFileCountLimit: null,
                    restrictedToMinimumLevel: _config.MinimumLevel);
            }
            else
            {
                loggerConfig = loggerConfig.WriteTo.File(
                    path: logFileName,
                    outputTemplate: _config.OutputTemplate,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: _config.MaxFileSizeMB * 1024 * 1024,
                    retainedFileCountLimit: null,
                    restrictedToMinimumLevel: _config.MinimumLevel);
            }
        }

        return loggerConfig.CreateLogger();
    }

    #region 日志记录方法

    public void LogVerbose(string message, params object[] args)
    {
        _logger.Verbose(message, args);
    }

    public void LogDebug(string message, params object[] args)
    {
        _logger.Debug(message, args);
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.Information(message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.Warning(message, args);
    }

    public void LogError(string message, params object[] args)
    {
        _logger.Error(message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.Error(exception, message, args);
    }

    public void LogFatal(string message, params object[] args)
    {
        _logger.Fatal(message, args);
    }

    public void LogFatal(Exception exception, string message, params object[] args)
    {
        _logger.Fatal(exception, message, args);
    }

    /// <summary>
    /// 记录带上下文信息的日志
    /// </summary>
    public void LogWithContext(LogEventLevel level, string message, Dictionary<string, object> context,
        Exception exception = null)
    {
        var logEvent = new LogEventLevel[] { level };
        ILogger logger = _logger;

        foreach (var kvp in context ?? new Dictionary<string, object>())
        {
            var i = logger.ForContext(kvp.Key, kvp.Value);
            logger = logger.ForContext(kvp.Key, kvp.Value);
        }

        if (exception != null)
        {
            logger.Write(level, exception, message);
        }
        else
        {
            logger.Write(level, message);
        }
    }

    #endregion

    #region 维护功能

    /// <summary>
    /// 执行日志维护任务
    /// </summary>
    private async Task PerformMaintenanceAsync()
    {
        try
        {
            if (_config.EnableFile)
            {
                await CompressOldLogsAsync();
                await DeleteOldLogsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "执行日志维护任务时发生错误");
        }
    }

    /// <summary>
    /// 压缩旧日志文件
    /// </summary>
    private async Task CompressOldLogsAsync()
    {
        var logDirectory = new DirectoryInfo(_config.LogPath);
        if (!logDirectory.Exists) return;

        var cutoffDate = DateTime.Now.Date.AddDays(-_config.CompressDaysThreshold);
        var logFiles = logDirectory.GetFiles("log*.txt")
            .Where(f => f.CreationTime.Date < cutoffDate && !f.Name.EndsWith(".gz"))
            .ToList();

        foreach (var file in logFiles)
        {
            try
            {
                var compressedFileName = file.FullName + ".gz";
                if (File.Exists(compressedFileName)) continue;

                await CompressFileAsync(file.FullName, compressedFileName);
                File.Delete(file.FullName);

                _logger.Information("已压缩日志文件: {FileName}", file.Name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "压缩日志文件失败: {FileName}", file.Name);
            }
        }
    }

    /// <summary>
    /// 删除过期的日志文件
    /// </summary>
    private async Task DeleteOldLogsAsync()
    {
        var logDirectory = new DirectoryInfo(_config.LogPath);
        if (!logDirectory.Exists) return;

        var cutoffDate = DateTime.Now.Date.AddDays(-_config.RetentionDays);
        var oldFiles = logDirectory.GetFiles("log*.*")
            .Where(f => f.CreationTime.Date < cutoffDate)
            .ToList();

        foreach (var file in oldFiles)
        {
            try
            {
                File.Delete(file.FullName);
                _logger.Information("已删除过期日志文件: {FileName}", file.Name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "删除过期日志文件失败: {FileName}", file.Name);
            }
        }
    }

    /// <summary>
    /// 压缩文件
    /// </summary>
    private async Task CompressFileAsync(string sourceFile, string compressedFile)
    {
        using (var originalFileStream = File.OpenRead(sourceFile))
        using (var compressedFileStream = File.Create(compressedFile))
        using (var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
        {
            await originalFileStream.CopyToAsync(compressionStream);
        }
    }

    /// <summary>
    /// 手动执行维护任务
    /// </summary>
    public async Task RunMaintenanceAsync()
    {
        await PerformMaintenanceAsync();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _maintenanceTimer?.Stop();
                _maintenanceTimer?.Dispose();
                _logger?.Dispose();
            }

            _disposed = true;
        }
    }

    ~UniversalLogger()
    {
        Dispose(false);
    }

    #endregion
}

/// <summary>
/// 静态日志记录器(单例模式)
/// </summary>
public static class Log
{
    private static UniversalLogger _instance;
    private static readonly object _lock = new object();

    /// <summary>
    /// 初始化日志记录器
    /// </summary>
    public static void Initialize(LoggerConfig config = null)
    {
        lock (_lock)
        {
            _instance?.Dispose();
            _instance = new UniversalLogger(config);
        }
    }

    /// <summary>
    /// 从配置文件初始化
    /// </summary>
    public static void InitializeFromConfig(IConfiguration configuration, string sectionName = "Logging")
    {
        lock (_lock)
        {
            _instance?.Dispose();
            _instance = UniversalLogger.FromConfiguration(configuration, sectionName);
        }
    }

    /// <summary>
    /// 从JSON文件初始化
    /// </summary>
    public static void InitializeFromJsonFile(string configPath)
    {
        lock (_lock)
        {
            _instance?.Dispose();
            _instance = UniversalLogger.FromJsonFile(configPath);
        }
    }

    private static UniversalLogger Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new UniversalLogger();
                    }
                }
            }

            return _instance;
        }
    }

    public static void Verbose(string message, params object[] args) => Instance.LogVerbose(message, args);
    public static void Debug(string message, params object[] args) => Instance.LogDebug(message, args);
    public static void Information(string message, params object[] args) => Instance.LogInformation(message, args);
    public static void Warning(string message, params object[] args) => Instance.LogWarning(message, args);
    public static void Error(string message, params object[] args) => Instance.LogError(message, args);

    public static void Error(Exception exception, string message, params object[] args) =>
        Instance.LogError(exception, message, args);

    public static void Fatal(string message, params object[] args) => Instance.LogFatal(message, args);

    public static void Fatal(Exception exception, string message, params object[] args) =>
        Instance.LogFatal(exception, message, args);

    public static void WithContext(LogEventLevel level, string message, Dictionary<string, object> context,
        Exception exception = null)
        => Instance.LogWithContext(level, message, context, exception);

    public static async Task RunMaintenanceAsync() => await Instance.RunMaintenanceAsync();
}
