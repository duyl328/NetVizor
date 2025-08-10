using System.Text;
using Common.Utils;
using Shell.Models;
using Utils.ETW.Etw;
using Infrastructure.Models;

namespace Shell.Services;

/// <summary>
/// 应用网速信息服务
/// </summary>
public class AppSpeedInfoService
{
    private static readonly Lazy<AppSpeedInfoService> _instance = new(() => new AppSpeedInfoService());
    public static AppSpeedInfoService Instance => _instance.Value;

    /// <summary>
    /// 获取网速占用最高的应用程序列表
    /// </summary>
    /// <param name="count">返回的应用程序数量 (1-5)</param>
    /// <returns>按总网速降序排列的应用程序列表</returns>
    public List<AppSpeedInfo> GetTopSpeedApps(int count = 3)
    {
        count = Math.Max(1, Math.Min(5, count));
        
        try
        {
            // 从GlobalNetworkMonitor获取快照
            var snapshot = GlobalNetworkMonitor.Instance.GetSnapshot();
            
            if (snapshot?.Applications == null || !snapshot.Applications.Any())
            {
                return new List<AppSpeedInfo>();
            }

            // 计算每个应用的网速，并按总速度排序
            var topApps = snapshot.Applications
                .Where(app => app.TotalSendSpeed > 0 || app.TotalReceiveSpeed > 0) // 只包含有网络活动的应用
                .OrderByDescending(app => app.TotalSendSpeed + app.TotalReceiveSpeed) // 按总速度排序
                .Take(count)
                .Select(app => new AppSpeedInfo
                {
                    ProcessId = app.ProcessId,
                    AppName = GetDisplayAppName(app),
                    UploadSpeedBytes = app.TotalSendSpeed,
                    DownloadSpeedBytes = app.TotalReceiveSpeed,
                    UploadSpeed = FormatSpeed(app.TotalSendSpeed),
                    DownloadSpeed = FormatSpeed(app.TotalReceiveSpeed),
                    IconPath = GetAppIconPath(app)
                })
                .ToList();

            return topApps;
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出异常，返回空列表
            Common.Logger.Log.Error4Ctx($"获取Top应用网速时出错: {ex.Message}");
            return new List<AppSpeedInfo>();
        }
    }

    /// <summary>
    /// 获取应用程序的显示名称
    /// </summary>
    private string GetDisplayAppName(ApplicationSnapshot app)
    {
        // 优先使用程序信息中的产品名称
        if (app.ProgramInfo?.ProductName != null && !string.IsNullOrEmpty(app.ProgramInfo.ProductName))
        {
            return app.ProgramInfo.ProductName;
        }

        // 如果没有产品名称，使用文件描述
        if (app.ProgramInfo?.FileDescription != null && !string.IsNullOrEmpty(app.ProgramInfo.FileDescription))
        {
            return app.ProgramInfo.FileDescription;
        }

        // 如果没有程序信息，尝试从进程ID获取进程名
        try
        {
            var process = System.Diagnostics.Process.GetProcessById(app.ProcessId);
            return process.ProcessName;
        }
        catch
        {
            return $"Process {app.ProcessId}";
        }
    }

    /// <summary>
    /// 获取应用程序图标路径
    /// </summary>
    private string GetAppIconPath(ApplicationSnapshot app)
    {
        // 尝试从程序信息中获取图标Base64数据
        if (app.ProgramInfo?.IconBase64 != null && !string.IsNullOrEmpty(app.ProgramInfo.IconBase64))
        {
            return app.ProgramInfo.IconBase64;
        }

        // 如果没有图标信息，返回null
        return null;
    }

    /// <summary>
    /// 格式化网速显示文本
    /// </summary>
    /// <param name="bytesPerSecond">字节/秒</param>
    /// <returns>格式化的速度字符串</returns>
    private string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond <= 0)
            return "0 B/s";

        // 根据NetViewSettings中的单位设置来格式化
        var settings = NetViewSettings.Instance;
        
        return settings.SpeedUnit switch
        {
            SpeedUnit.Bytes => $"{bytesPerSecond:F2} B/s",
            SpeedUnit.KB => $"{bytesPerSecond / 1024:F2} KB/s",
            SpeedUnit.MB => $"{bytesPerSecond / 1024 / 1024:F2} MB/s",
            SpeedUnit.Auto => FormatSpeedAuto(bytesPerSecond),
            _ => FormatSpeedAuto(bytesPerSecond)
        };
    }

    /// <summary>
    /// 自动选择最适合的单位格式化速度
    /// </summary>
    private string FormatSpeedAuto(double bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
            return $"{bytesPerSecond:F0} B/s";
        
        if (bytesPerSecond < 1024 * 1024)
            return $"{bytesPerSecond / 1024:F2} KB/s";
        
        return $"{bytesPerSecond / 1024 / 1024:F2} MB/s";
    }

    /// <summary>
    /// 获取格式化的速度信息摘要（用于调试）
    /// </summary>
    public string GetSpeedSummary(int count = 3)
    {
        var topApps = GetTopSpeedApps(count);
        
        if (!topApps.Any())
        {
            return "当前没有检测到网络活动的应用程序";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Top {topApps.Count} 网速占用应用:");
        
        for (int i = 0; i < topApps.Count; i++)
        {
            var app = topApps[i];
            sb.AppendLine($"{i + 1}. {app.AppName}");
            sb.AppendLine($"   ↑ {app.UploadSpeed}  ↓ {app.DownloadSpeed}");
        }

        return sb.ToString();
    }
}