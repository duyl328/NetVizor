using System.Diagnostics;
using Infrastructure.Models;

namespace Infrastructure.GlobalCaches;

/// <summary>
/// 进程信息缓存
/// </summary>
public sealed class ProcessCache
{
    private static readonly Lazy<ProcessCache> _instance = new Lazy<ProcessCache>(() => new ProcessCache());

    public static ProcessCache Instance => _instance.Value;

    public record ProcessCacheKey(int Pid, DateTime StartTime);
    
    public ProcessCacheKey GetProcessKey(int pid)
    {
        var process = Process.GetProcessById(pid);
        return new ProcessCacheKey(pid, process.StartTime);
    }
    
    // 私有构造函数，禁止外部创建实例
    private ProcessCache()
    {
        // 初始化缓存等操作
    }

    // 示例缓存字段
    private readonly Dictionary<ProcessCacheKey, ProgramInfo> _cache = new Dictionary<ProcessCacheKey, ProgramInfo>();

    // 设置缓存
    public void Set(ProcessCacheKey key, ProgramInfo value)
    {
        lock (_cache)
        {
            _cache[key] = value;
        }
    }

    // 获取缓存
    public ProgramInfo? Get(ProcessCacheKey key)
    {
        lock (_cache)
        {
            return _cache.GetValueOrDefault(key);
        }
    }

    // 移除缓存
    public void Remove(ProcessCacheKey key)
    {
        lock (_cache)
        {
            _cache.Remove(key);
        }
    }
}
