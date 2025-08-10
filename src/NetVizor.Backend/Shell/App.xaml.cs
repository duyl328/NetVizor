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
using Data.Models;
using Data.Core;
using Data.Repositories;
using Shell.Views;
using Utils.ETW.Etw;
using Utils.Firewall;
using Dapper;
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

    // 网络接口缓存
    private static readonly Dictionary<string, object> _networkInterfaceCache = new Dictionary<string, object>();
    private static DateTime _cacheLastUpdateTime = DateTime.MinValue;
    private static readonly TimeSpan _cacheExpireTime = TimeSpan.FromMinutes(5); // 缓存5分钟

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

        #region 网络统计API

        // 获取网络接口列表
        server.Get("/api/statistics/interfaces", async (context) =>
        {
            try
            {
                var timeRange = context.QueryParams["timeRange"] ?? "hour";
                
                // 检查缓存是否有效
                var cacheKey = $"interfaces_{timeRange}";
                var now = DateTime.Now;
                
                if (_networkInterfaceCache.ContainsKey(cacheKey) && 
                    (now - _cacheLastUpdateTime) < _cacheExpireTime)
                {
                    // 使用缓存数据
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = true,
                        Data = _networkInterfaceCache[cacheKey],
                        Message = "获取网络接口列表成功（缓存）"
                    });
                    return;
                }

                // 缓存已过期或不存在，重新获取数据
                var networkCardGuids = new HashSet<string>();

                // 根据时间范围从不同数据表获取网卡ID
                switch (timeRange.ToLower())
                {
                    case "hour":
                        // 从 GlobalNetwork 表获取最近1小时的数据
                        var hourData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByTimeRangeAsync("", 
                            DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(), 
                            DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                        foreach (var item in hourData)
                            networkCardGuids.Add(item.NetworkCardGuid);
                        break;
                    case "day":
                        // 从 GlobalNetwork 表获取最近1天的数据
                        var dayData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByTimeRangeAsync("", 
                            DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(), 
                            DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                        foreach (var item in dayData)
                            networkCardGuids.Add(item.NetworkCardGuid);
                        break;
                    case "week":
                        // 从 GlobalNetwork 表获取最近7天的数据
                        var weekData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByTimeRangeAsync("", 
                            DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds(), 
                            DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                        foreach (var item in weekData)
                            networkCardGuids.Add(item.NetworkCardGuid);
                        break;
                    case "month":
                        // 从 GlobalNetwork 表获取最近30天的数据
                        var monthData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByTimeRangeAsync("", 
                            DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds(), 
                            DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                        foreach (var item in monthData)
                            networkCardGuids.Add(item.NetworkCardGuid);
                        break;
                }

                // 如果没有找到任何网卡数据，使用当前连接的网卡
                if (!networkCardGuids.Any())
                {
                    var connectedInterfaces = BasicNetworkMonitor.GetConnectedNetworkInterfaces();
                    foreach (var iface in connectedInterfaces)
                    {
                        networkCardGuids.Add(iface.Id);
                    }
                }

                // 获取每个网卡的详细信息
                var interfaces = new List<object>();
                foreach (var guid in networkCardGuids)
                {
                    var networkInfo = NetworkAdapterHelper.GetNetworkInfoByGuid(guid);
                    if (networkInfo != null)
                    {
                        interfaces.Add(new
                        {
                            id = guid,
                            name = networkInfo.NetConnectionID ?? networkInfo.Name,
                            displayName = networkInfo.Description ?? networkInfo.Name,
                            isActive = true, // 因为在数据库中有流量数据，所以认为是可用的
                            macAddress = networkInfo.MACAddress
                        });
                    }
                    else
                    {
                        // 如果无法获取网卡信息，但数据库中有流量数据，仍然认为是有效的
                        interfaces.Add(new
                        {
                            id = guid,
                            name = $"Network Interface {guid.Substring(0, 8)}",
                            displayName = $"Network Adapter {guid.Substring(0, 8)}",
                            isActive = true, // 有流量数据就认为是可用的
                            macAddress = ""
                        });
                    }
                }

                // 更新缓存
                _networkInterfaceCache[cacheKey] = interfaces;
                _cacheLastUpdateTime = now;

                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = true,
                    Data = interfaces,
                    Message = "获取网络接口列表成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"获取网络接口列表失败: {ex.Message}"
                });
            }
        });

        // 获取可用时间范围
        server.Get("/api/statistics/available-ranges", async (context) =>
        {
            try
            {
                var availableRanges = new List<object>();
                var now = DateTimeOffset.UtcNow;

                // 检查是否有小时数据 (GlobalNetworkHourly)
                var hasHourlyData = await HasNetworkDataInTimeRangeAsync("hourly", now.AddHours(-1).ToUnixTimeSeconds());
                if (hasHourlyData)
                {
                    availableRanges.Add(new
                    {
                        type = "hour",
                        name = "1小时",
                        available = true,
                        startTime = now.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });
                }

                // 检查是否有天数据 (GlobalNetworkDaily)
                var hasDailyData = await HasNetworkDataInTimeRangeAsync("daily", now.AddDays(-1).ToUnixTimeSeconds());
                if (hasDailyData)
                {
                    availableRanges.Add(new
                    {
                        type = "day",
                        name = "1天",
                        available = true,
                        startTime = now.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });
                }

                // 检查是否有周数据
                var hasWeeklyData = await HasNetworkDataInTimeRangeAsync("daily", now.AddDays(-7).ToUnixTimeSeconds());
                if (hasWeeklyData)
                {
                    availableRanges.Add(new
                    {
                        type = "week",
                        name = "1周",
                        available = true,
                        startTime = now.AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });
                }

                // 检查是否有月数据
                var hasMonthlyData = await HasNetworkDataInTimeRangeAsync("daily", now.AddDays(-30).ToUnixTimeSeconds());
                if (hasMonthlyData)
                {
                    availableRanges.Add(new
                    {
                        type = "month",
                        name = "1个月",
                        available = true,
                        startTime = now.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });
                }

                // 如果没有任何数据，至少返回hour（用户刚安装）
                if (!availableRanges.Any())
                {
                    availableRanges.Add(new
                    {
                        type = "hour",
                        name = "1小时",
                        available = true,
                        startTime = now.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });
                }

                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = true,
                    Data = availableRanges.First(),
                    Message = "获取可用时间范围成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"获取可用时间范围失败: {ex.Message}"
                });
            }
        });

        // 清理网络接口缓存
        server.Post("/api/statistics/clear-cache", async (context) =>
        {
            try
            {
                _networkInterfaceCache.Clear();
                _cacheLastUpdateTime = DateTime.MinValue;
                
                await context.Response.WriteJsonAsync(new ResponseModel<string>
                {
                    Success = true,
                    Data = "缓存已清理",
                    Message = "网络接口缓存清理成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"清理缓存失败: {ex.Message}"
                });
            }
        });

        #endregion

        #region 流量数据API

        // 获取流量趋势数据
        server.Get("/api/traffic/trends", async (context) =>
        {
            try
            {
                var timeRange = context.QueryParams["timeRange"] ?? "1hour";
                var interfaceId = context.QueryParams["interfaceId"] ?? "all";
                
                var points = new List<object>();
                var now = DateTimeOffset.UtcNow;

                switch (timeRange)
                {
                    case "1hour":
                        // 从 GlobalNetwork 表获取最近1小时数据，按5秒间隔
                        var hourData = await GetTrafficTrendsAsync("hour", interfaceId, now.AddHours(-1).ToUnixTimeSeconds(), now.ToUnixTimeSeconds());
                        points = hourData;
                        break;
                    case "1day":
                        // 从 GlobalNetworkHourly 表获取最近1天数据，按小时间隔
                        var dayData = await GetTrafficTrendsAsync("day", interfaceId, now.AddDays(-1).ToUnixTimeSeconds(), now.ToUnixTimeSeconds());
                        points = dayData;
                        break;
                    case "7days":
                        // 从 GlobalNetworkDaily 表获取最近7天数据，按天间隔
                        var weekData = await GetTrafficTrendsAsync("week", interfaceId, now.AddDays(-7).ToUnixTimeSeconds(), now.ToUnixTimeSeconds());
                        points = weekData;
                        break;
                    case "30days":
                        // 从 GlobalNetworkDaily 表获取最近30天数据，按天间隔
                        var monthData = await GetTrafficTrendsAsync("month", interfaceId, now.AddDays(-30).ToUnixTimeSeconds(), now.ToUnixTimeSeconds());
                        points = monthData;
                        break;
                }

                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = true,
                    Data = new
                    {
                        @interface = interfaceId,
                        timeRange = timeRange,
                        points = points
                    },
                    Message = "获取流量趋势数据成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"获取流量趋势数据失败: {ex.Message}"
                });
            }
        });

        // 获取Top应用流量数据
        server.Get("/api/traffic/top-apps", async (context) =>
        {
            try
            {
                var timeRange = context.QueryParams["timeRange"] ?? "1hour";
                var limit = int.Parse(context.QueryParams["limit"] ?? "10");
                
                // 根据时间范围计算天数，对于1hour特殊处理
                IEnumerable<AppNetworkTopInfo> topApps;
                
                if (timeRange == "1hour")
                {
                    // 对于1小时数据，我们需要特殊处理，因为GetTopAppsByTrafficAsync不支持小时查询
                    // 使用1天的数据作为替代，实际应用中可能需要创建专门的小时查询方法
                    topApps = await DatabaseManager.Instance.Networks.GetTopAppsByTrafficAsync(limit, 1);
                }
                else
                {
                    int days = timeRange switch
                    {
                        "1day" => 1,
                        "7days" => 7,
                        "30days" => 30,
                        _ => 1
                    };
                    topApps = await DatabaseManager.Instance.Networks.GetTopAppsByTrafficAsync(limit, days);
                }
                var result = new List<object>();

                foreach (var app in topApps)
                {
                    // GetTopAppsByTrafficAsync 已经通过 LEFT JOIN 获取了应用信息
                    // 只有当JOIN没有获取到信息时才额外查询AppInfo表
                    string processName = System.IO.Path.GetFileName(app.AppPath ?? "");
                    string displayName = app.AppName ?? "";
                    string iconBase64 = "";
                    
                    if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(app.AppId))
                    {
                        // 如果JOIN没有获取到应用名称，尝试单独查询
                        var appInfo = await DatabaseManager.Instance.AppInfos.GetAppByAppIdAsync(app.AppId);
                        if (appInfo != null)
                        {
                            displayName = appInfo.Name ?? "";
                            iconBase64 = appInfo.Base64Icon ?? "";
                        }
                    }
                    
                    if (string.IsNullOrEmpty(displayName))
                    {
                        displayName = processName.Replace(".exe", "") ?? "Unknown";
                    }

                    result.Add(new
                    {
                        processName = processName,
                        displayName = displayName,
                        icon = iconBase64,
                        totalBytes = app.TotalTraffic
                    });
                }

                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = true,
                    Data = result,
                    Message = "获取Top应用流量数据成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"获取Top应用流量数据失败: {ex.Message}"
                });
            }
        });

        #endregion

        #region 软件排行榜API

        // 获取软件流量TOP100排行
        server.Get("/api/apps/top-traffic", async (context) =>
        {
            try
            {
                var timeRange = context.QueryParams["timeRange"] ?? "1hour";
                var page = int.Parse(context.QueryParams["page"] ?? "1");
                var pageSize = int.Parse(context.QueryParams["pageSize"] ?? "100");
                
                // 根据时间范围计算天数，对于1hour特殊处理
                IEnumerable<AppNetworkTopInfo> allApps;
                
                if (timeRange == "1hour")
                {
                    // 对于1小时数据，我们需要特殊处理，因为GetTopAppsByTrafficAsync不支持小时查询
                    // 使用1天的数据作为替代，实际应用中可能需要创建专门的小时查询方法
                    allApps = await DatabaseManager.Instance.Networks.GetTopAppsByTrafficAsync(pageSize * page, 1);
                }
                else
                {
                    int days = timeRange switch
                    {
                        "1day" => 1,
                        "7days" => 7,
                        "30days" => 30,
                        _ => 1
                    };
                    // 获取总数据（限制较大数量用于分页）
                    allApps = await DatabaseManager.Instance.Networks.GetTopAppsByTrafficAsync(pageSize * page, days);
                }
                var totalCount = allApps.Count();
                
                // 分页处理
                var pagedApps = allApps.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                var items = new List<object>();
                int rank = (page - 1) * pageSize + 1;

                foreach (var app in pagedApps)
                {
                    // GetTopAppsByTrafficAsync 已经通过 LEFT JOIN 获取了应用信息
                    // 只有当JOIN没有获取到信息时才额外查询AppInfo表
                    string processName = System.IO.Path.GetFileName(app.AppPath ?? "");
                    string displayName = app.AppName ?? "";
                    string iconBase64 = "";
                    string version = "";
                    string company = "";

                    var b = !string.IsNullOrEmpty(app.AppId);
                    var isNullOrEmpty = !string.IsNullOrEmpty(displayName);
                    if (isNullOrEmpty && b)
                    {
                        // 如果JOIN没有获取到应用名称，尝试单独查询
                        var appInfo = await DatabaseManager.Instance.AppInfos.GetAppByAppIdAsync(app.AppId);
                        if (appInfo != null)
                        {
                            displayName = appInfo.Name ?? "";
                            iconBase64 = appInfo.Base64Icon ?? "";
                            version = appInfo.Version ?? "";
                            company = appInfo.Company ?? "";
                        }
                    }
                    
                    if (isNullOrEmpty)
                    {
                        displayName = processName.Replace(".exe", "") ?? "Unknown";
                    }

                    items.Add(new
                    {
                        rank = rank++,
                        processName = processName,
                        displayName = displayName,
                        processPath = app.AppPath ?? "",
                        appId = app.AppId ?? "",
                        icon = iconBase64,
                        version = version,
                        company = company,
                        totalBytes = app.TotalTraffic,
                        uploadBytes = app.TotalUpload,
                        connectionCount = app.ConnectionCount
                    });
                }

                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = true,
                    Data = new
                    {
                        total = totalCount,
                        page = page,
                        pageSize = pageSize,
                        items = items
                    },
                    Message = "获取软件流量排行成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"获取软件流量排行失败: {ex.Message}"
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

        #region 应用详细网络分析API

        // 获取应用详细网络分析数据
        server.Get("/api/apps/network-analysis", async (context) =>
        {
            try
            {
                var appId = context.QueryParams["appId"];
                var timeRange = context.QueryParams["timeRange"] ?? "1day";
                
                if (string.IsNullOrEmpty(appId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteJsonAsync(new ResponseModel<object>
                    {
                        Success = false,
                        Message = "缺少必需参数 appId"
                    });
                    return;
                }

                // 计算时间范围
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long startTime = timeRange switch
                {
                    "1hour" => DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(),
                    "1day" => DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(),
                    "7days" => DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds(),
                    "30days" => DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds(),
                    _ => DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds()
                };

                // 获取应用基本信息
                var appInfo = await DatabaseManager.Instance.AppInfos.GetAppByAppIdAsync(appId);
                
                // 获取网络数据
                var networkData = await DatabaseManager.Instance.Networks.GetAppNetworkByTimeRangeAsync(appId, startTime, now);
                var networkList = networkData.ToList();

                // 计算基本统计
                var totalUpload = networkList.Sum(x => x.UploadBytes);
                var totalDownload = networkList.Sum(x => x.DownloadBytes);
                var totalConnections = networkList.Count;

                // 获取前30个流量最高的连接
                var topConnections = networkList
                    .Where(x => x.UploadBytes + x.DownloadBytes > 0) // 只统计有流量的连接
                    .GroupBy(x => new { x.LocalIP, x.LocalPort, x.RemoteIP, x.RemotePort, x.Protocol })
                    .Select(g => new
                    {
                        localIP = g.Key.LocalIP,
                        localPort = g.Key.LocalPort,
                        remoteIP = g.Key.RemoteIP,
                        remotePort = g.Key.RemotePort,
                        protocol = g.Key.Protocol,
                        totalUpload = g.Sum(x => x.UploadBytes),
                        totalDownload = g.Sum(x => x.DownloadBytes),
                        totalTraffic = g.Sum(x => x.UploadBytes + x.DownloadBytes),
                        connectionCount = g.Count(),
                        firstSeen = DateTimeOffset.FromUnixTimeSeconds(g.Min(x => x.Timestamp)).ToString("yyyy-MM-dd HH:mm:ss"),
                        lastSeen = DateTimeOffset.FromUnixTimeSeconds(g.Max(x => x.Timestamp)).ToString("yyyy-MM-dd HH:mm:ss")
                    })
                    .OrderByDescending(x => x.totalTraffic)
                    .Take(30)
                    .ToList();

                // 计算协议占比
                var protocolStats = networkList
                    .Where(x => x.UploadBytes + x.DownloadBytes > 0)
                    .GroupBy(x => x.Protocol.ToUpper())
                    .Select(g => new
                    {
                        protocol = g.Key,
                        connectionCount = g.Count(),
                        totalTraffic = g.Sum(x => x.UploadBytes + x.DownloadBytes),
                        percentage = Math.Round((double)g.Count() / networkList.Where(x => x.UploadBytes + x.DownloadBytes > 0).Count() * 100, 2)
                    })
                    .OrderByDescending(x => x.totalTraffic)
                    .ToList();

                // 获取时间趋势数据（按小时分组）
                var timeTrends = networkList
                    .GroupBy(x => x.Timestamp / 3600 * 3600) // 按小时分组
                    .Select(g => new
                    {
                        timestamp = g.Key,
                        timeStr = DateTimeOffset.FromUnixTimeSeconds(g.Key).ToString("yyyy-MM-dd HH:mm"),
                        upload = g.Sum(x => x.UploadBytes),
                        download = g.Sum(x => x.DownloadBytes),
                        connections = g.Count()
                    })
                    .OrderBy(x => x.timestamp)
                    .ToList();

                // 端口分析
                var portAnalysis = networkList
                    .Where(x => x.UploadBytes + x.DownloadBytes > 0)
                    .GroupBy(x => x.RemotePort)
                    .Select(g => new
                    {
                        port = g.Key,
                        serviceName = GetPortServiceName(g.Key),
                        connectionCount = g.Count(),
                        totalTraffic = g.Sum(x => x.UploadBytes + x.DownloadBytes),
                        protocols = g.Select(x => x.Protocol).Distinct().ToList()
                    })
                    .OrderByDescending(x => x.totalTraffic)
                    .Take(20)
                    .ToList();

                // 构建返回数据
                var result = new
                {
                    // 应用基本信息
                    appInfo = new
                    {
                        appId = appId,
                        name = appInfo?.Name ?? "Unknown Application",
                        company = appInfo?.Company ?? "",
                        version = appInfo?.Version ?? "",
                        path = appInfo?.Path ?? "",
                        icon = appInfo?.Base64Icon ?? "",
                        hash = appInfo?.Hash ?? ""
                    },
                    
                    // 统计摘要
                    summary = new
                    {
                        timeRange = timeRange,
                        startTime = DateTimeOffset.FromUnixTimeSeconds(startTime).ToString("yyyy-MM-dd HH:mm:ss"),
                        endTime = DateTimeOffset.FromUnixTimeSeconds(now).ToString("yyyy-MM-dd HH:mm:ss"),
                        totalUpload = totalUpload,
                        totalDownload = totalDownload,
                        totalTraffic = totalUpload + totalDownload,
                        totalConnections = totalConnections,
                        uniqueRemoteIPs = networkList.Select(x => x.RemoteIP).Distinct().Count(),
                        uniqueRemotePorts = networkList.Select(x => x.RemotePort).Distinct().Count()
                    },
                    
                    // 前30个流量最高的连接
                    topConnections = topConnections,
                    
                    // 协议占比统计
                    protocolStats = protocolStats,
                    
                    // 时间趋势
                    timeTrends = timeTrends,
                    
                    // 端口分析
                    portAnalysis = portAnalysis
                };

                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = true,
                    Data = result,
                    Message = "获取应用网络分析数据成功"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteJsonAsync(new ResponseModel<object>
                {
                    Success = false,
                    Message = $"获取应用网络分析数据失败: {ex.Message}"
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

    // 辅助方法：获取所有网卡的小时数据
    private static async Task<List<GlobalNetworkAggregatedBase>> GetAllNetworkCardsHourlyDataAsync(int hours)
    {
        var result = new List<GlobalNetworkAggregatedBase>();
        var startTime = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeSeconds();
        
        // 使用现有的方法来获取所有网卡的小时数据
        // 先获取所有存在的网卡GUID
        try 
        {
            var allHourlyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync("", hours);
            var networkGuids = allHourlyData.Select(x => "").Distinct(); // 这里需要从实际数据中提取
            
            foreach (var guid in networkGuids)
            {
                if (!string.IsNullOrEmpty(guid))
                    result.Add(new GlobalNetworkHourly { NetworkCardGuid = guid });
            }
        }
        catch
        {
            // 如果出错，返回空列表
        }
        
        return result;
    }

    // 辅助方法：获取所有网卡的日数据  
    private static async Task<List<GlobalNetworkAggregatedBase>> GetAllNetworkCardsDailyDataAsync(int days)
    {
        var result = new List<GlobalNetworkAggregatedBase>();
        
        try 
        {
            var allDailyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync("", days);
            var networkGuids = allDailyData.Select(x => "").Distinct(); // 这里需要从实际数据中提取
            
            foreach (var guid in networkGuids)
            {
                if (!string.IsNullOrEmpty(guid))
                    result.Add(new GlobalNetworkDaily { NetworkCardGuid = guid });
            }
        }
        catch
        {
            // 如果出错，返回空列表
        }
        
        return result;
    }

    // 辅助方法：检查指定时间范围内是否有网络数据
    private static async Task<bool> HasNetworkDataInTimeRangeAsync(string dataType, long startTime)
    {
        try
        {
            // 使用现有方法检查数据
            switch (dataType)
            {
                case "hourly":
                    var hourlyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync("", 1);
                    return hourlyData.Any();
                case "daily":  
                    var dailyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync("", 1);
                    return dailyData.Any();
                default:
                    var networkData = await DatabaseManager.Instance.Networks.GetGlobalNetworkHistoryAsync("", 1);
                    return networkData.Any();
            }
        }
        catch
        {
            return false;
        }
    }

    // 辅助方法：获取流量趋势数据
    private static async Task<List<object>> GetTrafficTrendsAsync(string timeRange, string interfaceId, long startTime, long endTime)
    {
        var points = new List<object>();
        
        try
        {
            if (timeRange == "hour")
            {
                // 从 GlobalNetwork 表查询
                if (interfaceId == "all")
                {
                    // 获取所有网卡数据并聚合
                    var allData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByTimeRangeAsync("", startTime, endTime);
                    var groupedData = allData.GroupBy(d => d.Timestep).Select(g => new
                    {
                        timestamp = g.Key.ToString(),
                        uploadSpeed = g.Sum(x => x.Upload),
                        downloadSpeed = g.Sum(x => x.Download)
                    });
                    
                    points.AddRange(groupedData);
                }
                else
                {
                    var data = await DatabaseManager.Instance.Networks.GetGlobalNetworkByTimeRangeAsync(interfaceId, startTime, endTime);
                    points.AddRange(data.Select(d => new
                    {
                        timestamp = d.Timestep.ToString(),
                        uploadSpeed = d.Upload,
                        downloadSpeed = d.Download
                    }));
                }
            }
            else if (timeRange == "day")
            {
                // 从小时数据聚合
                if (interfaceId == "all")
                {
                    var allData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync("", 24);
                    var groupedData = allData.GroupBy(d => d.HourTimestamp).Select(g => new
                    {
                        timestamp = g.Key.ToString(),
                        uploadSpeed = g.Sum(x => x.AvgUpload),
                        downloadSpeed = g.Sum(x => x.AvgDownload)
                    });
                    
                    points.AddRange(groupedData);
                }
                else
                {
                    var data = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync(interfaceId, 24);
                    points.AddRange(data.Select(d => new
                    {
                        timestamp = d.HourTimestamp.ToString(),
                        uploadSpeed = d.AvgUpload,
                        downloadSpeed = d.AvgDownload
                    }));
                }
            }
            else
            {
                // 从日数据聚合
                int days = timeRange == "week" ? 7 : 30;
                if (interfaceId == "all")
                {
                    var allData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync("", days);
                    var groupedData = allData.GroupBy(d => d.DayTimestamp).Select(g => new
                    {
                        timestamp = g.Key.ToString(),
                        uploadSpeed = g.Sum(x => x.AvgUpload),
                        downloadSpeed = g.Sum(x => x.AvgDownload)
                    });
                    
                    points.AddRange(groupedData);
                }
                else
                {
                    var data = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync(interfaceId, days);
                    points.AddRange(data.Select(d => new
                    {
                        timestamp = d.DayTimestamp.ToString(),
                        uploadSpeed = d.AvgUpload,
                        downloadSpeed = d.AvgDownload
                    }));
                }
            }
        }
        catch
        {
            // 如果出错，返回空数据
        }
        
        return points;
    }

    /// <summary>
    /// 根据端口号获取服务名称
    /// </summary>
    /// <param name="port">端口号</param>
    /// <returns>服务名称</returns>
    private static string GetPortServiceName(int port)
    {
        return port switch
        {
            21 => "FTP",
            22 => "SSH",
            23 => "Telnet",
            25 => "SMTP",
            53 => "DNS",
            80 => "HTTP",
            110 => "POP3",
            123 => "NTP",
            143 => "IMAP",
            443 => "HTTPS",
            993 => "IMAPS",
            995 => "POP3S",
            1433 => "SQL Server",
            3306 => "MySQL",
            3389 => "RDP",
            5432 => "PostgreSQL",
            6379 => "Redis",
            8080 => "HTTP-Alt",
            8443 => "HTTPS-Alt",
            _ when port >= 1024 && port <= 5000 => "User Port",
            _ when port > 5000 && port < 32768 => "Service Port", 
            _ when port >= 32768 => "Dynamic Port",
            _ => "Unknown"
        };
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
