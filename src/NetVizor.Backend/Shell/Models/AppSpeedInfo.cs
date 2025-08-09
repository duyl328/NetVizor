using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shell.Models;

/// <summary>
/// 应用程序网速信息
/// </summary>
public class AppSpeedInfo : INotifyPropertyChanged
{
    private int _processId;
    private string _appName;
    private string _uploadSpeed;
    private string _downloadSpeed;
    private double _uploadSpeedBytes;
    private double _downloadSpeedBytes;
    private string _iconPath;

    /// <summary>
    /// 进程ID
    /// </summary>
    public int ProcessId
    {
        get => _processId;
        set { _processId = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 应用程序名称
    /// </summary>
    public string AppName
    {
        get => _appName;
        set { _appName = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 上传速度文本
    /// </summary>
    public string UploadSpeed
    {
        get => _uploadSpeed;
        set { _uploadSpeed = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 下载速度文本
    /// </summary>
    public string DownloadSpeed
    {
        get => _downloadSpeed;
        set { _downloadSpeed = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 上传速度（字节/秒）
    /// </summary>
    public double UploadSpeedBytes
    {
        get => _uploadSpeedBytes;
        set { _uploadSpeedBytes = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 下载速度（字节/秒）
    /// </summary>
    public double DownloadSpeedBytes
    {
        get => _downloadSpeedBytes;
        set { _downloadSpeedBytes = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 总速度（字节/秒）
    /// </summary>
    public double TotalSpeedBytes => UploadSpeedBytes + DownloadSpeedBytes;

    /// <summary>
    /// 应用程序图标路径
    /// </summary>
    public string IconPath
    {
        get => _iconPath;
        set { _iconPath = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}