using System.IO;
using Common;
using Common.ExpandException;
using Common.Logger;

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
    /// <summary>
    /// 获取指定进程的信息
    /// </summary>
    /// <param name="pid"></param>
    /// <returns></returns>
    /// <exception cref="ExceptionExpand"></exception>
    public static ProgramInfo? InspectProcess(int pid)
    {
        try
        {
            // 1. 预检查：验证进程是否存在
            Process proc;
            try
            {
                proc = Process.GetProcessById(pid);
            }
            catch (ArgumentException)
            {
                // Log.Information($"进程 {pid} 不存在");
                return null; // 返回 null 而不是抛异常
            }

            // 2. 检查进程是否已退出
            bool hasExited = false;
            try
            {
                hasExited = proc.HasExited;
            }
            catch (Exception ex)
            {
                Log.Information($"无法检查进程 {pid} 状态: {ex.Message}");
                return null; // 如果连状态都无法检查，直接返回 null
            }

            if (hasExited)
            {
                Log.Information($"进程 {pid} 已退出");
                return null; // 或者返回一个标记为已退出的 ProgramInfo 对象
            }

            #region 检查缓存

            var pck = ProcessCache.GetProcessKey(pid);
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
                ProcessId = proc.Id
            };

            // 安全获取启动时间
            try
            {
                exeInfo.StartTime = proc.StartTime;
            }
            catch (Exception ex)
            {
                Log.Information($"无法获取进程 {pid} 的启动时间: {ex.Message}");
                exeInfo.StartTime = DateTime.MinValue;
            }

            // 安全获取退出状态（再次检查，因为状态可能在执行过程中改变）
            try
            {
                exeInfo.HasExited = proc.HasExited;
            }
            catch (Exception ex)
            {
                Log.Information($"无法获取进程 {pid} 的退出状态: {ex.Message}");
                exeInfo.HasExited = false;
            }

            // 安全获取内存使用量
            try
            {
                exeInfo.UseMemory = proc.WorkingSet64;
            }
            catch (Exception ex)
            {
                Log.Information($"无法获取进程 {pid} 的内存信息: {ex.Message}");
                exeInfo.UseMemory = 0;
            }

            // 安全获取线程数
            try
            {
                exeInfo.ThreadCount = proc.Threads.Count;
            }
            catch (Exception ex)
            {
                Log.Information($"无法获取进程 {pid} 的线程信息: {ex.Message}");
                exeInfo.ThreadCount = 0;
            }

            // 安全获取主模块信息
            try
            {
                var mainModule = proc.MainModule;
                exeInfo.MainModulePath = mainModule?.FileName;
                exeInfo.MainModuleName = mainModule?.ModuleName;
            }
            catch (Exception ex)
            {
                Log.Information($"无法获取进程 {pid} 的模块信息！: {ex.Message}");
                exeInfo.MainModulePath = null;
                exeInfo.MainModuleName = proc.ProcessName; // 使用进程名作为备用
            }

            // 只有在进程已退出时才能访问 ExitTime 和 ExitCode
            if (exeInfo.HasExited)
            {
                try
                {
                    exeInfo.ExitTime = proc.ExitTime;
                    exeInfo.ExitCode = proc.ExitCode;
                }
                catch (Exception ex)
                {
                    Log.Information($"无法获取进程 {pid} 的退出信息: {ex.Message}");
                    exeInfo.ExitTime = null;
                    exeInfo.ExitCode = null;
                }
            }
            else
            {
                exeInfo.ExitTime = null;
                exeInfo.ExitCode = null;
            }

            // 获取版本信息
            if (exeInfo.MainModulePath != null)
            {
                try
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(exeInfo.MainModulePath);

                    exeInfo.FileDescription = versionInfo.FileDescription;
                    exeInfo.ProductName = versionInfo.ProductName;
                    exeInfo.CompanyName = versionInfo.CompanyName;
                    exeInfo.Version = versionInfo.FileVersion;
                    exeInfo.LegalCopyright = versionInfo.LegalCopyright;
                }
                catch (Exception ex)
                {
                    Log.Information($"无法获取进程 {pid} 的版本信息: {ex.Message}");
                }
            }

            // 获取软件 Icon
            try
            {
                var iconBase64 = IconHelper.GetIconBase64(exeInfo.MainModulePath);
                exeInfo.IconBase64 = iconBase64;
            }
            catch (Exception ex)
            {
                Log.Information($"无法获取进程 {pid} 的图标: {ex.Message}");
            }

            // 获取编译时间戳（PE Header 中的时间戳）
            try
            {
                if (!string.IsNullOrEmpty(exeInfo.MainModulePath))
                {
                    var compileTime = GetPECompileTime(exeInfo.MainModulePath);
                    exeInfo.CompileTimestamp = compileTime;
                }
            }
            catch (Exception ex)
            {
                Log.Information($"无法获取进程 {pid} 的编译时间戳: {ex.Message}");
                exeInfo.CompileTimestamp = null;
            }

            #endregion

            ProcessCache.Instance.Set(pck, exeInfo);
            return exeInfo;
        }
        // 3. 关键改进：捕获异常但不再抛出，而是返回 null
        catch (ArgumentException ex)
        {
            Log.Warning($"获取进程信息失败：{ex.Message}"); // 改为 Warning 级别
            return null; // 返回 null 而不是抛异常
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning($"进程 {pid} 已退出或无法访问：{ex.Message}");
            return null;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Log.Warning($"无权限访问进程 {pid}：{ex.Message}");
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Warning($"拒绝访问进程 {pid}：{ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error($"获取进程 {pid} 信息时发生未知错误：{ex.Message}"); // 未知错误用 Error 级别
            return null;
        }
        finally
        {
            // 4. 添加资源清理（如果需要）
            // proc?.Dispose(); // 注意：Process 对象通常不需要手动释放，GC 会处理
        }
    }

    /// <summary>
    /// 获取项目编译时间
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private static DateTime? GetPECompileTime(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            fs.Seek(0x3C, SeekOrigin.Begin);
            int peOffset = reader.ReadInt32();

            fs.Seek(peOffset + 8, SeekOrigin.Begin);
            int timestamp = reader.ReadInt32();

            return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取程序路径（名称）
    /// </summary>
    /// <param name="pid"></param>
    /// <returns></returns>
    public static string GetProcessPath(int pid)
    {
        var proc = Process.GetProcessById(pid);
        var mainModule = proc.MainModule;
        if (mainModule == null)
        {
            return proc.ProcessName;
        }
        else
        {
            return mainModule.FileName;
        }
    }
    // WMI 查询（能拿到路径，但性能稍差）
    // static string GetProcessPathWmi(int pid)
    //  {
    //      using var searcher = new ManagementObjectSearcher(
    //          $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {pid}");
    //
    //      foreach (ManagementObject obj in searcher.Get())
    //      {
    //          return obj["ExecutablePath"]?.ToString();
    //      }
    //
    //      return null;
    //  }

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
// Infrastructure.utils FormatBytes