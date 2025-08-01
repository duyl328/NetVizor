namespace Data.Models;

// 
/// <summary>
/// 软件信息
/// </summary>
public class AppInfo
{
    public int Id { get; set; }

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