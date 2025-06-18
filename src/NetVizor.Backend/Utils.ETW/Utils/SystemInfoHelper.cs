namespace Utils.ETW.Utils;

public static class SystemInfoHelper
{
    /// <summary>
    /// 检查是否是管理员权限
    /// </summary>
    /// <returns></returns>
    public static bool IsRunAsAdministrator()
    {
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

}
