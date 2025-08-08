using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Utils.ETW.Models;

namespace Shell.ViewModel;

/// <summary>
/// 进程网络统计的ViewModel
/// 用于在UI中显示Top榜信息
/// </summary>
public class ProcessNetworkStatsViewModel : INotifyPropertyChanged
{
    private ProcessNetworkStats _stats;

    public ProcessNetworkStatsViewModel(ProcessNetworkStats stats)
    {
        _stats = stats;
        LoadIcon();
    }

    /// <summary>
    /// 进程ID
    /// </summary>
    public int ProcessId => _stats.ProcessId;

    /// <summary>
    /// 进程名称
    /// </summary>
    public string ProcessName => _stats.ProcessName;

    /// <summary>
    /// 格式化的上传速度
    /// </summary>
    public string FormattedUploadSpeed => _stats.FormattedUploadSpeed;

    /// <summary>
    /// 格式化的下载速度
    /// </summary>
    public string FormattedDownloadSpeed => _stats.FormattedDownloadSpeed;

    /// <summary>
    /// 格式化的总速度
    /// </summary>
    public string FormattedTotalSpeed => _stats.FormattedTotalSpeed;

    /// <summary>
    /// 总速度（用于排序）
    /// </summary>
    public double TotalSpeed => _stats.TotalSpeed;

    private BitmapImage? _iconSource;

    /// <summary>
    /// 图标源
    /// </summary>
    public BitmapImage? IconSource
    {
        get => _iconSource;
        private set
        {
            _iconSource = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 更新统计数据
    /// </summary>
    public void UpdateStats(ProcessNetworkStats newStats)
    {
        _stats = newStats;
        OnPropertyChanged(nameof(ProcessName));
        OnPropertyChanged(nameof(FormattedUploadSpeed));
        OnPropertyChanged(nameof(FormattedDownloadSpeed));
        OnPropertyChanged(nameof(FormattedTotalSpeed));
        OnPropertyChanged(nameof(TotalSpeed));

        // 如果图标还未加载，尝试重新加载
        if (_iconSource == null)
        {
            LoadIcon();
        }
    }

    /// <summary>
    /// 加载进程图标
    /// </summary>
    private void LoadIcon()
    {
        try
        {
            if (_stats.ApplicationInfo?.ProgramInfo?.IconBase64 != null)
            {
                var iconBytes = Convert.FromBase64String(_stats.ApplicationInfo.ProgramInfo.IconBase64);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new System.IO.MemoryStream(iconBytes);
                bitmap.DecodePixelWidth = 12; // 优化性能，直接解码为所需大小
                bitmap.DecodePixelHeight = 12;
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // 缓存以提高性能
                bitmap.EndInit();
                bitmap.Freeze(); // 冻结以允许跨线程访问

                IconSource = bitmap;
            }
            else
            {
                // 使用默认图标或留空
                IconSource = null;
            }
        }
        catch (Exception)
        {
            // 如果图标加载失败，使用null（UI会显示默认外观）
            IconSource = null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}