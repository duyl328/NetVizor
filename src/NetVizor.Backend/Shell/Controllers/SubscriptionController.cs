using System;
using System.Text.Json;
using System.Threading.Tasks;
using Application;
using Application.Utils;
using Common;
using Common.Logger;
using Common.Net.HttpConn;
using Common.Net.Models;
using Common.Utils;
using Shell.Models;

namespace Shell.Controllers;

public class SubscriptionController : BaseController
{
    public async Task SubscribeApplicationAsync(HttpContext context)
    {
        if (string.IsNullOrEmpty(context.RequestBody))
        {
            await WriteErrorResponseAsync(context, "请求体不能为空");
            return;
        }

        try
        {
            string? uuid = GetHeaderValue(context, "uuid");
            Log.Warning($"接收到的数据: {context.RequestBody}, 用户 Id: {uuid}");

            if (string.IsNullOrWhiteSpace(uuid))
            {
                await WriteErrorResponseAsync(context, "用户 ID 丢失!");
                return;
            }

            var subscriptionInfo = ParseRequestBody<SubscriptionInfo>(context.RequestBody);
            if (subscriptionInfo != null)
            {
                DispatchEngine.Instance.AddApplicationInfo(uuid, new DispatchModel
                {
                    Interval = subscriptionInfo.Interval
                });

                Log.Warning($"时间: {subscriptionInfo.Interval}");
            }
        }
        catch (JsonException ex)
        {
            await WriteErrorResponseAsync(context, $"请求数据格式错误: {ex.Message}");
            return;
        }

        await WriteJsonResponseAsync(context, new ResponseModel<string>
        {
            Success = true,
            Data = "成功",
            Message = "订阅成功"
        });
    }

    public async Task SubscribeProcessAsync(HttpContext context)
    {
        if (string.IsNullOrEmpty(context.RequestBody))
        {
            await WriteErrorResponseAsync(context, "请求体不能为空");
            return;
        }

        try
        {
            string? uuid = GetHeaderValue(context, "uuid");

            if (string.IsNullOrWhiteSpace(uuid))
            {
                await WriteErrorResponseAsync(context, "用户 ID 丢失!");
                return;
            }

            var subscriptionInfo = ParseRequestBody<SubscriptionProcessInfo>(context.RequestBody);
            if (subscriptionInfo != null)
            {
                DispatchEngine.Instance.DeleteProcessInfo(uuid);
                DispatchEngine.Instance.AddProcessInfo(uuid, new ProcessDispatchModel
                {
                    Interval = subscriptionInfo.Interval,
                    ProcessIds = subscriptionInfo.ProcessIds
                });
                Log.Warning($"时间: {subscriptionInfo.Interval}");
                Log.Warning($"监视的 ID : {subscriptionInfo.ProcessIds}");
            }
        }
        catch (JsonException ex)
        {
            await WriteErrorResponseAsync(context, $"请求数据格式错误: {ex.Message}");
            return;
        }

        await WriteJsonResponseAsync(context, new ResponseModel<string>
        {
            Success = true,
            Data = "成功",
            Message = "订阅成功"
        });
    }

    public async Task SubscribeAppInfoAsync(HttpContext context)
    {
        if (string.IsNullOrEmpty(context.RequestBody))
        {
            await WriteErrorResponseAsync(context, "请求体不能为空");
            return;
        }

        try
        {
            string? uuid = GetHeaderValue(context, "uuid");
            if (string.IsNullOrWhiteSpace(uuid))
            {
                await WriteErrorResponseAsync(context, "用户 ID 丢失!");
                return;
            }

            var subscriptionRequest = ParseRequestBody<SubscriptionAppInfo>(context.RequestBody);
            if (subscriptionRequest != null && !string.IsNullOrWhiteSpace(subscriptionRequest.ApplicationPath))
            {
                DispatchEngine.Instance.AddAppDetailInfo(uuid, new AppDetailDispatchModel
                {
                    Interval = subscriptionRequest.Interval,
                    ApplicationPath = subscriptionRequest.ApplicationPath
                });
                Log.Information($"客户端 {uuid} 订阅了应用详情: {subscriptionRequest.ApplicationPath}");
            }
            else
            {
                await WriteErrorResponseAsync(context, "无效的订阅请求，缺少 'ApplicationPath'");
                return;
            }
        }
        catch (JsonException ex)
        {
            await WriteErrorResponseAsync(context, $"请求数据格式错误: {ex.Message}");
            return;
        }

        await WriteJsonResponseAsync(context, new ResponseModel<string>
        {
            Success = true,
            Data = "成功",
            Message = "订阅成功"
        });
    }

    public async Task UnsubscribeAsync(HttpContext context)
    {
        if (string.IsNullOrEmpty(context.RequestBody))
        {
            await WriteErrorResponseAsync(context, "请求体不能为空");
            return;
        }

        try
        {
            string? uuid = GetHeaderValue(context, "uuid");
            Log.Warning($"接收到的数据: {context.RequestBody}, 用户 Id: {uuid}");

            var subscriptionInfo = ParseRequestBody<SubscriptionInfo>(context.RequestBody);
            if (subscriptionInfo != null && !string.IsNullOrWhiteSpace(uuid))
            {
                if (AppConfig.ApplicationInfoSubscribe.Equals(subscriptionInfo.SubscriptionType))
                {
                    DispatchEngine.Instance.DeleteApplicationInfo(uuid);
                }
                else if (AppConfig.ProcessInfoSubscribe.Equals(subscriptionInfo.SubscriptionType))
                {
                    DispatchEngine.Instance.DeleteProcessInfo(uuid);
                }
                else if (AppConfig.AppDetailInfoSubscribe.Equals(subscriptionInfo.SubscriptionType))
                {
                    DispatchEngine.Instance.DeleteAppDetailInfo(uuid);
                }

                Log.Warning($"取消订阅的 名称: {subscriptionInfo.SubscriptionType}");
            }
        }
        catch (JsonException ex)
        {
            await WriteErrorResponseAsync(context, $"请求数据格式错误: {ex.Message}");
            return;
        }

        await WriteJsonResponseAsync(context, new ResponseModel<string>
        {
            Success = true,
            Data = "成功",
            Message = "取消订阅成功"
        });
    }
}
