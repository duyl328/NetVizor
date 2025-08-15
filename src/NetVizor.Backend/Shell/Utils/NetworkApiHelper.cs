using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data;
using Data.Models;

namespace Shell.Utils;

public static class NetworkApiHelper
{
    private static readonly Dictionary<string, object> _networkInterfaceCache = new();
    private static DateTime _cacheLastUpdateTime = DateTime.MinValue;
    private static readonly TimeSpan _cacheExpireTime = TimeSpan.FromMinutes(5);

    public static Dictionary<string, object> NetworkInterfaceCache => _networkInterfaceCache;
    public static DateTime CacheLastUpdateTime => _cacheLastUpdateTime;
    public static TimeSpan CacheExpireTime => _cacheExpireTime;

    public static void UpdateCacheTime() => _cacheLastUpdateTime = DateTime.Now;

    public static async Task<List<GlobalNetworkAggregatedBase>> GetAllNetworkCardsHourlyDataAsync(int hours)
    {
        var result = new List<GlobalNetworkAggregatedBase>();
        
        try
        {
            var allHourlyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync("", hours);
            var networkGuids = allHourlyData.Select(x => "").Distinct();

            foreach (var guid in networkGuids)
            {
                if (!string.IsNullOrEmpty(guid))
                    result.Add(new GlobalNetworkHourly { NetworkCardGuid = guid });
            }
        }
        catch
        {
            // 如果出错，返回空列表
        }

        return result;
    }

    public static async Task<List<GlobalNetworkAggregatedBase>> GetAllNetworkCardsDailyDataAsync(int days)
    {
        var result = new List<GlobalNetworkAggregatedBase>();

        try
        {
            var allDailyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync("", days);
            var networkGuids = allDailyData.Select(x => "").Distinct();

            foreach (var guid in networkGuids)
            {
                if (!string.IsNullOrEmpty(guid))
                    result.Add(new GlobalNetworkDaily { NetworkCardGuid = guid });
            }
        }
        catch
        {
            // 如果出错，返回空列表
        }

        return result;
    }

    public static async Task<bool> HasNetworkDataInTimeRangeAsync(string dataType, long startTime)
    {
        try
        {
            switch (dataType)
            {
                case "hourly":
                    var hourlyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync("", 1);
                    return hourlyData.Any();
                case "daily":
                    var dailyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync("", 1);
                    return dailyData.Any();
                default:
                    var networkData = await DatabaseManager.Instance.Networks.GetGlobalNetworkHistoryAsync("", 1);
                    return networkData.Any();
            }
        }
        catch
        {
            return false;
        }
    }

    public static async Task<List<object>> GetTrafficTrendsAsync(string timeRange, string interfaceId, long startTime, long endTime)
    {
        var points = new List<object>();

        try
        {
            if (timeRange == "hour")
            {
                if (interfaceId == "all")
                {
                    var allData = await DatabaseManager.Instance.Networks
                        .GetGlobalNetworkByTimeRangeAsync("", startTime, endTime);
                    var groupedData = allData.GroupBy(d => d.Timestep).Select(g => new
                    {
                        timestamp = g.Key.ToString(),
                        uploadSpeed = g.Sum(x => x.Upload),
                        downloadSpeed = g.Sum(x => x.Download)
                    });

                    points.AddRange(groupedData);
                }
                else
                {
                    var data = await DatabaseManager.Instance.Networks.GetGlobalNetworkByTimeRangeAsync(interfaceId,
                        startTime, endTime);
                    points.AddRange(data.Select(d => new
                    {
                        timestamp = d.Timestep.ToString(),
                        uploadSpeed = d.Upload,
                        downloadSpeed = d.Download
                    }));
                }
            }
            else if (timeRange == "day")
            {
                if (interfaceId == "all")
                {
                    var allData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync("", 24);
                    var groupedData = allData.GroupBy(d => d.HourTimestamp).Select(g => new
                    {
                        timestamp = g.Key.ToString(),
                        uploadSpeed = g.Sum(x => x.AvgUpload),
                        downloadSpeed = g.Sum(x => x.AvgDownload)
                    });

                    points.AddRange(groupedData);
                }
                else
                {
                    var data = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync(interfaceId, 24);
                    points.AddRange(data.Select(d => new
                    {
                        timestamp = d.HourTimestamp.ToString(),
                        uploadSpeed = d.AvgUpload,
                        downloadSpeed = d.AvgDownload
                    }));
                }
            }
            else
            {
                int days = timeRange == "week" ? 7 : 30;
                if (interfaceId == "all")
                {
                    var allData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync("", days);
                    var groupedData = allData.GroupBy(d => d.DayTimestamp).Select(g => new
                    {
                        timestamp = g.Key.ToString(),
                        uploadSpeed = g.Sum(x => x.AvgUpload),
                        downloadSpeed = g.Sum(x => x.AvgDownload)
                    });

                    points.AddRange(groupedData);
                }
                else
                {
                    var data = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync(interfaceId, days);
                    points.AddRange(data.Select(d => new
                    {
                        timestamp = d.DayTimestamp.ToString(),
                        uploadSpeed = d.AvgUpload,
                        downloadSpeed = d.AvgDownload
                    }));
                }
            }
        }
        catch
        {
            // 如果出错，返回空数据
        }

        return points;
    }

    public static string GetPortServiceName(int port)
    {
        return port switch
        {
            21 => "FTP",
            22 => "SSH",
            23 => "Telnet",
            25 => "SMTP",
            53 => "DNS",
            80 => "HTTP",
            110 => "POP3",
            123 => "NTP",
            143 => "IMAP",
            443 => "HTTPS",
            993 => "IMAPS",
            995 => "POP3S",
            1433 => "SQL Server",
            3306 => "MySQL",
            3389 => "RDP",
            5432 => "PostgreSQL",
            6379 => "Redis",
            8080 => "HTTP-Alt",
            8443 => "HTTPS-Alt",
            _ when port >= 1024 && port <= 5000 => "User Port",
            _ when port > 5000 && port < 32768 => "Service Port",
            _ when port >= 32768 => "Dynamic Port",
            _ => "Unknown"
        };
    }
}