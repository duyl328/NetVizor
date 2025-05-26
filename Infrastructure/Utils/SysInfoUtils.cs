using Common;

namespace Infrastructure.utils;

using Infrastructure.GlobalCaches;
using Infrastructure.Models;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

public static class SysInfoUtils
{
    /// <summary>
    /// 程序自身信息
    /// </summary>
    /// <returns></returns>
    public static string GetProgramDiagnostics()
    {
        var sb = new StringBuilder();

        // === 程序自身信息 ===
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        sb.AppendLine("=== 程序信息 ===");
        sb.AppendLine($"程序集名称：{asm.GetName().Name}");
        sb.AppendLine($"版本号：{asm.GetName().Version}");
        sb.AppendLine($"路径：{asm.Location}");
        sb.AppendLine($"启动目录：{AppDomain.CurrentDomain.BaseDirectory}");
        sb.AppendLine($"命令行参数：{string.Join(" ", Environment.GetCommandLineArgs())}");
        sb.AppendLine();

        // === 进程运行信息 ===
        var proc = Process.GetCurrentProcess();
        sb.AppendLine("=== 进程状态 ===");
        sb.AppendLine($"进程 ID：{proc.Id}");
        sb.AppendLine($"进程名称：{proc.ProcessName}");
        sb.AppendLine($"启动时间：{proc.StartTime}");
        sb.AppendLine($"运行时长：{DateTime.Now - proc.StartTime}");
        sb.AppendLine($"线程数：{proc.Threads.Count}");
        sb.AppendLine($"内存占用：{FormatBytes(proc.WorkingSet64)}");
        sb.AppendLine();

        // === 系统环境信息 ===
        sb.AppendLine("=== 系统环境 ===");
        sb.AppendLine($"操作系统：{RuntimeInformation.OSDescription}");
        sb.AppendLine($"架构：{(Environment.Is64BitOperatingSystem ? "64位" : "32位")}");
        sb.AppendLine($"CPU 核心数：{Environment.ProcessorCount}");
        sb.AppendLine($"机器名：{Environment.MachineName}");
        sb.AppendLine($"当前用户：{Environment.UserName}");
        sb.AppendLine();

        return sb.ToString();
    }


    /// <summary>
    /// 获取指定进程的信息
    /// </summary>
    /// <param name="pid"></param>
    /// <returns></returns>
    /// <exception cref="ExceptionExpand"></exception>
    public static ProgramInfo InspectProcess(int pid)
    {
        try
        {
            var proc = Process.GetProcessById(pid);

            #region 检查缓存

            var pck = new ProcessCache.ProcessCacheKey(pid, proc.StartTime);
            var programInfo = ProcessCache.Instance.Get(pck);
            if (programInfo != null)
            {
                return programInfo;
            }

            #endregion

            #region 获取进程信息

            var exeInfo = new ProgramInfo
            {
                ProcessName = proc.ProcessName,
                ProcessId = proc.Id,
                StartTime = proc.StartTime,
                HasExited = proc.HasExited,
                ExitTime = proc.ExitTime,
                ExitCode = proc.ExitCode,
                UseMemory = proc.WorkingSet64,
                ThreadCount = proc.Threads.Count,
                MainModulePath = proc.MainModule?.FileName,
                MainModuleName = proc.MainModule?.ModuleName
            };

            // MainModule 是一个涉及 native 调用的属性，不建议频繁调用，尤其在枚举大量进程时可能影响性能。

            if (exeInfo.MainModulePath != null)
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(exeInfo.MainModulePath);

                exeInfo.FileDescription = versionInfo.FileDescription;
                exeInfo.ProductName = versionInfo.ProductName;
                exeInfo.CompanyName = versionInfo.CompanyName;
                exeInfo.Version = versionInfo.FileVersion;
                exeInfo.LegalCopyright = versionInfo.LegalCopyright;
            }

            // 获取软件 Icon 【不一定存在】
            var iconBase64 = IconHelper.GetIconBase64(exeInfo.MainModulePath);
            exeInfo.IconBase64 = iconBase64;

            #endregion

            ProcessCache.Instance.Set(pck, exeInfo);
            return exeInfo;
        }
        // 只捕获能遇见的异常
        catch (ArgumentException ex)
        {
            Console.WriteLine($"获取进程信息失败：{ex.Message}");
            throw ExceptionEnum.ProcessGetException;
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
