using System.Diagnostics;
using Infrastructure.Models;

namespace Infrastructure.GlobalCaches;

/// <summary>
/// 进程信息缓存
/// </summary>
public sealed class ProcessCache
{
    private static readonly Lazy<ProcessCache> _instance = new(() => new ProcessCache());

    public static ProcessCache Instance => _instance.Value;

    public static string GetProcessKey(int pid)
    {
        var process = Process.GetProcessById(pid);
        return $"{pid}-{process.StartTime:yyyyMMddHHmmss}";
    }
    
    // 私有构造函数，禁止外部创建实例
    private ProcessCache()
    {
        // 初始化缓存等操作
    }

    /// <summary>
    /// 缓存字典【给定初始量 1000 ，后期可根据需求调整，避免频繁扩容】
    /// </summary>
    private readonly Dictionary<string, ProgramInfo> _cache = new(1000);

    // 设置缓存
    public void Set(string key, ProgramInfo value)
    {
        lock (_cache)
        {
            _cache[key] = value;
        }
    }

    // 获取缓存
    public ProgramInfo? Get(string key)
    {
        lock (_cache)
        {
            return _cache.GetValueOrDefault(key);
        }
    }

    // 移除缓存
    public void Remove(string key)
    {
        lock (_cache)
        {
            _cache.Remove(key);
        }
    }
}
/*
 PID 重用问题：
 
🧠 背景 recap：
操作系统（如 Windows）中的每个连接通常可以获取一个 PID。
但由于：

进程退出后，PID 会被操作系统复用（可能很快）

某些连接（如 TIME_WAIT、CLOSE_WAIT）在进程退出后仍然保留一段时间
因此：不能仅仅根据 PID 匹配连接与进程。

✅ 解决方法：引入 进程启动时间 + 连接时间戳 双重判断
✅ 核心原则：
一个连接只能属于：连接开始时间 ≥ 进程启动时间 的进程

❗反面例子：
如果连接时间是 18:00
但 PID 对应的进程启动时间是 18:10
→ 说明这个进程是后来启动的（PID 被重用了）
→ ❌ 不应该关联这条连接
 
 */
