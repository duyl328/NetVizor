using System.Net;
using System.Net.Sockets;

namespace Application.Utils;

/// <summary>
/// 
/// </summary>
public static class SysHelper
{
    /// <summary>
    /// 获取可用端口
    /// </summary>
    /// <returns></returns>
    public static int GetAvailablePort()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0); // 端口号为0表示由系统自动分配
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
