using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Utils;
using Common.Net.HttpConn;
using Common.Net.Models;
using Common.Utils;
using Data;
using Data.Models;
using Data.Repositories;
using Shell.Utils;
using Shell.Services;

namespace Shell.Controllers;

public class NetworkController : BaseController
{
    private static ServerStartupManager? _serverManager;

    public async Task GetInterfacesAsync(HttpContext context)
    {
        try
        {
            var timeRange = GetQueryParam(context, "timeRange", "hour");
            var cacheKey = $"interfaces_{timeRange}";
            var now = DateTime.Now;

            if (NetworkApiHelper.NetworkInterfaceCache.ContainsKey(cacheKey) &&
                (now - NetworkApiHelper.CacheLastUpdateTime) < NetworkApiHelper.CacheExpireTime)
            {
                await WriteJsonResponseAsync(context, new ResponseModel<object>
                {
                    Success = true,
                    Data = NetworkApiHelper.NetworkInterfaceCache[cacheKey],
                    Message = "获取网络接口列表成功（缓存）"
                });
                return;
            }

            var networkCardGuids = new HashSet<string>();
            var startTime = timeRange.ToLower() switch
            {
                "hour" => DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(),
                "day" => DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(),
                "week" => DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds(),
                "month" => DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds(),
                _ => DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds()
            };

            var endTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var data = await DatabaseManager.Instance.Networks.GetGlobalNetworkByTimeRangeAsync("", startTime, endTime);
            foreach (var item in data)
                networkCardGuids.Add(item.NetworkCardGuid);

            if (!networkCardGuids.Any())
            {
                var connectedInterfaces = BasicNetworkMonitor.GetConnectedNetworkInterfaces();
                foreach (var iface in connectedInterfaces)
                {
                    networkCardGuids.Add(iface.Id);
                }
            }

            var interfaces = new List<object>();
            foreach (var guid in networkCardGuids)
            {
                var networkInfo = NetworkAdapterHelper.GetNetworkInfoByGuid(guid);
                if (networkInfo != null)
                {
                    interfaces.Add(new
                    {
                        id = guid,
                        name = networkInfo.NetConnectionID ?? networkInfo.Name,
                        displayName = networkInfo.Description ?? networkInfo.Name,
                        isActive = true,
                        macAddress = networkInfo.MACAddress
                    });
                }
                else
                {
                    interfaces.Add(new
                    {
                        id = guid,
                        name = $"Network Interface {guid.Substring(0, 8)}",
                        displayName = $"Network Adapter {guid.Substring(0, 8)}",
                        isActive = true,
                        macAddress = ""
                    });
                }
            }

            NetworkApiHelper.NetworkInterfaceCache[cacheKey] = interfaces;
            NetworkApiHelper.UpdateCacheTime();

            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = true,
                Data = interfaces,
                Message = "获取网络接口列表成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取网络接口列表失败: {ex.Message}", 500);
        }
    }

    public async Task GetAvailableRangesAsync(HttpContext context)
    {
        try
        {
            var availableRanges = new List<object>();
            var now = DateTimeOffset.UtcNow;

            var hasHourlyData =
                await NetworkApiHelper.HasNetworkDataInTimeRangeAsync("hourly", now.AddHours(-1).ToUnixTimeSeconds());
            if (hasHourlyData)
            {
                availableRanges.Add(new
                {
                    type = "hour",
                    name = "1小时",
                    available = true,
                    startTime = now.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
            }

            var hasDailyData =
                await NetworkApiHelper.HasNetworkDataInTimeRangeAsync("daily", now.AddDays(-1).ToUnixTimeSeconds());
            if (hasDailyData)
            {
                availableRanges.Add(new
                {
                    type = "day",
                    name = "1天",
                    available = true,
                    startTime = now.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
            }

            var hasWeeklyData =
                await NetworkApiHelper.HasNetworkDataInTimeRangeAsync("daily", now.AddDays(-7).ToUnixTimeSeconds());
            if (hasWeeklyData)
            {
                availableRanges.Add(new
                {
                    type = "week",
                    name = "1周",
                    available = true,
                    startTime = now.AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
            }

            var hasMonthlyData =
                await NetworkApiHelper.HasNetworkDataInTimeRangeAsync("daily", now.AddDays(-30).ToUnixTimeSeconds());
            if (hasMonthlyData)
            {
                availableRanges.Add(new
                {
                    type = "month",
                    name = "1个月",
                    available = true,
                    startTime = now.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
            }

            if (!availableRanges.Any())
            {
                availableRanges.Add(new
                {
                    type = "hour",
                    name = "1小时",
                    available = true,
                    startTime = now.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });
            }

            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = true,
                Data = availableRanges.First(),
                Message = "获取可用时间范围成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取可用时间范围失败: {ex.Message}", 500);
        }
    }

    public async Task ClearCacheAsync(HttpContext context)
    {
        try
        {
            NetworkApiHelper.NetworkInterfaceCache.Clear();

            await WriteJsonResponseAsync(context, new ResponseModel<string>
            {
                Success = true,
                Data = "缓存已清理",
                Message = "网络接口缓存清理成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"清理缓存失败: {ex.Message}", 500);
        }
    }

    public async Task GetTrafficTrendsAsync(HttpContext context)
    {
        try
        {
            var timeRange = GetQueryParam(context, "timeRange", "1hour");
            var interfaceId = GetQueryParam(context, "interfaceId", "all");

            var now = DateTimeOffset.UtcNow;
            var (startTime, mappedTimeRange) = timeRange switch
            {
                "1hour" => (now.AddHours(-1).ToUnixTimeSeconds(), "hour"),
                "1day" => (now.AddDays(-1).ToUnixTimeSeconds(), "day"),
                "7days" => (now.AddDays(-7).ToUnixTimeSeconds(), "week"),
                "30days" => (now.AddDays(-30).ToUnixTimeSeconds(), "month"),
                _ => (now.AddHours(-1).ToUnixTimeSeconds(), "hour")
            };

            var points = await NetworkApiHelper.GetTrafficTrendsAsync(mappedTimeRange, interfaceId, startTime,
                now.ToUnixTimeSeconds());

            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = true,
                Data = new
                {
                    @interface = interfaceId,
                    timeRange = timeRange,
                    points = points
                },
                Message = "获取流量趋势数据成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取流量趋势数据失败: {ex.Message}", 500);
        }
    }

    public async Task GetTopAppsAsync(HttpContext context)
    {
        try
        {
            var timeRange = GetQueryParam(context, "timeRange", "1hour");
            var limit = int.Parse(GetQueryParam(context, "limit", "10"));

            IEnumerable<AppNetworkTopInfo> topApps;

            if (timeRange == "1hour")
            {
                topApps = await DatabaseManager.Instance.Networks.GetTopAppsByTrafficAsync(limit, 1);
            }
            else
            {
                int days = timeRange switch
                {
                    "1day" => 1,
                    "7days" => 7,
                    "30days" => 30,
                    _ => 1
                };
                topApps = await DatabaseManager.Instance.Networks.GetTopAppsByTrafficAsync(limit, days);
            }

            var result = new List<object>();

            foreach (var app in topApps)
            {
                string processName = System.IO.Path.GetFileName(app.AppPath ?? "");
                string displayName = app.AppName ?? "";
                string iconBase64 = "";

                if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(app.AppId))
                {
                    var appInfo = await DatabaseManager.Instance.AppInfos.GetAppByAppIdAsync(app.AppId);
                    if (appInfo != null)
                    {
                        displayName = appInfo.Name ?? "";
                        iconBase64 = appInfo.Base64Icon ?? "";
                    }
                }

                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = processName.Replace(".exe", "") ?? "Unknown";
                }

                result.Add(new
                {
                    processName = processName,
                    displayName = displayName,
                    icon = iconBase64,
                    totalBytes = app.TotalTraffic
                });
            }

            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = true,
                Data = result,
                Message = "获取Top应用流量数据成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取Top应用流量数据失败: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// 获取实时网络统计数据
    /// </summary>
    public async Task GetRealTimeStatsAsync(HttpContext context)
    {
        try
        {
            if (_serverManager == null)
            {
                await WriteErrorResponseAsync(context, "网络监控服务未启动", 503);
                return;
            }

            // 获取所有活跃进程的实时统计
            var activeProcessStats = _serverManager.GetAllActiveProcessStats();

            // 计算总体统计
            var totalUploadSpeed = activeProcessStats.Sum(s => s.BytesOutPerSecond);
            var totalDownloadSpeed = activeProcessStats.Sum(s => s.BytesInPerSecond);
            var totalSpeed = totalUploadSpeed + totalDownloadSpeed;

            // 格式化进程列表
            var processes = activeProcessStats.OrderByDescending(s => s.TotalBytesPerSecond)
                .Take(20) // 只返回前20个活跃进程
                .Select(s => new
                {
                    processId = s.ProcessId,
                    processName = s.ProcessName,
                    connectionCount = s.ConnectionCount,
                    bytesInPerSecond = s.BytesInPerSecond,
                    bytesOutPerSecond = s.BytesOutPerSecond,
                    totalBytesPerSecond = s.TotalBytesPerSecond,
                    formattedDownloadSpeed = s.FormattedDownloadSpeed,
                    formattedUploadSpeed = s.FormattedUploadSpeed,
                    formattedTotalSpeed = s.FormattedTotalSpeed,
                    lastUpdate = s.LastUpdate
                }).ToList();

            var result = new
            {
                timestamp = DateTime.Now,
                summary = new
                {
                    totalUploadSpeed = totalUploadSpeed,
                    totalDownloadSpeed = totalDownloadSpeed,
                    totalSpeed = totalSpeed,
                    formattedUploadSpeed = FormatSpeed(totalUploadSpeed),
                    formattedDownloadSpeed = FormatSpeed(totalDownloadSpeed),
                    formattedTotalSpeed = FormatSpeed(totalSpeed),
                    activeProcessCount = activeProcessStats.Count
                },
                processes = processes
            };

            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = true,
                Data = result,
                Message = "获取实时网络统计成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取实时网络统计失败: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// 设置ServerStartupManager实例（供启动时调用）
    /// </summary>
    public static void SetServerManager(ServerStartupManager serverManager)
    {
        _serverManager = serverManager;
    }

    /// <summary>
    /// 格式化网速显示
    /// </summary>
    private static string FormatSpeed(double bytesPerSec)
    {
        if (bytesPerSec >= 1024 * 1024 * 1024)
            return $"{bytesPerSec / (1024.0 * 1024 * 1024):F1} GB/s";
        if (bytesPerSec >= 1024 * 1024)
            return $"{bytesPerSec / (1024.0 * 1024):F1} MB/s";
        if (bytesPerSec >= 1024)
            return $"{bytesPerSec / 1024.0:F1} KB/s";
        return $"{bytesPerSec:F0} B/s";
    }
}