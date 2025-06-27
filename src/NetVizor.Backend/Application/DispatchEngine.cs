using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common;
using Common.Logger;
using Common.Net.WebSocketConn;
using Infrastructure.Models;
using Utils.ETW.Etw;

namespace Shell;

public class DispatchEngine
{
    private static readonly Lazy<DispatchEngine> _instance = new(() => new DispatchEngine());
    public static DispatchEngine Instance => _instance.Value;

    private DispatchEngine()
    {
    }
    // region 软件信息订阅

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

            var serialize = JsonSerializer.Serialize(appInfoList);

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
                        Message = "数据发送123",
                        Timestamp = AppInfoLastSendTime[keyValuePair.Key],
                        Type = AppConfig.ApplicationInfoSubscribe
                    };
                    WebSocketManager.Instance.SendToClient(keyValuePair.Key, rm);
                }
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    // endregion
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
/// 订阅信息内容
/// </summary>
public class SubscriptionInfo
{
    [JsonPropertyName("subscriptionType")] public string SubscriptionType { get; set; }
    [JsonPropertyName("interval")] public int Interval { get; set; }
}