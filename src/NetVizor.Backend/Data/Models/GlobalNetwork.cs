namespace Data.Models;

public class GlobalNetwork
{
    /// <summary>
    /// 时间戳
    /// </summary>
    public long Timestep { get; set; }

    /// <summary>
    /// 上传量    
    /// </summary>
    /// <returns></returns>
    public long Upload { get; set; }

    /// <summary>
    /// 下载量
    /// </summary>
    public long Download { get; set; }

    /// <summary>
    /// 网卡 Guid
    /// </summary>
    public string NetworkCardGuid { get; set; }
}