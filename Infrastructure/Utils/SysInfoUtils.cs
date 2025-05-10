using System.Text.Json;

namespace Infrastructure.utils;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public static class SysInfoUtils
{
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

    public static void InspectProcess(int pid)
    {
        try
        {
            var proc = Process.GetProcessById(pid);

            Console.WriteLine($"进程名称：{proc.ProcessName}");
            Console.WriteLine($"进程ID：{proc.Id}");
            Console.WriteLine($"启动时间：{proc.StartTime}");
            Console.WriteLine($"是否退出：{proc.HasExited}");
            Console.WriteLine($"退出时间：{proc.ExitTime}");
            Console.WriteLine($"退出代码：{proc.ExitCode}");
            Console.WriteLine($"占用内存：{FormatBytes(proc.WorkingSet64)}");
            Console.WriteLine($"线程数：{proc.Threads.Count}");
            Console.WriteLine($"主模块路径：{proc.MainModule?.FileName}");
            Console.WriteLine($"启动文件名：{proc.MainModule?.ModuleName}");
            var serialize = JsonSerializer.Serialize(proc.MainModule);
            Console.WriteLine($"YYY：{serialize}");
            var filePath = proc.MainModule?.FileName;
            if (filePath != null)
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(filePath);

                Console.WriteLine($"文件描述：{versionInfo.FileDescription}");
                Console.WriteLine($"产品名称：{versionInfo.ProductName}");
                Console.WriteLine($"公司名称：{versionInfo.CompanyName}");
                Console.WriteLine($"版本号：{versionInfo.FileVersion}");
                Console.WriteLine($"版权：{versionInfo.LegalCopyright}");
                var serialize1 = JsonSerializer.Serialize(versionInfo);
                
                Console.WriteLine($"XXX：{serialize1}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取进程信息失败：{ex.Message}");
        }
    }
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
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
