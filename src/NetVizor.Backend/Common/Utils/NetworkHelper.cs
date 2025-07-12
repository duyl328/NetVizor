using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logger;

namespace Common.Utils
{
    /// <summary>
    /// 网络工具公共类 - 提供格式化、验证等公共功能
    /// </summary>
    public static class NetworkHelper
    {
        #region 常量定义

        private const long MAX_REASONABLE_SPEED = 1_000_000_000; // 1GB/s 最大合理速度
        private const double MIN_TIME_INTERVAL = 0.5; // 最小时间间隔（秒）
        private const int MAC_ADDRESS_LENGTH = 12;
        private const int IPV4_PREFIX_MAX = 32;
        private const int IPV6_PREFIX_MAX = 128;

        #endregion

        #region 格式化方法

        /// <summary>
        /// 格式化网速显示
        /// </summary>
        /// <param name="bytesPerSecond">每秒字节数</param>
        /// <returns>格式化后的速度字符串</returns>
        public static string FormatSpeed(double bytesPerSecond)
        {
            if (!IsValidSpeed(bytesPerSecond))
            {
                return "0.00 B/s";
            }

            var units = new[] { "B/s", "KB/s", "MB/s", "GB/s", "TB/s" };
            var unitIndex = 0;
            var speed = bytesPerSecond;

            while (speed >= 1024 && unitIndex < units.Length - 1)
            {
                speed /= 1024;
                unitIndex++;
            }

            return $"{speed:0.00} {units[unitIndex]}";
        }

        /// <summary>
        /// 格式化接口速度显示
        /// </summary>
        /// <param name="bitsPerSecond">每秒比特数</param>
        /// <returns>格式化后的速度字符串</returns>
        public static string FormatInterfaceSpeed(long bitsPerSecond)
        {
            if (bitsPerSecond <= 0)
            {
                return "未知";
            }

            var units = new[] { "bps", "Kbps", "Mbps", "Gbps", "Tbps" };
            var unitIndex = 0;
            var speed = (double)bitsPerSecond;

            while (speed >= 1000 && unitIndex < units.Length - 1)
            {
                speed /= 1000;
                unitIndex++;
            }

            return $"{speed:0.##} {units[unitIndex]}";
        }

        /// <summary>
        /// 格式化字节数显示
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化后的字节数字符串</returns>
        public static string FormatBytes(long bytes)
        {
            if (bytes < 0)
            {
                return "0 B";
            }

            var units = new[] { "B", "KB", "MB", "GB", "TB", "PB" };
            var unitIndex = 0;
            var size = (double)bytes;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:0.##} {units[unitIndex]}";
        }

        /// <summary>
        /// 格式化MAC地址
        /// </summary>
        /// <param name="physicalAddress">物理地址对象</param>
        /// <param name="separator">分隔符，默认为"-"</param>
        /// <returns>格式化后的MAC地址</returns>
        public static string FormatMacAddress(PhysicalAddress physicalAddress, string separator = "-")
        {
            if (physicalAddress == null)
            {
                return string.Empty;
            }

            var address = physicalAddress.ToString();
            if (string.IsNullOrEmpty(address) || address.Length != MAC_ADDRESS_LENGTH)
            {
                return address;
            }

            return string.Join(separator,
                Enumerable.Range(0, 6)
                    .Select(i => address.Substring(i * 2, 2))
                    .ToArray());
        }

        /// <summary>
        /// 格式化系统运行时间
        /// </summary>
        /// <param name="uptime">运行时间</param>
        /// <returns>格式化后的运行时间字符串</returns>
        public static string FormatUptime(TimeSpan uptime)
        {
            if (uptime.TotalSeconds < 0)
            {
                return "0秒";
            }

            var parts = new List<string>();

            if (uptime.Days > 0)
                parts.Add($"{uptime.Days}天");
            if (uptime.Hours > 0)
                parts.Add($"{uptime.Hours}小时");
            if (uptime.Minutes > 0)
                parts.Add($"{uptime.Minutes}分");
            if (uptime.Seconds > 0 || parts.Count == 0)
                parts.Add($"{uptime.Seconds}秒");

            return string.Join("", parts);
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证网速是否有效
        /// </summary>
        /// <param name="speed">网速值</param>
        /// <returns>是否有效</returns>
        public static bool IsValidSpeed(double speed)
        {
            return speed >= 0 &&
                   !double.IsNaN(speed) &&
                   !double.IsInfinity(speed) &&
                   speed < MAX_REASONABLE_SPEED;
        }

        /// <summary>
        /// 验证时间间隔是否足够
        /// </summary>
        /// <param name="timeInterval">时间间隔（秒）</param>
        /// <returns>是否足够</returns>
        public static bool IsValidTimeInterval(double timeInterval)
        {
            return timeInterval >= MIN_TIME_INTERVAL;
        }

        /// <summary>
        /// 验证字节数变化是否合理
        /// </summary>
        /// <param name="bytesChange">字节数变化</param>
        /// <returns>是否合理</returns>
        public static bool IsValidBytesChange(long bytesChange)
        {
            return bytesChange >= 0 && bytesChange < MAX_REASONABLE_SPEED;
        }

        #endregion

        #region 计算方法

        /// <summary>
        /// 计算网速
        /// </summary>
        /// <param name="currentBytes">当前字节数</param>
        /// <param name="previousBytes">之前字节数</param>
        /// <param name="timeInterval">时间间隔（秒）</param>
        /// <returns>网速（字节/秒）</returns>
        public static double CalculateSpeed(long currentBytes, long previousBytes, double timeInterval)
        {
            if (!IsValidTimeInterval(timeInterval))
            {
                return 0;
            }

            var bytesChange = currentBytes - previousBytes;
            if (!IsValidBytesChange(bytesChange))
            {
                return 0;
            }

            return bytesChange / timeInterval;
        }

        /// <summary>
        /// 计算子网掩码
        /// </summary>
        /// <param name="unicast">单播地址信息</param>
        /// <returns>子网掩码字符串</returns>
        public static string CalculateSubnetMask(UnicastIPAddressInformation unicast)
        {
            if (unicast == null)
            {
                return string.Empty;
            }

            try
            {
                // 优先使用IPv4Mask
                if (unicast.IPv4Mask != null)
                {
                    return unicast.IPv4Mask.ToString();
                }

                // 通过前缀长度计算
                var prefixLength = unicast.PrefixLength;
                if (prefixLength > 0 && prefixLength <= IPV4_PREFIX_MAX)
                {
                    var mask = (uint)(0xFFFFFFFF << (IPV4_PREFIX_MAX - prefixLength));
                    return new IPAddress(new byte[]
                    {
                        (byte)(mask >> 24),
                        (byte)(mask >> 16),
                        (byte)(mask >> 8),
                        (byte)mask
                    }).ToString();
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"计算子网掩码时发生错误: {ex.Message}");
            }

            return string.Empty;
        }

        #endregion

        #region 获取方法

        /// <summary>
        /// 获取网络接口类型描述
        /// </summary>
        /// <param name="type">接口类型</param>
        /// <returns>类型描述</returns>
        public static string GetInterfaceTypeDescription(NetworkInterfaceType type)
        {
            return type switch
            {
                NetworkInterfaceType.Ethernet => "以太网",
                NetworkInterfaceType.Wireless80211 => "Wi-Fi",
                NetworkInterfaceType.Loopback => "环回接口",
                NetworkInterfaceType.Ppp => "PPP",
                NetworkInterfaceType.Tunnel => "隧道",
                NetworkInterfaceType.Slip => "SLIP",
                NetworkInterfaceType.TokenRing => "令牌环",
                NetworkInterfaceType.Fddi => "FDDI",
                NetworkInterfaceType.BasicIsdn => "基本ISDN",
                NetworkInterfaceType.PrimaryIsdn => "主要ISDN",
                NetworkInterfaceType.GenericModem => "通用调制解调器",
                NetworkInterfaceType.FastEthernetT => "快速以太网T",
                NetworkInterfaceType.Isdn => "ISDN",
                NetworkInterfaceType.FastEthernetFx => "快速以太网FX",
                NetworkInterfaceType.Wman => "无线城域网",
                NetworkInterfaceType.Wwanpp => "无线广域网PP",
                NetworkInterfaceType.Wwanpp2 => "无线广域网PP2",
                _ => type.ToString()
            };
        }

        /// <summary>
        /// 获取系统运行时间
        /// </summary>
        /// <returns>系统运行时间</returns>
        public static TimeSpan GetSystemUptime()
        {
            return TimeSpan.FromMilliseconds(Environment.TickCount64);
        }

        /// <summary>
        /// 获取外网IP地址
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>外网IP信息</returns>
        public static async Task<ExternalIPInfo> GetExternalIPAsync(CancellationToken cancellationToken = default)
        {
            var externalInfo = new ExternalIPInfo();

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // 获取IPv4地址
            externalInfo.IPv4 = await GetIPv4AddressAsync(client, cancellationToken);

            // 获取IPv6地址
            externalInfo.IPv6 = await GetIPv6AddressAsync(client, cancellationToken);

            return externalInfo;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取IPv4外网地址
        /// </summary>
        private static async Task<string> GetIPv4AddressAsync(HttpClient client, CancellationToken cancellationToken)
        {
            var ipv4Services = new[]
            {
                "https://api.ipify.org",
                "https://icanhazip.com",
                "https://ipv4.icanhazip.com",
                "https://checkip.amazonaws.com"
            };

            foreach (var service in ipv4Services)
            {
                try
                {
                    var response = await client.GetStringAsync(service, cancellationToken);
                    var ip = response?.Trim();
                    if (!string.IsNullOrEmpty(ip) && IPAddress.TryParse(ip, out _))
                    {
                        return ip;
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"获取IPv4地址失败 ({service}): {ex.Message}");
                }
            }

            return "无法获取";
        }

        /// <summary>
        /// 获取IPv6外网地址
        /// </summary>
        private static async Task<string> GetIPv6AddressAsync(HttpClient client, CancellationToken cancellationToken)
        {
            var ipv6Services = new[]
            {
                "https://api6.ipify.org",
                "https://ipv6.icanhazip.com"
            };

            foreach (var service in ipv6Services)
            {
                try
                {
                    var response = await client.GetStringAsync(service, cancellationToken);
                    var ip = response?.Trim();
                    if (!string.IsNullOrEmpty(ip) && IPAddress.TryParse(ip, out _))
                    {
                        return ip;
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"获取IPv6地址失败 ({service}): {ex.Message}");
                }
            }

            return "不支持或无法获取";
        }

        #endregion
    }

    /// <summary>
    /// 外网IP信息
    /// </summary>
    public class ExternalIPInfo
    {
        public string IPv4 { get; set; } = string.Empty;
        public string IPv6 { get; set; } = string.Empty;
        public string ISP { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    /// <summary>
    /// 网速信息
    /// </summary>
    public class NetworkSpeed
    {
        public double DownloadSpeed { get; set; }
        public double UploadSpeed { get; set; }
        public double TotalSpeed => DownloadSpeed + UploadSpeed;

        public string DownloadSpeedText => NetworkHelper.FormatSpeed(DownloadSpeed);
        public string UploadSpeedText => NetworkHelper.FormatSpeed(UploadSpeed);
        public string TotalSpeedText => NetworkHelper.FormatSpeed(TotalSpeed);
    }
}