using System.ComponentModel;
using System.Windows;
using Shell.Utils;

namespace Shell.Views;

public partial class NetView : Window
{
    private TrayIconManager _trayIconManager;

    public NetView()
    {
        InitializeComponent();
        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        _trayIconManager = new TrayIconManager(this);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // 阻止窗口关闭，而是隐藏到托盘
        e.Cancel = true;
        this.Hide();
    }

    protected override void OnClosed(EventArgs e)
    {
        _trayIconManager?.Dispose();
        base.OnClosed(e);
    }

    // 如果你想要真正关闭程序的方法
    public void ForceClose()
    {
        _trayIconManager?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}