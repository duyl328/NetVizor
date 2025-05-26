using System.Configuration;
using System.Data;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using Application.Utils;
using Microsoft.Extensions.Logging;

namespace Shell;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 注册全局异常处理
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        base.OnStartup(e);

        // ✅ 启动你的服务（如 WebSocket、HTTP、端口监听等）
        StartMyServer();

        // 启动主窗口
        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();
    }
    
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception, "UI线程异常");
        
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
            LogException(ex, "Task未观察异常");
        }
    
        e.SetObserved(); // 标记异常已被观察，防止应用终止
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        LogException(exception, "非UI线程异常");
    
        if (e.IsTerminating)
        {
            // 应用即将终止，进行紧急清理
            // EmergencyCleanup();
            // LogCritical("应用程序即将因未处理异常而终止");
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

    private void LogException(Exception exception, string context)
    {
        // var logLevel = GetLogLevel(exception);
        // var logMessage = $"[{context}] {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";
        //
        // // 使用日志框架记录
        // Logger<>.Log(logLevel, logMessage);
        //
        // // 严重异常时发送邮件或其他通知
        // if (logLevel == LogLevel.Critical)
        // {
        //     NotifyDevelopers(exception, context);
        // }
    }
    // 对于async/await操作，确保正确处理异常
    public async Task<T> SafeExecuteAsync<T>(Func<Task<T>> operation, T defaultValue = default)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            LogException(ex, "异步操作异常");
            return defaultValue;
        }
    }

// 对于Fire-and-Forget的Task，确保异常被捕获
    public void SafeFireAndForget(Func<Task> operation)
    {
        Task.Run(async () =>
        {
            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                LogException(ex, "后台任务异常");
            }
        });
    }
    private void StartMyServer()
    {
        int port = SysHelper.GetAvailablePort();
        Console.WriteLine($"服务启动在端口: {port}");

        // 例如开启线程或任务监听 WS：
        Task.Run(() =>
        {
            StartWebSocketServer(port);
        });
    }


    private void StartWebSocketServer(int port)
    {
        // 示例服务逻辑，如上文 WebSocket 实现
    }

}
