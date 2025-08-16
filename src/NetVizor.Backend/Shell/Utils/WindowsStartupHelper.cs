using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using Common.Logger;

namespace Shell.Utils;

/// <summary>
/// Windows开机自启动辅助类
/// </summary>
public static class WindowsStartupHelper
{
    private const string REGISTRY_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string APP_NAME = "NetVizor";

    /// <summary>
    /// 设置开机自启动
    /// </summary>
    /// <param name="enable">是否启用自启动</param>
    /// <returns>操作是否成功</returns>
    public static bool SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true);
            if (key == null)
            {
                Log.Error4Ctx("无法打开注册表项");
                return false;
            }

            if (enable)
            {
                // 获取当前执行文件的路径
                var executablePath = Assembly.GetExecutingAssembly().Location;
                
                // 如果是.NET Core/.NET 5+应用，使用exe文件路径
                if (executablePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    executablePath = Path.ChangeExtension(executablePath, ".exe");
                }

                // 如果exe文件不存在，尝试从当前目录查找
                if (!File.Exists(executablePath))
                {
                    var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var exeName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) + ".exe";
                    executablePath = Path.Combine(currentDir ?? "", exeName);
                }

                if (!File.Exists(executablePath))
                {
                    Log.Error4Ctx($"找不到可执行文件: {executablePath}");
                    return false;
                }

                // 添加自启动项
                key.SetValue(APP_NAME, $"\"{executablePath}\"");
                Log.Info($"已设置开机自启动: {executablePath}");
            }
            else
            {
                // 删除自启动项
                if (key.GetValue(APP_NAME) != null)
                {
                    key.DeleteValue(APP_NAME);
                    Log.Info("已取消开机自启动");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error4Ctx($"设置开机自启动失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 检查是否已设置开机自启动
    /// </summary>
    /// <returns>是否已设置自启动</returns>
    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, false);
            if (key == null)
            {
                return false;
            }

            var value = key.GetValue(APP_NAME)?.ToString();
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            Log.Warning($"检查开机自启动状态失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取当前设置的启动路径
    /// </summary>
    /// <returns>启动路径，如果未设置则返回null</returns>
    public static string? GetStartupPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, false);
            if (key == null)
            {
                return null;
            }

            var value = key.GetValue(APP_NAME)?.ToString();
            return value?.Trim('"'); // 移除引号
        }
        catch (Exception ex)
        {
            Log.Warning($"获取开机自启动路径失败: {ex.Message}");
            return null;
        }
    }
}