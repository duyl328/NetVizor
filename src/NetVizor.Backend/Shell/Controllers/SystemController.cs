using System;
using System.Linq;
using System.Threading.Tasks;
using Application.Utils;
using Common.Net.HttpConn;
using Common.Net.Models;
using Common.Utils;
using Utils.ETW.Etw;

namespace Shell.Controllers;

public class SystemController : BaseController
{
    public async Task GetRealtimeNetworkDataAsync(HttpContext context)
    {
        try
        {
            var networkCardGuid = GetQueryParam(context, "networkCardGuid");

            var interfaces = BasicNetworkMonitor.GetConnectedNetworkInterfaces();
            var targetInterfaces = string.IsNullOrEmpty(networkCardGuid)
                ? interfaces
                : interfaces.Where(i => i.Id == networkCardGuid).ToList();

            var realtimeData = new List<object>();
            foreach (var networkInterface in targetInterfaces)
            {
                var speed = BasicNetworkMonitor.CalculateSpeedById(networkInterface.Id);
                realtimeData.Add(new
                {
                    networkCardGuid = networkInterface.Id,
                    networkCardName = networkInterface.Name,
                    uploadSpeed = speed.UploadSpeed,
                    downloadSpeed = speed.DownloadSpeed,
                    uploadSpeedText = speed.UploadSpeedText,
                    downloadSpeedText = speed.DownloadSpeedText,
                    isConnected = networkInterface.IsConnected,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            }

            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = true,
                Data = realtimeData,
                Message = "获取实时网络数据成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取实时网络数据失败: {ex.Message}", 500);
        }
    }

    public async Task GetActiveAppsAsync(HttpContext context)
    {
        try
        {
            var limit = int.Parse(GetQueryParam(context, "limit", "10"));

            var snapshot = GlobalNetworkMonitor.Instance.GetSnapshot();

            var activeApps = snapshot.Applications
                .Where(app => app.Connections.Any(c => c.IsActive && (c.BytesSent > 0 || c.BytesReceived > 0)))
                .OrderByDescending(app => app.Connections.Sum(c => c.BytesSent + c.BytesReceived))
                .Take(limit)
                .Select(app => new
                {
                    processId = app.ProcessId,
                    processName = app.ProgramInfo?.ProcessName ?? "Unknown",
                    path = app.ProgramInfo?.MainModulePath ?? "",
                    totalUploadBytes = app.Connections.Sum(c => c.BytesSent),
                    totalDownloadBytes = app.Connections.Sum(c => c.BytesReceived),
                    activeConnections = app.Connections.Count(c => c.IsActive),
                    icon = app.ProgramInfo?.IconBase64 ?? ""
                })
                .ToList();

            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = true,
                Data = activeApps,
                Message = "获取实时活跃应用成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取实时活跃应用失败: {ex.Message}", 500);
        }
    }

    public async Task GetSystemInfoAsync(HttpContext context)
    {
        try
        {
            var systemInfo = new
            {
                version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                isAdministrator = SysHelper.IsAdministrator(),
                etwEnabled = SysHelper.IsAdministrator(),
                operatingSystem = Environment.OSVersion.ToString(),
                machineName = Environment.MachineName,
                userDomainName = Environment.UserDomainName,
                userName = Environment.UserName,
                processorCount = Environment.ProcessorCount,
                workingSet = Environment.WorkingSet,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = true,
                Data = systemInfo,
                Message = "获取系统信息成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取系统信息失败: {ex.Message}", 500);
        }
    }

    public async Task GetCollectionStatsAsync(HttpContext context)
    {
        try
        {
            var stats = new
            {
                isRunning = true,
                activeNetworkInterfaces = BasicNetworkMonitor.GetConnectedNetworkInterfaces().Count,
                etwStatus = SysHelper.IsAdministrator() ? "运行中" : "需要管理员权限",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = true,
                Data = stats,
                Message = "获取收集统计信息成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取收集统计信息失败: {ex.Message}", 500);
        }
    }
}
