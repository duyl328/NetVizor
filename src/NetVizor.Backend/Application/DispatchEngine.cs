using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common;
using Common.Logger;
using Common.Net.WebSocketConn;
using Common.Utils;
using Infrastructure.Models;
using Infrastructure.utils;
using Utils.ETW.Etw;

namespace Shell;

public class DispatchEngine
{
    private static readonly Lazy<DispatchEngine> _instance = new(() => new DispatchEngine());
    public static DispatchEngine Instance => _instance.Value;

    private DispatchEngine()
    {
    }

    #region 软件信息订阅

    /// <summary>
    /// 软件信息订阅
    /// </summary>
    private ConcurrentDictionary<string, DispatchModel> ApplicationInfoDispatch = [];

    /// <summary>
    /// 软件信息上次发送时间
    /// </summary>
    private ConcurrentDictionary<string, DateTime> AppInfoLastSendTime = [];

    /// <summary>
    /// 添加订阅
    /// </summary>
    public void AddApplicationInfo(string clientId, DispatchModel model)
    {
        ApplicationInfoDispatch.TryAdd(clientId, model);
    }

    /// <summary>
    /// 更新订阅
    /// </summary>
    public void UpdateApplicationInfo(string clientId, DispatchModel model)
    {
        ApplicationInfoDispatch[clientId] = model;
    }

    /// <summary>
    /// 删除订阅
    /// </summary>
    public void DeleteApplicationInfo(string clientId)
    {
        ApplicationInfoDispatch.TryRemove(clientId, out _);
    }

    // 添加这个字段来保持Timer的强引用；Timer 对象没有强引用，会被垃圾回收器回收。
    private Timer _applicationInfoTimer;

    /// <summary>
    /// 软件信息分发
    /// </summary>
    public void ApplicationInfoDistribute()
    {
        _applicationInfoTimer = new Timer(state =>
        {
            if (ApplicationInfoDispatch.Count == 0)
            {
                return;
            }

            // 获取信息
            var programInfos = GlobalNetworkMonitor.Instance.GetAllPrograms();
            var grouped = programInfos
                .GroupBy(p => p.MainModulePath ?? p.ProcessName)
                .ToDictionary(g => g.Key, g => g.ToList());
            var list = grouped.Values.ToList();

            var appInfoList = new List<ApplicationInfoModel>();
            foreach (var infose in list)
            {
                if (infose.Count == 0)
                {
                    continue;
                }

                ApplicationInfoModel pi = new ApplicationInfoModel
                {
                    Id = "null"
                };
                var pids = new List<int>();
                var startTime = DateTime.Now;
                var isExit = true;
                var useMemory = 0L;
                var threadCount = 0;
                var id = "";
                foreach (var programInfo in infose)
                {
                    pi.ProcessName ??= programInfo.ProcessName;
                    pi.ExitTime ??= programInfo.ExitTime;
                    pi.ExitCode ??= programInfo.ExitCode;
                    pi.MainModulePath ??= programInfo.MainModulePath;
                    pi.MainModuleName ??= programInfo.MainModuleName;
                    pi.FileDescription ??= programInfo.FileDescription;
                    pi.ProductName ??= programInfo.ProductName;
                    pi.CompanyName ??= programInfo.CompanyName;
                    pi.Version ??= programInfo.Version;
                    pi.LegalCopyright ??= programInfo.LegalCopyright;
                    pi.IconBase64 = programInfo.IconBase64;
                    id += programInfo.ProcessId;
                    pids.Add(programInfo.ProcessId);
                    // 只记录最早的时间
                    if (programInfo.StartTime < startTime)
                    {
                        startTime = programInfo.StartTime;
                    }

                    // 全部退出才算退出
                    if (!programInfo.HasExited && isExit)
                    {
                        isExit = programInfo.HasExited;
                    }

                    useMemory += programInfo.UseMemory;
                    threadCount += programInfo.ThreadCount;
                }

                pi.Id = id;
                pi.ProcessIds = pids;
                pi.StartTime = startTime;
                pi.HasExited = isExit;
                pi.UseMemory = useMemory;
                pi.ThreadCount = threadCount;

                appInfoList.Add(pi);
            }

            var serialize = JsonHelper.ToJson(appInfoList);

            foreach (var keyValuePair in ApplicationInfoDispatch)
            {
                // 间隔时间
                var interval = keyValuePair.Value.Interval;
                // 上一次是否发送
                var isHasLast = AppInfoLastSendTime.TryGetValue(keyValuePair.Key, out DateTime lastSendTime);

                // 本次是否应该发送
                var isShouldSend = false;
                // 具有上一次，检查时间
                if (isHasLast)
                {
                    var intervalTime = (DateTime.Now - lastSendTime).TotalMilliseconds;

                    isShouldSend = intervalTime >= interval;
                }
                // 没有上一次，直接发送
                else
                {
                    isShouldSend = true;
                }

                // 是否应该发送
                if (isShouldSend)
                {
                    AppInfoLastSendTime[keyValuePair.Key] = DateTime.Now;
                    var rm = new ResponseMessage
                    {
                        Success = true,
                        Data = serialize,
                        Message = "程序信息发送",
                        Timestamp = AppInfoLastSendTime[keyValuePair.Key],
                        Type = AppConfig.ApplicationInfoSubscribe
                    };
                    WebSocketManager.Instance.SendToClient(keyValuePair.Key, rm);
                }
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region 线程信息订阅

    /// <summary>
    /// 线程信息订阅
    /// </summary>
    private ConcurrentDictionary<string, ProcessDispatchModel> ProcessInfoDispatch = [];

    /// <summary>
    /// 软件信息上次发送时间
    /// </summary>
    private ConcurrentDictionary<string, DateTime> ProcessInfoLastSendTime = [];

    /// <summary>
    /// 添加订阅
    /// </summary>
    public void AddProcessInfo(string clientId, ProcessDispatchModel model)
    {
        ProcessInfoDispatch.TryAdd(clientId, model);
    }

    /// <summary>
    /// 更新订阅
    /// </summary>
    public void UpdateProcessInfo(string clientId, ProcessDispatchModel model)
    {
        ProcessInfoDispatch[clientId] = model;
    }

    /// <summary>
    /// 删除订阅
    /// </summary>
    public void DeleteProcessInfo(string clientId)
    {
        ProcessInfoDispatch.TryRemove(clientId, out _);
    }

    // 添加这个字段来保持Timer的强引用；Timer 对象没有强引用，会被垃圾回收器回收。
    private Timer _processInfoTimer;

    /// <summary>
    /// 需要监视的线程列表
    /// </summary>
    public List<int> MonitorProcessList = new List<int>();

    /// <summary>
    /// 进行信息分发
    /// </summary>
    public void ProcessInfoDistribute()
    {
        _processInfoTimer = new Timer(state =>
        {
            if (ProcessInfoDispatch.Count == 0)
            {
                return;
            }

            /*
                用户 A 订阅了 1、2、3 进程
                用户 B 订阅了 5、6、7 进程
                不同的的订阅的内容将进行不同的分发
             */
            // 用户订阅的内容
            foreach (var processDispatchModel in ProcessInfoDispatch)
            {
                // 检查是否发送，如果不发送，则跳过数据获取
                // 间隔时间
                var interval = processDispatchModel.Value.Interval;
                // 上一次是否发送
                var isHasLast = AppInfoLastSendTime.TryGetValue(processDispatchModel.Key, out DateTime lastSendTime);

                // 本次是否应该发送
                var isShouldSend = false;
                // 具有上一次，检查时间
                if (isHasLast)
                {
                    var intervalTime = (DateTime.Now - lastSendTime).TotalMilliseconds;

                    isShouldSend = intervalTime >= interval;
                }
                // 没有上一次，直接发送
                else
                {
                    isShouldSend = true;
                }

                if (!isShouldSend)
                {
                    continue;
                }
                
                // 获取用户的订阅信息
                var dispatchModel = processDispatchModel.Value;

                if (dispatchModel.ProcessIds == null || dispatchModel.ProcessIds.Count == 0)
                {
                    continue;
                }
                
                List<ProcessType> pts = new List<ProcessType>();
                foreach (var processId in dispatchModel.ProcessIds)
                {
                    var connection = GlobalNetworkMonitor.Instance.GetConnection(processId);
                    var inspectProcess = SysInfoUtils.InspectProcess(processId);

                    if (inspectProcess == null)
                    {
                        continue;
                    }
                    
                    ProcessType pt = new ProcessType();
                    // 上行总数据
                    long allBytesSent = 0;
                    // 下行总数据
                    long allBytesReceived = 0;
                    // 上行网速总数据
                    double allSendSpeed = 0;
                    // 下行网速总数据
                    double allReceiveSpeed = 0;
                    // 软件启动时间
                    var startTime = DateTime.Now;
                    // 统计所有连接的信息
                    foreach (var connectionInfo in connection)
                    {
                        // 数据发送量
                        allBytesSent += connectionInfo.BytesSent;
                        allBytesReceived += connectionInfo.BytesReceived;

                        // 网速数据
                        allSendSpeed += connectionInfo.CurrentSendSpeed;
                        allReceiveSpeed += connectionInfo.CurrentReceiveSpeed;
                    }

                    pt.ProcessName = inspectProcess.ProcessName;
                    pt.ProcessId = inspectProcess.ProcessId;
                    pt.StartTime = inspectProcess.StartTime;
                    pt.HasExited = inspectProcess.HasExited;
                    pt.ExitTime = inspectProcess.ExitTime;
                    pt.ExitCode = inspectProcess.ExitCode;
                    pt.UseMemory = inspectProcess.UseMemory;
                    pt.ThreadCount = inspectProcess.ThreadCount;
                    pt.MainModulePath = inspectProcess.MainModulePath;
                    pt.MainModuleName = inspectProcess.MainModuleName;
                    pt.TotalUploaded = allBytesSent;
                    pt.TotalDownloaded = allBytesReceived;
                    pt.UploadSpeed = allSendSpeed;
                    pt.DownloadSpeed = allReceiveSpeed;
                    pt.Connections = connection;
                    
                    pts.Add(pt);
                }
                
                // 数据整理完毕，开始分发内容
                var json = JsonHelper.ToJson(pts);
                AppInfoLastSendTime[processDispatchModel.Key] = DateTime.Now;
                var rm = new ResponseMessage
                {
                    Success = true,
                    Data = json,
                    Message = "进程信息发送",
                    Timestamp = AppInfoLastSendTime[processDispatchModel.Key],
                    Type = AppConfig.ProcessInfoSubscribe
                };
                WebSocketManager.Instance.SendToClient(processDispatchModel.Key, rm);

            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    #endregion
}

/// <summary>
/// 调度参数模型
/// </summary>
public class DispatchModel
{
    /// <summary>
    /// 信息间隔
    /// </summary>
    public int Interval { get; set; }
}

/// <summary>
/// 进程订阅模型
/// </summary>
public class ProcessDispatchModel : DispatchModel
{
    // 订阅的进程列表
    public List<int>? ProcessIds { get; set; }
}

/// <summary>
/// 订阅信息内容
/// </summary>
public class SubscriptionInfo
{
    public string SubscriptionType { get; set; }
    public int Interval { get; set; }
}

/// <summary>
/// 进程订阅信息
/// </summary>
public class SubscriptionProcessInfo : SubscriptionInfo
{
    // 订阅的进程列表
    public List<int> ProcessIds { get; set; }
}