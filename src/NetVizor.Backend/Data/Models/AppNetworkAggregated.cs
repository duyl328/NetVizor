namespace Data.Models;

/// <summary>
/// 应用网络聚合数据基类
/// </summary>
public abstract class AppNetworkAggregatedBase
{
    public int Id { get; set; }

    /// <summary>
    /// 软件 ID
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// 总上传字节数
    /// </summary>
    public long TotalUploadBytes { get; set; }

    /// <summary>
    /// 总下载字节数
    /// </summary>
    public long TotalDownloadBytes { get; set; }

    /// <summary>
    /// 连接数量
    /// </summary>
    public int ConnectionCount { get; set; }

    /// <summary>
    /// 唯一远程IP数量
    /// </summary>
    public int UniqueRemoteIPs { get; set; }

    /// <summary>
    /// 唯一远程端口数量
    /// </summary>
    public int UniqueRemotePorts { get; set; }

    /// <summary>
    /// 创建时间戳
    /// </summary>
    public long CreatedTimestamp { get; set; }
}

/// <summary>
/// 应用网络小时聚合数据
/// </summary>
public class AppNetworkHourly : AppNetworkAggregatedBase
{
    /// <summary>
    /// 小时时间戳（Unix时间戳，精确到小时）
    /// </summary>
    public long HourTimestamp { get; set; }
}

/// <summary>
/// 应用网络天聚合数据
/// </summary>
public class AppNetworkDaily : AppNetworkAggregatedBase
{
    /// <summary>
    /// 天时间戳（Unix时间戳，精确到天）
    /// </summary>
    public long DayTimestamp { get; set; }
}

/// <summary>
/// 应用网络周聚合数据
/// </summary>
public class AppNetworkWeekly : AppNetworkAggregatedBase
{
    /// <summary>
    /// 周时间戳（Unix时间戳，精确到周）
    /// </summary>
    public long WeekTimestamp { get; set; }
}

/// <summary>
/// 应用网络月聚合数据
/// </summary>
public class AppNetworkMonthly : AppNetworkAggregatedBase
{
    /// <summary>
    /// 月时间戳（Unix时间戳，精确到月）
    /// </summary>
    public long MonthTimestamp { get; set; }
}