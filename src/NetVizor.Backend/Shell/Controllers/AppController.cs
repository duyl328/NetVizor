using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Utils;
using Common.Net.HttpConn;
using Common.Net.Models;
using Data;
using Data.Models;
using Data.Repositories;
using Shell.Utils;

namespace Shell.Controllers;

public class AppController : BaseController
{
    public async Task GetTopTrafficAppsAsync(HttpContext context)
    {
        try
        {
            var timeRange = GetQueryParam(context, "timeRange", "1hour");
            var page = int.Parse(GetQueryParam(context, "page", "1"));
            var pageSize = int.Parse(GetQueryParam(context, "pageSize", "100"));

            IEnumerable<AppNetworkTopInfo> allApps;

            if (timeRange == "1hour")
            {
                allApps = await DatabaseManager.Instance.Networks.GetTopAppsByTrafficAsync(pageSize * page, 1);
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
                allApps = await DatabaseManager.Instance.Networks.GetTopAppsByTrafficAsync(pageSize * page, days);
            }

            var totalCount = allApps.Count();
            var pagedApps = allApps.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var items = new List<object>();
            int rank = (page - 1) * pageSize + 1;

            foreach (var app in pagedApps)
            {
                string processName = System.IO.Path.GetFileName(app.AppPath ?? "");
                string displayName = app.AppName ?? "";
                string iconBase64 = "";
                string version = "";
                string company = "";

                var hasAppId = !string.IsNullOrEmpty(app.AppId);
                var hasDisplayName = !string.IsNullOrEmpty(displayName);
                
                if (hasDisplayName && hasAppId)
                {
                    var appInfo = await DatabaseManager.Instance.AppInfos.GetAppByAppIdAsync(app.AppId);
                    if (appInfo != null)
                    {
                        displayName = appInfo.Name ?? "";
                        iconBase64 = appInfo.Base64Icon ?? "";
                        version = appInfo.Version ?? "";
                        company = appInfo.Company ?? "";
                    }
                }

                if (hasDisplayName)
                {
                    displayName = processName.Replace(".exe", "") ?? "Unknown";
                }

                items.Add(new
                {
                    rank = rank++,
                    processName = processName,
                    displayName = displayName,
                    processPath = app.AppPath ?? "",
                    appId = app.AppId ?? "",
                    icon = iconBase64,
                    version = version,
                    company = company,
                    totalBytes = app.TotalTraffic,
                    uploadBytes = app.TotalUpload,
                    connectionCount = app.ConnectionCount
                });
            }

            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = true,
                Data = new
                {
                    total = totalCount,
                    page = page,
                    pageSize = pageSize,
                    items = items
                },
                Message = "获取软件流量排行成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取软件流量排行失败: {ex.Message}", 500);
        }
    }

    public async Task GetNetworkAnalysisAsync(HttpContext context)
    {
        try
        {
            var appId = GetQueryParam(context, "appId");
            var timeRange = GetQueryParam(context, "timeRange", "1day");

            if (string.IsNullOrEmpty(appId))
            {
                await WriteErrorResponseAsync(context, "缺少必需参数 appId");
                return;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long startTime = timeRange switch
            {
                "1hour" => DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(),
                "1day" => DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(),
                "7days" => DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds(),
                "30days" => DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds(),
                _ => DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds()
            };

            var appInfo = await DatabaseManager.Instance.AppInfos.GetAppByAppIdAsync(appId);
            var networkData = await DatabaseManager.Instance.Networks.GetAppNetworkByTimeRangeAsync(appId, startTime, now);
            var networkList = networkData.ToList();

            var totalUpload = networkList.Sum(x => x.UploadBytes);
            var totalDownload = networkList.Sum(x => x.DownloadBytes);
            var totalConnections = networkList.Count;

            var topConnections = networkList
                .Where(x => x.UploadBytes + x.DownloadBytes > 0)
                .GroupBy(x => new { x.LocalIP, x.LocalPort, x.RemoteIP, x.RemotePort, x.Protocol })
                .Select(g => new
                {
                    localIP = g.Key.LocalIP,
                    localPort = g.Key.LocalPort,
                    remoteIP = g.Key.RemoteIP,
                    remotePort = g.Key.RemotePort,
                    protocol = g.Key.Protocol,
                    totalUpload = g.Sum(x => x.UploadBytes),
                    totalDownload = g.Sum(x => x.DownloadBytes),
                    totalTraffic = g.Sum(x => x.UploadBytes + x.DownloadBytes),
                    connectionCount = g.Count(),
                    firstSeen = DateTimeOffset.FromUnixTimeSeconds(g.Min(x => x.Timestamp)).ToString("yyyy-MM-dd HH:mm:ss"),
                    lastSeen = DateTimeOffset.FromUnixTimeSeconds(g.Max(x => x.Timestamp)).ToString("yyyy-MM-dd HH:mm:ss")
                })
                .OrderByDescending(x => x.totalTraffic)
                .Take(30)
                .ToList();

            var protocolStats = networkList
                .Where(x => x.UploadBytes + x.DownloadBytes > 0)
                .GroupBy(x => x.Protocol.ToUpper())
                .Select(g => new
                {
                    protocol = g.Key,
                    connectionCount = g.Count(),
                    totalTraffic = g.Sum(x => x.UploadBytes + x.DownloadBytes),
                    percentage = Math.Round((double)g.Count() / networkList.Where(x => x.UploadBytes + x.DownloadBytes > 0).Count() * 100, 2)
                })
                .OrderByDescending(x => x.totalTraffic)
                .ToList();

            var timeTrends = networkList
                .GroupBy(x => x.Timestamp / 3600 * 3600)
                .Select(g => new
                {
                    timestamp = g.Key,
                    timeStr = DateTimeOffset.FromUnixTimeSeconds(g.Key).ToString("yyyy-MM-dd HH:mm"),
                    upload = g.Sum(x => x.UploadBytes),
                    download = g.Sum(x => x.DownloadBytes),
                    connections = g.Count()
                })
                .OrderBy(x => x.timestamp)
                .ToList();

            var portAnalysis = networkList
                .Where(x => x.UploadBytes + x.DownloadBytes > 0)
                .GroupBy(x => x.RemotePort)
                .Select(g => new
                {
                    port = g.Key,
                    serviceName = NetworkApiHelper.GetPortServiceName(g.Key),
                    connectionCount = g.Count(),
                    totalTraffic = g.Sum(x => x.UploadBytes + x.DownloadBytes),
                    protocols = g.Select(x => x.Protocol).Distinct().ToList()
                })
                .OrderByDescending(x => x.totalTraffic)
                .Take(20)
                .ToList();

            var result = new
            {
                appInfo = new
                {
                    appId = appId,
                    name = appInfo?.Name ?? "Unknown Application",
                    company = appInfo?.Company ?? "",
                    version = appInfo?.Version ?? "",
                    path = appInfo?.Path ?? "",
                    icon = appInfo?.Base64Icon ?? "",
                    hash = appInfo?.Hash ?? ""
                },

                summary = new
                {
                    timeRange = timeRange,
                    startTime = DateTimeOffset.FromUnixTimeSeconds(startTime).ToString("yyyy-MM-dd HH:mm:ss"),
                    endTime = DateTimeOffset.FromUnixTimeSeconds(now).ToString("yyyy-MM-dd HH:mm:ss"),
                    totalUpload = totalUpload,
                    totalDownload = totalDownload,
                    totalTraffic = totalUpload + totalDownload,
                    totalConnections = totalConnections,
                    uniqueRemoteIPs = networkList.Select(x => x.RemoteIP).Distinct().Count(),
                    uniqueRemotePorts = networkList.Select(x => x.RemotePort).Distinct().Count()
                },

                topConnections = topConnections,
                protocolStats = protocolStats,
                timeTrends = timeTrends,
                portAnalysis = portAnalysis
            };

            await WriteJsonResponseAsync(context, new ResponseModel<object>
            {
                Success = true,
                Data = result,
                Message = "获取应用网络分析数据成功"
            });
        }
        catch (Exception ex)
        {
            await WriteErrorResponseAsync(context, $"获取应用网络分析数据失败: {ex.Message}", 500);
        }
    }
}
