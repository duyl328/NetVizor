namespace Common.ExpandException;

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
