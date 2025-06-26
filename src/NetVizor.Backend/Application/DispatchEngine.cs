using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Net.WebSocketConn;
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

    /// <summary>
    /// 软件信息分发
    /// </summary>
    public void ApplicationInfoDistribute()
    {
        var timer = new Timer(state =>
        {
            if (ApplicationInfoDispatch.Count == 0)
            {
                return;
            }

            // 获取信息
            var programInfos = GlobalNetworkMonitor.Instance.GetAllPrograms();
            var serialize = JsonSerializer.Serialize(programInfos);

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
                        Message = "数据发送",
                        Timestamp = AppInfoLastSendTime[keyValuePair.Key]
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

