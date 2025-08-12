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
        // todo: 2025/6/24 15:53 开发过程中使用固定端口
        // return 8267;

        TcpListener listener = new TcpListener(IPAddress.Loopback, 0); // 端口号为0表示由系统自动分配
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public static bool IsAdministrator()
    {
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}