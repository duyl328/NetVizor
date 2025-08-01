namespace Data.Models;

/// <summary>
/// 应用程序网络活动记录
/// </summary>
public class AppNetwork
{
    public int Id { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// 本地IP地址
    /// </summary>
    public string LocalIP { get; set; } = string.Empty;

    /// <summary>
    /// 本地端口
    /// </summary>
    public int LocalPort { get; set; }

    /// <summary>
    /// 远程IP地址
    /// </summary>
    public string RemoteIP { get; set; } = string.Empty;

    /// <summary>
    /// 远程端口
    /// </summary>
    public int RemotePort { get; set; }

    /// <summary>
    /// 协议类型（TCP/UDP）
    /// </summary>
    public string Protocol { get; set; } = string.Empty;

    /// <summary>
    /// 上传字节数
    /// </summary>
    public long UploadBytes { get; set; }

    /// <summary>
    /// 下载字节数
    /// </summary>
    public long DownloadBytes { get; set; }

    /// <summary>
    /// 软件 ID = 路径 + 文件签名发布者 + 进程名 （文件签名发布者 可无）
    /// </summary>
    public string AppId { get; set; }
}