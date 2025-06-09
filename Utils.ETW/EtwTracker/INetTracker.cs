using Utils.ETW.Core;

namespace Utils.ETW.EtwTracker;

public interface INetTracker
{
    /// <summary>
    /// 设置处理方式
    /// </summary>
    /// <param name="networkCapture">ETW 连接对象</param>
    void SetupEtwHandlers(EtwNetworkCapture networkCapture);
}
