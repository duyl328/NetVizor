using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
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
            OnPropertyChanged(nameof(HasIcon));
        }
    }

    /// <summary>
    /// 是否有图标
    /// </summary>
    public bool HasIcon => _iconSource != null;

    /// <summary>
    /// 首字母
    /// </summary>
    public string InitialLetter => GetInitialLetter();

    /// <summary>
    /// 首字母背景色
    /// </summary>
    public System.Windows.Media.Brush InitialBackgroundColor => GetInitialBackgroundColor();

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

        // 通知首字母相关属性更新
        OnPropertyChanged(nameof(InitialLetter));
        OnPropertyChanged(nameof(InitialBackgroundColor));

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

    /// <summary>
    /// 获取进程名首字母
    /// </summary>
    private string GetInitialLetter()
    {
        if (string.IsNullOrEmpty(ProcessName))
            return "?";

        var name = ProcessName.Trim();
        if (name.Length == 0)
            return "?";

        // 获取第一个字符，转为大写
        var firstChar = name[0];
        if (char.IsLetter(firstChar))
        {
            return char.ToUpper(firstChar).ToString();
        }
        else if (char.IsDigit(firstChar))
        {
            return firstChar.ToString();
        }
        else
        {
            return "?";
        }
    }

    /// <summary>
    /// 根据进程名生成背景色
    /// </summary>
    private System.Windows.Media.Brush GetInitialBackgroundColor()
    {
        var colors = new[]
        {
            "#FF5722", "#E91E63", "#9C27B0", "#673AB7", "#3F51B5",
            "#2196F3", "#03A9F4", "#00BCD4", "#009688", "#4CAF50",
            "#8BC34A", "#CDDC39", "#FFC107", "#FF9800", "#FF5722"
        };

        if (string.IsNullOrEmpty(ProcessName))
        {
            return new SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9E9E9E"));
        }

        // 使用进程名的哈希值来选择颜色，确保同名进程使用相同颜色
        var hash = ProcessName.GetHashCode();
        var index = Math.Abs(hash) % colors.Length;
        return new SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colors[index]));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}