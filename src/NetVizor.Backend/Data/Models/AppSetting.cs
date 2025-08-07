namespace Data.Models;

/// <summary>
/// 应用程序设置
/// </summary>
public class AppSetting
{
    public int Id { get; set; }

    /// <summary>
    /// 窗口X坐标
    /// </summary>
    public int WindowX { get; set; } = 0;

    /// <summary>
    /// 窗口Y坐标
    /// </summary>
    public int WindowY { get; set; } = 0;

    /// <summary>
    /// 点击穿透功能
    /// </summary>
    public bool IsClickThrough { get; set; } = false;

    /// <summary>
    /// 位置锁定
    /// </summary>
    public bool IsPositionLocked { get; set; } = false;

    /// <summary>
    /// 对齐到屏幕
    /// </summary>
    public bool SnapToScreen { get; set; } = false;

    /// <summary>
    /// 显示详细信息
    /// </summary>
    public bool ShowDetailedInfo { get; set; } = false;

    /// <summary>
    /// 窗口置顶
    /// </summary>
    public bool IsTopmost { get; set; } = false;

    /// <summary>
    /// 文本颜色
    /// </summary>
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// 背景颜色
    /// </summary>
    public string BackgroundColor { get; set; } = "#000000";

    /// <summary>
    /// 透明度 (0-100)
    /// </summary>
    public int Opacity { get; set; } = 100;

    /// <summary>
    /// 速度单位 (0: bps, 1: KB/s, 2: MB/s)
    /// </summary>
    public int SpeedUnit { get; set; } = 1;

    /// <summary>
    /// 布局方向 (0: 横向, 1: 纵向)
    /// </summary>
    public int LayoutDirection { get; set; } = 0;

    /// <summary>
    /// 展示单位
    /// </summary>
    public bool ShowUnit { get; set; } = true;

    /// <summary>
    /// 双击动作 (0: None, 1: TrafficAnalysis, 2: Settings)
    /// </summary>
    public int DoubleClickAction { get; set; } = 0;

    /// <summary>
    /// 默认以管理员身份启动
    /// </summary>
    public bool RunAsAdmin { get; set; } = false;

    /// <summary>
    /// 是否开机自启
    /// </summary>
    public bool AutoStart { get; set; } = false;


    /// <summary>
    /// 更新时间
    /// </summary>
    public long UpdateTime { get; set; }
}