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
using Shell.Views;
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
        
        // NetView
        // var netView = new NetView();
        // netView.Show();
        
        var window = new NetView();
        window.Show();

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

// 对于C#来说，我想获取windows中的防火墙的信息？我在开发一个网络监控软件，我想顺便把防火墙管理给集成进去，前端搭配vue3展示，应该也能很漂亮。
// 所以第一步是获取防火墙信息，并且要是结构化的，能够转换为json发给前端的，所以我应该如何做？虽然有可能有些可能用不到，不过我希望尽可能多的获取到防火墙的信息。
// 我要完成的功能包括：获取所有防火墙信息，打开或关闭防火墙，更改某个规则（重命名、开关等），删除某个规则，增加新的规则。
// 基于此，对于它的C#后端，我们应该定义和设计这些API？
// 我想要获取尽可能全面的信息，包括但不限于出站、进站、程序、协议、端口、远程端口、作用域、操作、配置文件、名称等。
// 我想尽可能的复现windows官方的防火墙中的全部功能。
// 我们首先需要定义其全部的API，比如读取，修改，新增，删除。
//
//
//
// 我在开发一个网络监控软件，我想顺便把防火墙管理给集成进去，前端搭配vue3展示，应该也能很漂亮。
// 我现在后端已经能拿到了很多信息， 包括但不限于出站、进站、程序、协议、端口、远程端口、作用域、操作、配置文件、名称等。
// 我想尽可能的复现windows官方的防火墙中的全部功能。但是UI要更好看。
// 但是我也尽可能贴近官方防火墙设置的功能比如：比如windows官方防火墙的 “程序”-》自定义服务设置-》应用于下列服务，它的服务有很多，我们需要展示提供选择。
// 再有，“协议和端口”中的协议类型也有很多，我们也要展示。比如它甚至能指定用户，可以选择用户和组。
// 所以基于上，我们几乎是重写了防火墙的页面。
// 我现在需要你基于这个能获取到的信息（可能有些我没提到），使用html完成一个防火墙的看板，要点击某个规则可以编辑，可以新增。
// 新增的窗口和交互都需要完成，数据的展示也需要完成。你用假数据替代数据填充即可，要求界面要现代化，美观，优雅
