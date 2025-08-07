namespace Data.Models;

/// <summary>
/// 全局网络聚合数据基类
/// </summary>
public abstract class GlobalNetworkAggregatedBase
{
    public int Id { get; set; }

    /// <summary>
    /// 网卡 GUID
    /// </summary>
    public string NetworkCardGuid { get; set; }

    /// <summary>
    /// 总上传字节数
    /// </summary>
    public long TotalUpload { get; set; }

    /// <summary>
    /// 总下载字节数
    /// </summary>
    public long TotalDownload { get; set; }

    /// <summary>
    /// 平均上传速度
    /// </summary>
    public long AvgUpload { get; set; }

    /// <summary>
    /// 平均下载速度
    /// </summary>
    public long AvgDownload { get; set; }

    /// <summary>
    /// 最大上传速度
    /// </summary>
    public long MaxUpload { get; set; }

    /// <summary>
    /// 最大下载速度
    /// </summary>
    public long MaxDownload { get; set; }

    /// <summary>
    /// 记录数量（用于计算平均值）
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// 创建时间戳
    /// </summary>
    public long CreatedTimestamp { get; set; }
}

/// <summary>
/// 全局网络小时聚合数据
/// </summary>
public class GlobalNetworkHourly : GlobalNetworkAggregatedBase
{
    /// <summary>
    /// 小时时间戳（Unix时间戳，精确到小时）
    /// </summary>
    public long HourTimestamp { get; set; }
}

/// <summary>
/// 全局网络天聚合数据
/// </summary>
public class GlobalNetworkDaily : GlobalNetworkAggregatedBase
{
    /// <summary>
    /// 天时间戳（Unix时间戳，精确到天）
    /// </summary>
    public long DayTimestamp { get; set; }
}

/// <summary>
/// 全局网络周聚合数据
/// </summary>
public class GlobalNetworkWeekly : GlobalNetworkAggregatedBase
{
    /// <summary>
    /// 周时间戳（Unix时间戳，精确到周）
    /// </summary>
    public long WeekTimestamp { get; set; }
}

/// <summary>
/// 全局网络月聚合数据
/// </summary>
public class GlobalNetworkMonthly : GlobalNetworkAggregatedBase
{
    /// <summary>
    /// 月时间戳（Unix时间戳，精确到月）
    /// </summary>
    public long MonthTimestamp { get; set; }
}