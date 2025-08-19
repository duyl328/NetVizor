using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Data;
using Data.Models;
using Dapper;

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
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            switch (dataType)
            {
                case "hourly":
                    // 1小时视图：检查原始数据（临时方案，直到数据库迁移完成）
                    var rawData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByTimeRangeAsync("", startTime, currentTime);
                    return rawData.Any();
                    
                case "daily":
                    // 24小时视图：优先检查小时级聚合数据，其次检查5分钟级和原始数据
                    var hourlyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByHourAsync("", 24);
                    if (hourlyData.Any(h => h.HourTimestamp >= startTime)) return true;
                    
                    var minutelyDataDaily = await DatabaseManager.Instance.Networks.GetGlobalNetworkByMinuteAsync("", 1440); // 24小时 = 1440分钟
                    if (minutelyDataDaily.Any(m => m.MinuteTimestamp >= startTime)) return true;
                    
                    var rawDataDaily = await DatabaseManager.Instance.Networks.GetGlobalNetworkByTimeRangeAsync("", startTime, currentTime);
                    return rawDataDaily.Any();
                    
                default:
                    // 7天/30天视图：检查天级聚合数据
                    var dailyData = await DatabaseManager.Instance.Networks.GetGlobalNetworkByDayAsync("", 32);
                    return dailyData.Any(d => d.DayTimestamp >= startTime);
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
            // 获取实际数据的时间范围
            var (actualStartTime, actualEndTime) = await GetActualDataRangeAsync(startTime, endTime);
            
            if (actualStartTime == 0 || actualEndTime == 0)
            {
                return points; // 返回空数据
            }

            // 智能调整查询范围：使用实际数据范围与请求范围的交集
            var queryStartTime = Math.Max(startTime, actualStartTime);
            var queryEndTime = Math.Min(endTime, actualEndTime);
            
            // 计算实际数据跨度来动态调整聚合间隔
            var actualDataSpan = queryEndTime - queryStartTime;
            if (timeRange == "hour")
            {
                // 1小时视图：动态调整聚合间隔
                int intervalSeconds;
                if (actualDataSpan <= 300) // 5分钟内的数据
                {
                    intervalSeconds = 5; // 5秒聚合
                }
                else if (actualDataSpan <= 1800) // 30分钟内的数据
                {
                    intervalSeconds = 10; // 10秒聚合
                }
                else
                {
                    intervalSeconds = 30; // 30秒聚合
                }

                if (interfaceId == "all")
                {
                    var allData = await DatabaseManager.Instance.Networks
                        .GetGlobalNetworkByTimeRangeAsync("", queryStartTime, queryEndTime);
                    var groupedData = allData.GroupBy(d => (d.Timestep / intervalSeconds) * intervalSeconds)
                        .Select(g => new
                        {
                            timestamp = g.Key.ToString(),
                            uploadSpeed = (long)g.Sum(x => x.Upload),
                            downloadSpeed = (long)g.Sum(x => x.Download)
                        })
                        .OrderBy(x => long.Parse(x.timestamp));
                    points.AddRange(groupedData);
                }
                else
                {
                    var data = await DatabaseManager.Instance.Networks
                        .GetGlobalNetworkByTimeRangeAsync(interfaceId, queryStartTime, queryEndTime);
                    var groupedData = data.GroupBy(d => (d.Timestep / intervalSeconds) * intervalSeconds)
                        .Select(g => new
                        {
                            timestamp = g.Key.ToString(),
                            uploadSpeed = (long)g.Average(x => x.Upload),
                            downloadSpeed = (long)g.Average(x => x.Download)
                        })
                        .OrderBy(x => long.Parse(x.timestamp));
                    points.AddRange(groupedData);
                }
            }
            else if (timeRange == "day")
            {
                // 24小时视图：动态调整聚合间隔
                int intervalSeconds;
                if (actualDataSpan <= 1800) // 30分钟内的数据
                {
                    intervalSeconds = 30; // 30秒聚合
                }
                else if (actualDataSpan <= 7200) // 2小时内的数据  
                {
                    intervalSeconds = 60; // 1分钟聚合
                }
                else
                {
                    intervalSeconds = 300; // 5分钟聚合
                }

                if (interfaceId == "all")
                {
                    var allData = await DatabaseManager.Instance.Networks
                        .GetGlobalNetworkByTimeRangeAsync("", queryStartTime, queryEndTime);
                    var groupedData = allData.GroupBy(d => (d.Timestep / intervalSeconds) * intervalSeconds)
                        .Select(g => new
                        {
                            timestamp = g.Key.ToString(),
                            uploadSpeed = (long)g.Sum(x => x.Upload),
                            downloadSpeed = (long)g.Sum(x => x.Download)
                        })
                        .OrderBy(x => long.Parse(x.timestamp));
                    points.AddRange(groupedData);
                }
                else
                {
                    var data = await DatabaseManager.Instance.Networks
                        .GetGlobalNetworkByTimeRangeAsync(interfaceId, queryStartTime, queryEndTime);
                    var groupedData = data.GroupBy(d => (d.Timestep / intervalSeconds) * intervalSeconds)
                        .Select(g => new
                        {
                            timestamp = g.Key.ToString(),
                            uploadSpeed = (long)g.Average(x => x.Upload),
                            downloadSpeed = (long)g.Average(x => x.Download)
                        })
                        .OrderBy(x => long.Parse(x.timestamp));
                    points.AddRange(groupedData);
                }
            }
            else
            {
                // 7天/30天视图：智能调整聚合间隔
                int intervalSeconds;
                
                if (timeRange == "week")
                {
                    // 7天视图：根据实际数据跨度动态调整
                    if (actualDataSpan <= 7200) // 2小时内数据
                    {
                        intervalSeconds = 300; // 5分钟聚合
                    }
                    else if (actualDataSpan <= 86400) // 1天内数据
                    {
                        intervalSeconds = 900; // 15分钟聚合
                    }
                    else
                    {
                        intervalSeconds = 1800; // 30分钟聚合
                    }
                }
                else
                {
                    // 30天视图：根据实际数据跨度动态调整
                    if (actualDataSpan <= 86400) // 1天内数据
                    {
                        intervalSeconds = 1800; // 30分钟聚合
                    }
                    else if (actualDataSpan <= 604800) // 1周内数据
                    {
                        intervalSeconds = 3600; // 1小时聚合
                    }
                    else
                    {
                        intervalSeconds = 7200; // 2小时聚合
                    }
                }

                if (interfaceId == "all")
                {
                    var allData = await DatabaseManager.Instance.Networks
                        .GetGlobalNetworkByTimeRangeAsync("", queryStartTime, queryEndTime);
                    var groupedData = allData.GroupBy(d => (d.Timestep / intervalSeconds) * intervalSeconds)
                        .Select(g => new
                        {
                            timestamp = g.Key.ToString(),
                            uploadSpeed = (long)g.Sum(x => x.Upload),
                            downloadSpeed = (long)g.Sum(x => x.Download)
                        })
                        .OrderBy(x => long.Parse(x.timestamp));
                    points.AddRange(groupedData);
                }
                else
                {
                    var data = await DatabaseManager.Instance.Networks
                        .GetGlobalNetworkByTimeRangeAsync(interfaceId, queryStartTime, queryEndTime);
                    var groupedData = data.GroupBy(d => (d.Timestep / intervalSeconds) * intervalSeconds)
                        .Select(g => new
                        {
                            timestamp = g.Key.ToString(),
                            uploadSpeed = (long)g.Average(x => x.Upload),
                            downloadSpeed = (long)g.Average(x => x.Download)
                        })
                        .OrderBy(x => long.Parse(x.timestamp));
                    points.AddRange(groupedData);
                }
            }
        }
        catch
        {
            // 如果出错，返回空数据
        }

        return points;
    }

    /// <summary>
    /// 获取指定时间范围内的实际数据范围
    /// </summary>
    private static async Task<(long actualStartTime, long actualEndTime)> GetActualDataRangeAsync(long startTime, long endTime)
    {
        try
        {
            // 查询指定时间范围内的最早和最晚时间戳 - 通过反射获取NetworkRepository的连接
            var networkRepo = DatabaseManager.Instance.Networks;
            var contextField = networkRepo.GetType().GetField("_context", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var context = contextField?.GetValue(networkRepo);
            var connection = context?.GetType().GetProperty("Connection")?.GetValue(context) as System.Data.IDbConnection;
            
            if (connection == null)
            {
                return (0, 0);
            }
            
            var actualStartTime = await connection.QuerySingleOrDefaultAsync<long?>(
                "SELECT MIN(Timestep) FROM GlobalNetwork WHERE Timestep >= @StartTime AND Timestep <= @EndTime",
                new { StartTime = startTime, EndTime = endTime }) ?? 0;
            var actualEndTime = await connection.QuerySingleOrDefaultAsync<long?>(
                "SELECT MAX(Timestep) FROM GlobalNetwork WHERE Timestep >= @StartTime AND Timestep <= @EndTime",
                new { StartTime = startTime, EndTime = endTime }) ?? 0;

            return (actualStartTime, actualEndTime);
        }
        catch
        {
            return (0, 0);
        }
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