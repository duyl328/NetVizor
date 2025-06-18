using System.Windows;

namespace UI.Shared.Wpf;

public static class GlobalExceptionHandler
{
    public static void RegisterGlobalExceptionHandlers(System.Windows.Application app)
    {
        // UI线程异常处理
        app.DispatcherUnhandledException += (sender, e) =>
        {
            HandleException(e.Exception, "UI线程异常");
            e.Handled = true;
        };

        // 非UI线程异常处理
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                HandleException(ex, "非UI线程异常");
        };

        // 任务未观察到的异常处理
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            HandleException(e.Exception, "任务未观察异常");
            e.SetObserved();
        };
    }

    private static void HandleException(Exception ex, string context)
    {
        // TODO: 替换为你自己的日志系统或上报服务
        string message = $"【{context}】{ex.Message}\n{ex.StackTrace}";
        MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        // 例如 LogHelper.LogError(ex);
    }
}
