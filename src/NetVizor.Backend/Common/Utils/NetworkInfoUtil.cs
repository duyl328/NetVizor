using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using Common.Logger;

namespace Common.Utils
{
    /// <summary>
    /// 网络监控帮助类 - 使用 .NET 内置的 NetworkInterface 类
    /// </summary>
    public class NetworkMonitorHelper
    {
        #region 公共类和方法

        /// <summary>
        /// 网络接口信息
        /// </summary>
        public class NetworkInterface
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Id { get; set; }
            public uint Index { get; set; }
            public long BytesReceived { get; set; }
            public long BytesSent { get; set; }
            public bool IsConnected { get; set; }
            public NetworkInterfaceType Type { get; set; }
            public string TypeDescription { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// 网速信息
        /// </summary>
        public class NetworkSpeed
        {
            public double DownloadSpeed { get; set; } // bytes per second
            public double UploadSpeed { get; set; } // bytes per second
            public double TotalSpeed => DownloadSpeed + UploadSpeed;

            // 格式化显示
            public string DownloadSpeedText => FormatSpeed(DownloadSpeed);
            public string UploadSpeedText => FormatSpeed(UploadSpeed);
            public string TotalSpeedText => FormatSpeed(TotalSpeed);

            private string FormatSpeed(double bytesPerSecond)
            {
                // 防止显示负数或异常大的数值
                if (bytesPerSecond < 0 || double.IsNaN(bytesPerSecond) || double.IsInfinity(bytesPerSecond))
                {
                    return "0.00 B/s";
                }

                string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s" };
                int order = 0;
                while (bytesPerSecond >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    bytesPerSecond = bytesPerSecond / 1024;
                }

                return $"{bytesPerSecond:0.00} {sizes[order]}";
            }
        }

        // 缓存上次的统计数据
        private static Dictionary<string, (long bytesReceived, long bytesSent, DateTime updateTime)> previousStats =
            new Dictionary<string, (long, long, DateTime)>();

        private static bool isFirstRun = true;

        /// <summary>
        /// 获取接口类型描述
        /// </summary>
        private static string GetInterfaceTypeDescription(NetworkInterfaceType type)
        {
            switch (type)
            {
                case NetworkInterfaceType.Ethernet:
                    return "以太网";
                case NetworkInterfaceType.Wireless80211:
                    return "Wi-Fi";
                case NetworkInterfaceType.Loopback:
                    return "环回接口";
                case NetworkInterfaceType.Ppp:
                    return "PPP";
                case NetworkInterfaceType.Tunnel:
                    return "隧道";
                default:
                    return type.ToString();
            }
        }

        /// <summary>
        /// 获取所有网络接口
        /// </summary>
        public static List<NetworkInterface> GetNetworkInterfaces()
        {
            var interfaces = new List<NetworkInterface>();

            try
            {
                var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

                foreach (var ni in networkInterfaces)
                {
                    // 跳过环回接口
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        Log.Info($"跳过环回接口: {ni.Name}");
                        continue;
                    }

                    // 获取IPv4统计信息
                    IPv4InterfaceStatistics stats = null;
                    try
                    {
                        stats = ni.GetIPv4Statistics();
                    }
                    catch (Exception ex)
                    {
                        Log.Error4Ctx($"无法获取接口 {ni.Name} 的统计信息: {ex.Message}");
                        continue;
                    }

                    var networkInterface = new NetworkInterface
                    {
                        Name = ni.Name,
                        Description = ni.Description,
                        Id = ni.Id,
                        Index = 0, // NetworkInterface 类不提供 Index，使用 Id 作为唯一标识
                        BytesReceived = stats.BytesReceived,
                        BytesSent = stats.BytesSent,
                        IsConnected = ni.OperationalStatus == OperationalStatus.Up,
                        Type = ni.NetworkInterfaceType,
                        TypeDescription = GetInterfaceTypeDescription(ni.NetworkInterfaceType)
                    };

                    interfaces.Add(networkInterface);
                }
            }
            catch (Exception ex)
            {
                Log.Error4Ctx($"获取网络接口时发生错误: {ex.Message}");
                Log.Error4Ctx($"错误堆栈: {ex.StackTrace}");
            }

            return interfaces;
        }

        /// <summary>
        /// 计算指定网络接口的网速
        /// </summary>
        public static NetworkSpeed CalculateSpeed(uint interfaceIndex)
        {
            // 由于使用了 NetworkInterface 类，这里改为使用接口名称查找
            var interfaces = GetNetworkInterfaces();
            return CalculateSpeedByName(interfaces, interfaceIndex.ToString());
        }

        /// <summary>
        /// 根据接口名称计算网速
        /// </summary>
        public static NetworkSpeed CalculateSpeedByName(string interfaceName)
        {
            var interfaces = GetNetworkInterfaces();
            return CalculateSpeedByName(interfaces, interfaceName);
        }

        private static NetworkSpeed CalculateSpeedByName(List<NetworkInterface> currentInterfaces, string interfaceName)
        {
            Log.Info($"计算接口 {interfaceName} 的网速...");

            var currentInterface =
                currentInterfaces.FirstOrDefault(i => i.Name == interfaceName || i.Id == interfaceName);

            if (currentInterface == null)
            {
                Log.Warning($"未找到接口 {interfaceName}");
                return new NetworkSpeed();
            }

            var now = DateTime.Now;
            var speed = new NetworkSpeed();
            string key = currentInterface.Id;

            // 如果是第一次运行或没有该接口的历史数据，只保存数据
            if (isFirstRun || !previousStats.ContainsKey(key))
            {
                previousStats[key] = (currentInterface.BytesReceived, currentInterface.BytesSent, now);
                Log.Info($"接口 {interfaceName} 第一次记录数据");
                if (isFirstRun) isFirstRun = false;
                return speed;
            }

            var (prevBytesReceived, prevBytesSent, lastUpdateTime) = previousStats[key];
            var timeDiff = (now - lastUpdateTime).TotalSeconds;

            Log.Debug($"时间差: {timeDiff}秒");

            if (timeDiff > 0.5) // 至少0.5秒的间隔
            {
                // 计算字节差
                long downloadBytes = currentInterface.BytesReceived - prevBytesReceived;
                long uploadBytes = currentInterface.BytesSent - prevBytesSent;

                Log.Info($"下载字节差: {downloadBytes}, 上传字节差: {uploadBytes}");

                // 防止计数器重置或异常值
                if (downloadBytes >= 0 && downloadBytes < 1000000000) // 小于1GB/s
                {
                    speed.DownloadSpeed = downloadBytes / timeDiff;
                }

                if (uploadBytes >= 0 && uploadBytes < 1000000000) // 小于1GB/s
                {
                    speed.UploadSpeed = uploadBytes / timeDiff;
                }

                Log.Info($"接口 {interfaceName} - 下载: {speed.DownloadSpeedText}, 上传: {speed.UploadSpeedText}");

                // 更新缓存
                previousStats[key] = (currentInterface.BytesReceived, currentInterface.BytesSent, now);
            }

            return speed;
        }

        /// <summary>
        /// 计算所有网络接口的总网速
        /// </summary>
        public static NetworkSpeed CalculateTotalSpeed()
        {
            var currentInterfaces = GetNetworkInterfaces();
            var now = DateTime.Now;
            var totalSpeed = new NetworkSpeed();

            // 如果是第一次运行，只保存数据
            if (isFirstRun)
            {
                foreach (var currentInterface in currentInterfaces)
                {
                    previousStats[currentInterface.Id] =
                        (currentInterface.BytesReceived, currentInterface.BytesSent, now);
                }

                isFirstRun = false;
                Log.Info("第一次运行，初始化数据");
                return totalSpeed;
            }

            foreach (var currentInterface in currentInterfaces)
            {
                string key = currentInterface.Id;

                if (previousStats.ContainsKey(key))
                {
                    var (prevBytesReceived, prevBytesSent, lastUpdateTime) = previousStats[key];
                    var timeDiff = (now - lastUpdateTime).TotalSeconds;

                    if (timeDiff > 0.5) // 至少0.5秒的间隔
                    {
                        long downloadBytes = currentInterface.BytesReceived - prevBytesReceived;
                        long uploadBytes = currentInterface.BytesSent - prevBytesSent;

                        // 防止计数器重置或异常值
                        if (downloadBytes >= 0 && downloadBytes < 1000000000) // 小于1GB/s
                        {
                            totalSpeed.DownloadSpeed += downloadBytes / timeDiff;
                        }

                        if (uploadBytes >= 0 && uploadBytes < 1000000000) // 小于1GB/s
                        {
                            totalSpeed.UploadSpeed += uploadBytes / timeDiff;
                        }

                        // 更新缓存
                        previousStats[key] = (currentInterface.BytesReceived, currentInterface.BytesSent, now);
                    }
                }
                else
                {
                    // 新接口，添加到缓存
                    previousStats[key] = (currentInterface.BytesReceived, currentInterface.BytesSent, now);
                }
            }

            Log.Info($"总网速 - 下载: {totalSpeed.DownloadSpeedText}, 上传: {totalSpeed.UploadSpeedText}");

            return totalSpeed;
        }

        /// <summary>
        /// 重置统计数据
        /// </summary>
        public static void ResetStatistics()
        {
            Log.Info("重置网络统计数据");
            previousStats.Clear();
            isFirstRun = true;
        }

        #endregion
    }
}
