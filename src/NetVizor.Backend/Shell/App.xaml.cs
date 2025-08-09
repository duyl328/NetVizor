using System.Net.Http;
using System.Text.Json;
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
using Data;
using Shell.Views;
using Utils.ETW.Etw;
using Utils.Firewall;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Shell;
// 引用 Windows Forms 命名空间

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        // 异常处理
        ExceptionCatch();
        // 初始化日志
        var configModelLogging = AppConfig.Instance.ConfigModel.Logging;
        Log.Initialize(configModelLogging);
        base.OnStartup(e);

        // 初始化数据库和数据收集服务
        try
        {
            Log.Information("正在初始化数据库和网络监控服务...");
            await DatabaseManager.InitializeAsync();
            Log.Information("数据库和网络监控服务初始化完成");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据库初始化失败");
            MessageBox.Show($"数据库初始化失败: {ex.Message}", "错误", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }

        // 启动服务（如 WebSocket、HTTP、端口监听等）
        StartMyServer();

        // 创建 NetView 窗口 - 让它自己处理设置加载和应用
        try
        {
            Log.Information("创建 NetView 窗口...");
            var netView = new NetView();
            netView.Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "创建 NetView 窗口失败");
            throw; // 重新抛出异常，因为这是关键组件
        }

        // Mouth();

        // 设置应用程序在关闭最后一个窗口时不自动退出
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    private async void Mouth()
    {
        var firewallService = new FirewallService();
        // 防火墙状态
        // var statusAsync = await firewallService.GetStatusAsync();
        var rulesAsync = await firewallService.GetRulesAsync();
        Console.WriteLine(JsonHelper.ToJson(rulesAsync));
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

        #region 获取防火墙信息

        // 查询防火墙规则 - 支持分页和筛选
        server.Get("/api/firewall/rules", async (context) =>
        {
            try
            {
                var firewallApi = new WindowsFirewallApi();

                // 获取查询参数
                var queryParams = context.QueryParams;
                var startIndex = !string.IsNullOrEmpty(queryParams["start"]) ? int.Parse(queryParams["start"]) : 0;
                var limit = !string.IsNullOrEmpty(queryParams["limit"]) ? int.Parse(queryParams["limit"]) : 50;

                // 构建筛选条件
                var filter = new RuleFilter();
                if (!string.IsNullOrEmpty(queryParams["name"])) filter.NamePattern = queryParams["name"];
                if (!string.IsNullOrEmpty(queryParams["direction"]) &&
                    Enum.TryParse<RuleDirection>(queryParams["direction"], true, out var direction))
                    filter.Direction = direction;
                if (!string.IsNullOrEmpty(queryParams["enabled"]) &&
                    bool.TryParse(queryParams["enabled"], out var enabled))
                    filter.Enabled = enabled;
                if (!string.IsNullOrEmpty(queryParams["protocol"]) &&
                    Enum.TryParse<ProtocolType>(queryParams["protocol"], true, out var protocol))
                    filter.Protocol = protocol;
                if (!string.IsNullOrEmpty(queryParams["action"]) &&
                    Enum.TryParse<RuleAction>(queryParams["action"], true, out var action))
                    filter.Action = action;
                if (!string.IsNullOrEmpty(queryParams["application"]))
                    filter.ApplicationName = queryParams["application"];
                if (!string.IsNullOrEmpty(queryParams["port"])) filter.Port = queryParams["port"];
                // 添加关键字搜索支持
                if (!string.IsNullOrEmpty(queryParams["search"])) filter.SearchKeyword = queryParams["search"];

                // 获取筛选后的规则
                var allRules = firewallApi.GetRulesByFilter(filter);
                var totalCount = allRules.Count;

                // 分页
                var pagedRules = allRules.Skip(startIndex).Take(limit).ToList();

                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = true,
                    Data = new
                    {
                        rules = pagedRules,
                        totalCount = totalCount,
                        startIndex = startIndex,
                        limit = limit,
                        hasMore = startIndex + limit < totalCount
                    },
                    Message = "查询成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"查询防火墙规则失败: {ex.Message}"
                });
            }
        });

        // 新增防火墙规则
        server.Post("/api/firewall/rules", async (context) =>
        {
            try
            {
                Console.WriteLine(context.RequestBody);
                if (string.IsNullOrEmpty(context.RequestBody))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = "请求体不能为空"
                    });
                    return;
                }

                var createRequest = JsonHelper.FromJson<CreateRuleRequest>(context.RequestBody);
                if (createRequest == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = "请求数据格式错误"
                    });
                    return;
                }

                var firewallApi = new WindowsFirewallApi();
                var success = firewallApi.CreateRule(createRequest);

                if (success)
                {
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = true,
                        Message = "防火墙规则创建成功"
                    });
                }
                else
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = "防火墙规则创建失败"
                    });
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"创建防火墙规则失败: {ex.Message}"
                });
            }
        });

        // 编辑防火墙规则
        server.Put("/api/firewall/rules", async (context) =>
        {
            try
            {
                if (string.IsNullOrEmpty(context.RequestBody))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = "请求体不能为空"
                    });
                    return;
                }

                var updateRequest = JsonHelper.FromJson<UpdateRuleRequest>(context.RequestBody);
                if (updateRequest == null || string.IsNullOrEmpty(updateRequest.CurrentName))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = "请求数据格式错误或缺少规则名称"
                    });
                    return;
                }

                var firewallApi = new WindowsFirewallApi();
                var success = firewallApi.UpdateRule(updateRequest);

                if (success)
                {
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = true,
                        Message = "防火墙规则更新成功"
                    });
                }
                else
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = "防火墙规则更新失败，可能规则不存在"
                    });
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"更新防火墙规则失败: {ex.Message}"
                });
            }
        });

        // 删除防火墙规则
        server.Delete("/api/firewall/rules", async (context) =>
        {
            try
            {
                var queryParams = context.QueryParams;
                if (string.IsNullOrEmpty(queryParams["name"]))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = "缺少规则名称参数"
                    });
                    return;
                }

                var ruleName = queryParams["name"];
                var firewallApi = new WindowsFirewallApi();
                var success = firewallApi.DeleteRule(ruleName);

                if (success)
                {
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = true,
                        Message = "防火墙规则删除成功"
                    });
                }
                else
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = "防火墙规则删除失败，可能规则不存在"
                    });
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"删除防火墙规则失败: {ex.Message}"
                });
            }
        });

        // 获取防火墙状态
        server.Get("/api/firewall/status", async (context) =>
        {
            try
            {
                var firewallApi = new WindowsFirewallApi();
                var status = firewallApi.GetFirewallStatus();

                await context.Response.WriteJsonAsync(new ResponseModel<FirewallStatus>
                {
                    Success = true,
                    Data = status,
                    Message = "获取防火墙状态成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"获取防火墙状态失败: {ex.Message}"
                });
            }
        });

        // 获取防火墙统计信息
        server.Get("/api/firewall/statistics", async (context) =>
        {
            try
            {
                var firewallApi = new WindowsFirewallApi();
                var statistics = firewallApi.GetStatistics();

                await context.Response.WriteJsonAsync(new ResponseModel<FirewallStatistics>
                {
                    Success = true,
                    Data = statistics,
                    Message = "获取防火墙统计信息成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"获取防火墙统计信息失败: {ex.Message}"
                });
            }
        });

        #endregion

        #region 防火墙开关

        // 防火墙开关控制
        server.Post("/api/firewall/switch", async (context) =>
        {
            try
            {
                var queryParams = context.QueryParams;
                // 解析请求参数
                var enabledParam = !string.IsNullOrEmpty(queryParams["enabled"]) ? queryParams["enabled"] : "";
                var profileParam = !string.IsNullOrEmpty(queryParams["profile"]) ? queryParams["enabled"] : "";

                // 解析enabled参数
                if (string.IsNullOrEmpty(enabledParam))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = "缺少必需参数 'enabled'，请使用 true 或 false",
                        Data = null
                    });
                    return;
                }

                var trim = enabledParam.Trim();
                bool enabled = trim switch
                {
                    "true" => true,
                    "TRUE" => true,
                    "True" => true,
                    "false" => false,
                    "FALSE" => false,
                    "False" => false,
                    "1" => true,
                    "0" => false,
                    _ => throw new ArgumentException("enabled参数值无效，请使用 true/false 或 1/0")
                };

                // 解析profile参数
                FirewallProfile profile = FirewallProfile.All;
                if (!string.IsNullOrEmpty(profileParam))
                {
                    profile = profileParam switch
                    {
                        "domain" => FirewallProfile.Domain,
                        "private" => FirewallProfile.Private,
                        "public" => FirewallProfile.Public,
                        "all" => FirewallProfile.All,
                        _ => FirewallProfile.All
                    };
                }

                var api = new WindowsFirewallApi();
                bool result;
                string action;

                if (enabled)
                {
                    result = api.EnableFirewall(profile);
                    action = "启用";
                }
                else
                {
                    result = api.DisableFirewall(profile);
                    action = "禁用";
                }

                if (result)
                {
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = true,
                        Message = $"防火墙{action}成功 ({profile})",
                        Data = new
                        {
                            profile = profile.ToString(),
                            enabled = enabled,
                            action = action
                        }
                    });
                }
                else
                {
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = $"防火墙{action}失败，可能需要管理员权限",
                        Data = null
                    });
                }
            }
            catch (ArgumentException ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"操作防火墙时发生错误: {ex.Message}",
                    Data = null
                });
            }
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

        #region 网络数据API接口

        // 获取全局网络实时数据
        server.Get("/api/network/global/realtime", async (context) =>
        {
            try
            {
                var networkCardGuid = context.QueryParams["networkCardGuid"];

                // 获取网络接口状态
                var interfaces = BasicNetworkMonitor.GetConnectedNetworkInterfaces();
                var targetInterfaces = string.IsNullOrEmpty(networkCardGuid)
                    ? interfaces
                    : interfaces.Where(i => i.Id == networkCardGuid).ToList();

                var realtimeData = new List<object>();
                foreach (var networkInterface in targetInterfaces)
                {
                    var speed = BasicNetworkMonitor.CalculateSpeedById(networkInterface.Id);
                    realtimeData.Add(new
                    {
                        networkCardGuid = networkInterface.Id,
                        networkCardName = networkInterface.Name,
                        uploadSpeed = speed.UploadSpeed,
                        downloadSpeed = speed.DownloadSpeed,
                        uploadSpeedText = speed.UploadSpeedText,
                        downloadSpeedText = speed.DownloadSpeedText,
                        isConnected = networkInterface.IsConnected,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    });
                }

                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = true,
                    Data = realtimeData,
                    Message = "获取实时网络数据成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"获取实时网络数据失败: {ex.Message}"
                });
            }
        });

        // 获取实时活跃应用
        server.Get("/api/realtime/active-apps", async (context) =>
        {
            try
            {
                var limit = int.Parse(context.QueryParams["limit"] ?? "10");

                // 获取当前活跃的网络监控快照
                var snapshot = GlobalNetworkMonitor.Instance.GetSnapshot();

                var activeApps = snapshot.Applications
                    .Where(app => app.Connections.Any(c => c.IsActive && (c.BytesSent > 0 || c.BytesReceived > 0)))
                    .OrderByDescending(app => app.Connections.Sum(c => c.BytesSent + c.BytesReceived))
                    .Take(limit)
                    .Select(app => new
                    {
                        processId = app.ProcessId,
                        processName = app.ProgramInfo?.ProcessName ?? "Unknown",
                        path = app.ProgramInfo?.MainModulePath ?? "",
                        totalUploadBytes = app.Connections.Sum(c => c.BytesSent),
                        totalDownloadBytes = app.Connections.Sum(c => c.BytesReceived),
                        activeConnections = app.Connections.Count(c => c.IsActive),
                        icon = app.ProgramInfo?.IconBase64 ?? ""
                    })
                    .ToList();

                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = true,
                    Data = activeApps,
                    Message = "获取实时活跃应用成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"获取实时活跃应用失败: {ex.Message}"
                });
            }
        });

        // 获取系统信息
        server.Get("/api/system/info", async (context) =>
        {
            try
            {
                var systemInfo = new
                {
                    version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ??
                              "Unknown",
                    isAdministrator = SysHelper.IsAdministrator(),
                    etwEnabled = SysHelper.IsAdministrator(), // ETW需要管理员权限
                    operatingSystem = Environment.OSVersion.ToString(),
                    machineName = Environment.MachineName,
                    userDomainName = Environment.UserDomainName,
                    userName = Environment.UserName,
                    processorCount = Environment.ProcessorCount,
                    workingSet = Environment.WorkingSet,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = true,
                    Data = systemInfo,
                    Message = "获取系统信息成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"获取系统信息失败: {ex.Message}"
                });
            }
        });

        // 获取数据收集统计信息
        server.Get("/api/system/collection-stats", async (context) =>
        {
            try
            {
                // TODO: 实现获取NetworkDataCollectionService的统计信息
                var stats = new
                {
                    isRunning = true,
                    activeNetworkInterfaces = BasicNetworkMonitor.GetConnectedNetworkInterfaces().Count,
                    etwStatus = SysHelper.IsAdministrator() ? "运行中" : "需要管理员权限",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = true,
                    Data = stats,
                    Message = "获取收集统计信息成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"获取收集统计信息失败: {ex.Message}"
                });
            }
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