using System.Configuration;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Windows;
using System.Windows.Threading;
using Application;
using Application.Utils;
using Common;
using Common.Logger;
using Common.Net.HttpConn;
using Common.Net.Models;
using Common.Net.WebSocketConn;
using Common.Utils;
using Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using Shell;
using Utils.ETW.Etw;

namespace NetVizor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 异常处理
        ExceptionCatch();
        // 初始化日志
        var configModelLogging = AppConfig.Instance.ConfigModel.Logging;
        Log.Initialize(configModelLogging);
        base.OnStartup(e);

        // 启动服务（如 WebSocket、HTTP、端口监听等）
        // StartMyServer();
    }

    /// <summary>
    ///     异常捕捉
    /// </summary>
    private void ExceptionCatch()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        // 注册全局异常处理
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    /// <summary>
    ///     处理异常
    /// </summary>
    /// <param name="exception"></param>
    private void HandleException(Exception? exception)
    {
        Log.Error("未捕获异常", exception);
        // 错误记录，错误处理
        if (exception != null)
        {
            // todo:2023年10月29日 恭喜你发现了BUG页面
            MessageBox.Show($"恭喜你发现了BUG，请联系管理员解决：{exception.Message}");
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "UI线程异常");
        HandleException(e.Exception);
        // 根据异常类型决定是否继续运行
        if (CanRecover(e.Exception))
        {
            e.Handled = true; // 标记为已处理，防止应用崩溃
            // ShowUserFriendlyMessage("操作失败，请重试");
        }
        else
        {
            // ShowCriticalErrorDialog(e.Exception);
            // 让应用正常关闭而不是崩溃
        }
    }

    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        foreach (var ex in e.Exception.InnerExceptions)
        {
            Log.Error(e.Exception, "UI线程异常");
        }

        e.SetObserved(); // 标记异常已被观察，防止应用终止
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        Log.Error(exception, "非UI线程异常");

        if (e.IsTerminating)
        {
            // 应用即将终止，进行紧急清理
            // EmergencyCleanup();
            Log.Fatal("应用程序即将因未处理异常而终止");
        }
    }

    private bool CanRecover(Exception exception)
    {
        return exception switch
        {
            ArgumentException => true,
            InvalidOperationException => true,
            HttpRequestException => true,
            TimeoutException => true,
            OutOfMemoryException => false,
            StackOverflowException => false,
            AccessViolationException => false,
            _ => true
        };
    }

    private void StartMyServer()
    {
        int port = SysHelper.GetAvailablePort();

        AppConfig.Instance.WebSocketPort = port;

        AppConfig.Instance.WebSocketPath = $"ws://127.0.0.1:{port}";
        Log.Information4Ctx($"服务启动在端口: {AppConfig.Instance.WebSocketPort}");
        Log.Information($"服务完整地址: {AppConfig.Instance.WebSocketPath}");

        // 启动WebSocket服务器
        WebSocketManager.Instance.Start(AppConfig.Instance.WebSocketPath);

        // 订阅 websocket 关闭服务
        WebSocketManager.Instance.SubscribeConnectionClosed((args =>
                {
                    if (args.Uuid != null)
                    {
                        DispatchEngine.Instance.DeleteApplicationInfo(args.Uuid);
                        DispatchEngine.Instance.DeleteProcessInfo(args.Uuid);
                        DispatchEngine.Instance.DeleteAppDetailInfo(args.Uuid);
                    }
                }
            ));
        // 开启 WebSocket 定时发送服务
        DispatchEngine.Instance.ApplicationInfoDistribute();
        DispatchEngine.Instance.ProcessInfoDistribute();
        DispatchEngine.Instance.AppDetailInfoDistribute();

        // 启动 http 服务
        Task.Run(() => { _ = StartHttpServer(); });

        // 开启监听
        NewMethod();
    }

    private static async Task NewMethod()
    {
        if (!SysHelper.IsAdministrator())
        {
            Console.WriteLine("此程序需要管理员权限才能运行ETW监控！");
            Console.WriteLine("请以管理员身份重新运行程序。");
            Console.ReadKey();
            return;
        }

        var _networkManager = new EnhancedEtwNetworkManager();
        _networkManager.StartMonitoring();
    }

    private static async Task StartHttpServer()
    {
        // 开启 http 服务
        var server = new HttpServer("http://localhost:8268/");
        // 添加中间件
        server.UseMiddleware(Middlewares.RequestLogging);
        server.UseMiddleware(Middlewares.Cors);

        // 添加路由
        server.Get("/api",
            async (context) => { await context.Response.WriteJsonAsync(new { message = "Hi!" }); });

        #region 软件信息订阅

        server.Post("/api/subscribe-application", async (context) =>
        {
            // 正确的做法：只处理请求体数据
            if (string.IsNullOrEmpty(context.RequestBody))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = "请求体不能为空",
                });
                return;
            }

            // 解析请求数据
            try
            {
                // 直接使用已经读取的数据
                string requestData = context.RequestBody;

                // 打印原始数据用于调试
                string? uuid = context.Request.Headers["uuid"];
                Log.Warning($"接收到的数据: {requestData}, 用户 Id: {uuid}");

                if (string.IsNullOrWhiteSpace(uuid))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = $"用户 ID 丢失!",
                    });
                    return;
                }

                var subscriptionInfo = JsonHelper.FromJson<SubscriptionInfo>(requestData);
                if (subscriptionInfo != null)
                {
                    // 软件信息订阅
                    DispatchEngine.Instance.AddApplicationInfo(uuid, new DispatchModel
                    {
                        Interval = subscriptionInfo.Interval
                    });


                    Log.Warning($"时间: {subscriptionInfo.Interval}");
                }
            }
            catch (JsonException ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"请求数据格式错误: {ex.Message}",
                });
                return;
            }

            // 返回响应 - 只序列化纯数据对象
            context.Response.StatusCode = 200;
            await context.Response.WriteJsonAsync(new ResponseModel<string>
            {
                Success = true,
                Data = "成功",
                Message = "订阅成功",
            });
        });

        #endregion

        #region 线程信息订阅

        server.Post("/api/subscribe-process", async (context) =>
        {
            if (string.IsNullOrEmpty(context.RequestBody))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = "请求体不能为空",
                });
                return;
            }

            // 解析请求数据
            try
            {
                string requestData = context.RequestBody;

                // 打印原始数据用于调试
                string? uuid = context.Request.Headers["uuid"];

                if (string.IsNullOrWhiteSpace(uuid))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = $"用户 ID 丢失!",
                    });
                    return;
                }

                var subscriptionInfo = JsonHelper.FromJson<SubscriptionProcessInfo>(requestData);
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
                context.Response.StatusCode = 400;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"请求数据格式错误: {ex.Message}",
                });
                return;
            }

            // 返回响应 - 只序列化纯数据对象
            context.Response.StatusCode = 200;
            await context.Response.WriteJsonAsync(new ResponseModel<string>
            {
                Success = true,
                Data = "成功",
                Message = "订阅成功",
            });
        });

        #endregion

        #region 软件详细信息订阅

        server.Post("/api/subscribe-appinfo", async (context) =>
        {
            if (string.IsNullOrEmpty(context.RequestBody))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteJsonAsync(
                    new ResponseModel<object> { Success = false, Message = "请求体不能为空" });
                return;
            }

            try
            {
                string? uuid = context.Request.Headers["uuid"];
                if (string.IsNullOrWhiteSpace(uuid))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                        { Success = false, Message = "用户 ID 丢失!" });
                    return;
                }

                var subscriptionRequest = JsonHelper.FromJson<SubscriptionAppInfo>(context.RequestBody);
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
                    context.Response.StatusCode = 400;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                        { Success = false, Message = "无效的订阅请求，缺少 'ApplicationPath'" });
                    return;
                }
            }
            catch (JsonException ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                    { Success = false, Message = $"请求数据格式错误: {ex.Message}" });
                return;
            }

            context.Response.StatusCode = 200;
            await context.Response.WriteJsonAsync(new ResponseModel<string>
                { Success = true, Data = "成功", Message = "订阅成功" });
        });

        #endregion

        #region 取消订阅

        server.Post("/api/unsubscribe", async (context) =>
        {
            // 正确的做法：只处理请求体数据
            if (string.IsNullOrEmpty(context.RequestBody))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = "请求体不能为空",
                });
                return;
            }

            // 解析请求数据
            try
            {
                // 直接使用已经读取的数据
                string requestData = context.RequestBody;

                // 打印原始数据用于调试
                string? uuid = context.Request.Headers["uuid"];
                Log.Warning($"接收到的数据: {requestData}, 用户 Id: {uuid}");

                var subscriptionInfo = JsonHelper.FromJson<SubscriptionInfo>(requestData);
                if (subscriptionInfo != null && !string.IsNullOrWhiteSpace(uuid))
                {
                    // 软件信息订阅
                    if (AppConfig.ApplicationInfoSubscribe.Equals(subscriptionInfo.SubscriptionType))
                    {
                        DispatchEngine.Instance.DeleteApplicationInfo(uuid);
                    }
                    // 进程信息订阅
                    else if (AppConfig.ProcessInfoSubscribe.Equals(subscriptionInfo.SubscriptionType))
                    {
                        DispatchEngine.Instance.DeleteProcessInfo(uuid);
                    }
                    // 特定应用详情订阅
                    else if (AppConfig.AppDetailInfoSubscribe.Equals(subscriptionInfo.SubscriptionType))
                    {
                        DispatchEngine.Instance.DeleteAppDetailInfo(uuid);
                    }

                    Log.Warning($"取消订阅的 名称: {subscriptionInfo.SubscriptionType}");
                }
            }
            catch (JsonException ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"请求数据格式错误: {ex.Message}",
                });
                return;
            }

            // 返回响应 - 只序列化纯数据对象
            context.Response.StatusCode = 200;
            await context.Response.WriteJsonAsync(new ResponseModel<string>
            {
                Success = true,
                Data = "成功",
                Message = "取消订阅成功",
            });
        });

        #endregion

        // 启动服务器
        try
        {
            await server.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server error: {ex.Message}");
        }
    }
}

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