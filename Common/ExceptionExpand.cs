namespace Common;

public class ExceptionExpand : Exception
{
    /// <summary>
    /// 默认异常编码
    /// </summary>
    private const long DefaultCode = -1;

    public string ErrorMessage { get; private set; } = "";
    public long ErrorCode { get; private set; } = DefaultCode;
    public string Suggestion { get; private set; } = "";

    private ExceptionExpand()
    {
    }

    public ExceptionExpand(long errorCode, string massage, string solution) : base(
        massage)
    {
        ErrorCode = errorCode;
        Suggestion = solution;
    }

    public ExceptionExpand(long errorCode, string massage, string solution, Exception? innerException) : base(
        massage, innerException)
    {
        ErrorCode = errorCode;
        Suggestion = solution;
    }

    public override string ToString()
    {
        return $"Error Code: {ErrorCode}\nMessage: {Message}\nSuggestion: {Suggestion}\n{base.ToString()}";
    }
}

public class ExceptionEnum
{
    // region 系统常见错误本地化

    /// <summary>
    ///     进程信息获取失败
    /// </summary>
    public static readonly ExceptionExpand ProcessGetException = new(100001L, "进程消息获取失败,指定线程可能不存在或已关闭!", "以管理员权限重试!");
    
    
    // endregion
}
