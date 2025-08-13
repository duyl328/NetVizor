using Application;
using Common.Net.Models;

namespace Shell.Models;

/// <summary>
/// 特定应用详情订阅的请求体模型
/// </summary>
public class SubscriptionAppInfo : SubscriptionInfo
{
    /// <summary>
    /// 订阅的应用程序路径
    /// </summary>
    public string ApplicationPath { get; set; }
}
