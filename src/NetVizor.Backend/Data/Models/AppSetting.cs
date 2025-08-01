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

/*
 *文本颜色
背景颜色
透明度
速度单位
布局方向（横向，纵向）
展示单位（true/false)
双击动作（1，2，3，）
默认以管理员身份启动(true/false)
 *
 */

    /// <summary>
    /// 是否开机自启
    /// </summary>
    public bool AutoStart { get; set; } = false;


    /// <summary>
    /// 更新时间
    /// </summary>
    public long UpdateTime { get; set; }
}