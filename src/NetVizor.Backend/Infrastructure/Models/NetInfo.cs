
namespace Infrastructure.Models;

/// <summary>
/// 进程信息【软件信息】
/// </summary>
public class ProgramInfo
{
    /// <summary>
    /// 进程名称
    /// </summary>
    public string ProcessName { get; set; } = "";

    /// <summary>
    ///  进程ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// 启动时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    ///  是否退出
    /// </summary>
    public bool HasExited { get; set; }

    /*
     ExitTime 和 ExitCode 这两个属性只有在进程已经退出后才能访问。对于正在运行的进程，
     访问这些属性会抛出 "Process must exit before requested information can be determined" 异常。 
     */
    
    /// <summary>
    /// 退出时间
    /// </summary>
    public DateTime? ExitTime { get; set; }

    /// <summary>
    ///  退出代码
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    /// 占用内存
    /// </summary>
    public long UseMemory { get; set; }

    /// <summary>
    ///  线程数
    /// </summary>
    public int ThreadCount { get; set; }

    /// <summary>
    /// 主模块路径
    /// </summary>
    public string? MainModulePath { get; set; }

    /// <summary>
    /// 启动文件名
    /// </summary>
    public string? MainModuleName { get; set; }

    /// <summary>
    /// 文件描述
    /// </summary>
    public string? FileDescription { get; set; }

    /// <summary>
    /// 产品名称
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// 公司名称
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 版权
    /// </summary>
    public string? LegalCopyright { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string IconBase64 { get; set; } = "";
}

public class ApplicationInfoModel
{
    public required string Id { get; set; }
    /// <summary>
    /// 进程名称
    /// </summary>
    public string ProcessName { get; set; } = "";

    /// <summary>
    ///  进程ID
    /// </summary>
    public List<int> ProcessIds { get; set; }

    /// <summary>
    /// 启动时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    ///  是否退出
    /// </summary>
    public bool HasExited { get; set; }

    /// <summary>
    /// 退出时间
    /// </summary>
    public DateTime? ExitTime { get; set; }

    /// <summary>
    ///  退出代码
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    /// 占用内存
    /// </summary>
    public long UseMemory { get; set; }

    /// <summary>
    ///  线程数
    /// </summary>
    public int ThreadCount { get; set; }

    /// <summary>
    /// 主模块路径
    /// </summary>
    public string? MainModulePath { get; set; }

    /// <summary>
    /// 启动文件名
    /// </summary>
    public string? MainModuleName { get; set; }

    /// <summary>
    /// 文件描述
    /// </summary>
    public string? FileDescription { get; set; }

    /// <summary>
    /// 产品名称
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// 公司名称
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 版权
    /// </summary>
    public string? LegalCopyright { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string IconBase64 { get; set; } = "";
}