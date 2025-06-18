using System.Diagnostics;

namespace Common.Monitors;

public class ApplicationHealthMonitor
{
    private readonly Timer _healthCheckTimer;
    
    public ApplicationHealthMonitor()
    {
        _healthCheckTimer = new Timer(CheckApplicationHealth, null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
    }
    
    private void CheckApplicationHealth(object state)
    {
        try
        {
            // 检查内存使用
            var memoryUsage = GC.GetTotalMemory(false);
            if (memoryUsage > 500 * 1024 * 1024) // 500MB
            {
                // Logger.Warn($"内存使用过高: {memoryUsage / 1024 / 1024}MB");
                GC.Collect(); // 强制垃圾回收
            }
            
            // 检查线程数量
            var threadCount = Process.GetCurrentProcess().Threads.Count;
            if (threadCount > 100)
            {
                // Logger.Warn($"线程数量过多: {threadCount}");
            }
        }
        catch (Exception ex)
        {
            // LogException(ex, "健康检查异常");
        }
    }
}
