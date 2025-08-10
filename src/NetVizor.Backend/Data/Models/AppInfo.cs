namespace Data.Models;

// 
/// <summary>
/// 软件信息
/// </summary>
public class AppInfo
{
    public int Id { get; set; }

    /// <summary>
    /// 软件 ID（Hash版本，用于关联和索引）
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// 原始软件 ID = 路径 + 文件签名发布者 + 进程名 （文件签名发布者 可无）
    /// </summary>
    public string OriginalAppId { get; set; }

    /// <summary>
    /// 名字
    /// </summary>
    public string Name { get; set; }

    public string Path { get; set; }
    public string Version { get; set; }
    public string Company { get; set; }
    public string Base64Icon { get; set; }
    public string Hash { get; set; }

    /// <summary>
    /// 首次插入时间
    /// </summary>
    public long InsertTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public long UpdateTime { get; set; }

    /// <summary>
    /// 删除时间 （-1 或 0 是未删除）
    /// </summary>
    public long DeleteTime { get; set; }
}