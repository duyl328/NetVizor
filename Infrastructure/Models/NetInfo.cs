namespace Infrastructure.Models;

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

    /// <summary>
    /// 退出时间
    /// </summary>
    public DateTime ExitTime { get; set; }

    /// <summary>
    ///  退出代码
    /// </summary>
    public int ExitCode { get; set; }

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

/// <summary>
/// 网络信息
/// </summary>
public class NetInfo
{
    /// <summary>
    /// 进程 ID
    /// </summary>
    public int Pid { get; set; }

    /// <summary>
    /// 线程 ID
    /// </summary>
    public int Tid { get; set; }

    /// <summary>
    /// 数据方向 "Send" / "Recv"
    /// </summary>
    public string Direction { get; set; }

    /// <summary>
    /// 初次一次活动事件
    /// </summary>
    public DateTime FirstSeen { get; set; }

    /// <summary>
    /// 最近一次活动事件
    /// </summary>
    public DateTime LastSeen { get; set; }

    /// <summary>
    /// 连接状态
    /// </summary>
    public int State { get; set; }

    /// <summary>
    /// 源 IP
    /// </summary>
    public string SourceIp { get; set; }

    /// <summary>
    /// 源端口
    ///</summary>
    public int SourcePort { get; set; }

    /// <summary>
    /// 目标 IP
    ///</summary>
    public string DestIp { get; set; }

    /// <summary>
    /// 目标端口
    ///</summary>
    public int DestPort { get; set; }

    /// <summary>
    /// 协议 TCP / UDP
    ///</summary>
    public string Protocol { get; set; }

    /// <summary>
    /// 发送字节数
    /// </summary>
    public long TotalSendBytes { get; set; }

    /// <summary>
    /// 接收字节数
    /// </summary>
    public long TotalReceiveBytes { get; set; }
}
