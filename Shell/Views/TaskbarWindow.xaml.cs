using System.Runtime.InteropServices;
using System.Windows;
using Shell.Utils;

namespace Shell.Views;

public partial class TaskbarWindow : Window
{
    public TaskbarWindow()
    {
        InitializeComponent();
        // 获取任务栏位置
        var rect = TaskbarHelper.GetTaskbarPosition();

        // 将窗口贴在任务栏右下角
        this.Left = rect.right - this.Width;
        this.Top = rect.bottom - this.Height;
    }
    
}
