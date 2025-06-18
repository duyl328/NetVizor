namespace Common.ExpandException;

public class ExceptionEnum
{
    // region 系统常见错误本地化

    /// <summary>
    ///     进程信息获取失败
    /// </summary>
    public static readonly ExceptionExpand ProcessGetException = new(100001L, "进程消息获取失败,指定线程可能不存在或已关闭!", "以管理员权限重试!");
    
    
    // endregion
}