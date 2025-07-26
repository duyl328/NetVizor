using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using System.Windows.Media;

namespace Shell.UserControls;

public partial class TrayMenuWindow : Window
{
    public event Action OnShowMainWindow;
    public event Action OnExitApplication;
    public event Action OnHideToTray;

    // 菜单的固定尺寸（与XAML中设置的一致）
    private const double MENU_WIDTH = 180;
    private const double MENU_HEIGHT = 200;

    public TrayMenuWindow()
    {
        InitializeComponent();
        this.Loaded += TrayMenuWindow_Loaded;

        // 确保窗口使用Per-Monitor DPI Awareness
        this.SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var source = HwndSource.FromHwnd(hwnd);
            if (source != null)
            {
                source.AddHook(WndProc);
            }
        };
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // 处理DPI变化消息
        const int WM_DPICHANGED = 0x02E0;
        if (msg == WM_DPICHANGED)
        {
            // DPI发生变化时可以在这里处理
            Console.WriteLine("DPI changed detected");
        }

        return IntPtr.Zero;
    }

    private void TrayMenuWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 添加淡入动画
        // var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
        // this.BeginAnimation(OpacityProperty, fadeIn);

        // 添加缩放动画
        // var scaleTransform = new System.Windows.Media.ScaleTransform(0.8, 0.8);
        // this.RenderTransform = scaleTransform;
        // this.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

        // var scaleXAnimation = new DoubleAnimation(0.8, 1, TimeSpan.FromMilliseconds(200));
        // var scaleYAnimation = new DoubleAnimation(0.8, 1, TimeSpan.FromMilliseconds(200));

        // scaleXAnimation.EasingFunction = new QuadraticEase();
        // scaleYAnimation.EasingFunction = new QuadraticEase();

        // scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleXAnimation);
        // scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleYAnimation);
    }

    public void ShowAt(int x, int y)
    {
        // 使用主显示器工作区域的旧方法（保留向后兼容）
        var workArea = SystemParameters.WorkArea;
        var customWorkArea = new Shell.Utils.TrayIconManager.RECT
        {
            Left = (int)workArea.Left,
            Top = (int)workArea.Top,
            Right = (int)workArea.Right,
            Bottom = (int)workArea.Bottom
        };
        ShowAtWithWorkArea(x, y, customWorkArea);
    }

    public void ShowAtWithWorkArea(int x, int y, Shell.Utils.TrayIconManager.RECT workArea)
    {
        Console.WriteLine("=== TrayMenu ShowAtWithWorkArea Debug ===");
        Console.WriteLine($"输入鼠标位置: X={x}, Y={y}");
        Console.WriteLine(
            $"使用工作区域: Left={workArea.Left}, Top={workArea.Top}, Right={workArea.Right}, Bottom={workArea.Bottom}");
        Console.WriteLine($"工作区域尺寸: Width={workArea.Right - workArea.Left}, Height={workArea.Bottom - workArea.Top}");
        Console.WriteLine($"菜单尺寸: Width={MENU_WIDTH}, Height={MENU_HEIGHT}");

        // 获取当前窗口的DPI缩放信息
        var dpiScale = GetDpiScale();
        Console.WriteLine($"窗口DPI缩放: X={dpiScale.DpiScaleX:F2}, Y={dpiScale.DpiScaleY:F2}");

        // 计算菜单显示位置 - 优先显示在鼠标左上方
        double left = x - MENU_WIDTH;
        double top = y - MENU_HEIGHT;

        Console.WriteLine($"初始计算位置 (左上方): Left={left}, Top={top}");

        // 智能位置调整算法
        var adjustedPosition = CalculateOptimalPosition(x, y, workArea, MENU_WIDTH, MENU_HEIGHT);
        left = adjustedPosition.X;
        top = adjustedPosition.Y;

        Console.WriteLine($"智能调整后位置: Left={left}, Top={top}");

        // 最终边界检查（确保完全在工作区域内）
        left = Math.Max(workArea.Left, Math.Min(left, workArea.Right - MENU_WIDTH));
        top = Math.Max(workArea.Top, Math.Min(top, workArea.Bottom - MENU_HEIGHT));

        Console.WriteLine($"最终边界检查后位置: Left={left}, Top={top}");

        // 设置窗口位置
        this.Left = left;
        this.Top = top;

        // 显示窗口
        this.Show();
        this.Activate();
        this.Focus();

        // 验证实际设置的位置
        Console.WriteLine($"窗口实际位置: Left={this.Left}, Top={this.Top}");
        Console.WriteLine($"窗口实际尺寸: Width={this.ActualWidth}, Height={this.ActualHeight}");
        Console.WriteLine("=== Debug End ===\n");
    }

    private (double X, double Y) CalculateOptimalPosition(int mouseX, int mouseY,
        Shell.Utils.TrayIconManager.RECT workArea, double menuWidth, double menuHeight)
    {
        double left, top;

        // 定义四个可能的位置：左上、右上、左下、右下
        var positions = new[]
        {
            new
            {
                X = (double)(mouseX - menuWidth), Y = (double)(mouseY - menuHeight), Priority = 1, Name = "左上"
            }, // 左上 (优先)
            new { X = (double)mouseX, Y = (double)(mouseY - menuHeight), Priority = 2, Name = "右上" }, // 右上
            new { X = (double)(mouseX - menuWidth), Y = (double)mouseY, Priority = 3, Name = "左下" }, // 左下
            new { X = (double)mouseX, Y = (double)mouseY, Priority = 4, Name = "右下" } // 右下
        };

        // 检查每个位置是否完全在工作区域内
        foreach (var pos in positions.OrderBy(p => p.Priority))
        {
            bool fitsHorizontally = pos.X >= workArea.Left && pos.X + menuWidth <= workArea.Right;
            bool fitsVertically = pos.Y >= workArea.Top && pos.Y + menuHeight <= workArea.Bottom;

            Console.WriteLine(
                $"检查位置 {pos.Name}: X={pos.X:F1}, Y={pos.Y:F1}, 水平适合={fitsHorizontally}, 垂直适合={fitsVertically}");

            if (fitsHorizontally && fitsVertically)
            {
                Console.WriteLine($"选择位置: {pos.Name}");
                return (pos.X, pos.Y);
            }
        }

        // 如果没有位置完全适合，使用智能调整
        Console.WriteLine("没有完全适合的位置，使用智能调整");

        // 计算最佳的水平位置
        if (mouseX - menuWidth >= workArea.Left)
        {
            left = mouseX - menuWidth; // 左侧有足够空间
        }
        else if (mouseX + menuWidth <= workArea.Right)
        {
            left = mouseX; // 右侧有足够空间
        }
        else
        {
            // 两侧都不够，居中显示或贴边
            left = Math.Max(workArea.Left, workArea.Right - menuWidth);
        }

        // 计算最佳的垂直位置
        if (mouseY - menuHeight >= workArea.Top)
        {
            top = mouseY - menuHeight; // 上方有足够空间
        }
        else if (mouseY + menuHeight <= workArea.Bottom)
        {
            top = mouseY; // 下方有足够空间
        }
        else
        {
            // 上下都不够，贴顶部或底部
            top = Math.Max(workArea.Top, workArea.Bottom - menuHeight);
        }

        Console.WriteLine($"智能调整结果: Left={left:F1}, Top={top:F1}");
        return (left, top);
    }

    private System.Windows.DpiScale GetDpiScale()
    {
        try
        {
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                return VisualTreeHelper.GetDpi(this);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取DPI缩放失败: {ex.Message}");
        }

        // 返回默认缩放
        return new System.Windows.DpiScale(1.0, 1.0);
    }

    private void ShowMainWindow_Click(object sender, MouseButtonEventArgs e)
    {
        OnShowMainWindow?.Invoke();
        this.Hide();
    }

    private void HideToTray_Click(object sender, MouseButtonEventArgs e)
    {
        OnHideToTray?.Invoke();
        this.Hide();
    }

    private void About_Click(object sender, MouseButtonEventArgs e)
    {
        System.Windows.MessageBox.Show("这是一个带有系统托盘功能的WPF应用程序。", "关于",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        this.Hide();
    }

    private void ExitApplication_Click(object sender, MouseButtonEventArgs e)
    {
        var result = System.Windows.MessageBox.Show("确定要退出程序吗？", "确认退出",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            OnExitApplication?.Invoke();
        }

        this.Hide();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // 当窗口失去焦点时自动隐藏
        this.Hide();
    }
}