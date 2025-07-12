using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using Common.Logger;

namespace Common.Utils
{
    /// <summary>
    /// 基础网络监控帮助类 - 专注于网速计算
    /// </summary>
    public static class BasicNetworkMonitor
    {
        #region 数据模型

        /// <summary>
        /// 基础网络接口信息
        /// </summary>
        public class BasicNetworkInterface
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Id { get; set; } = string.Empty;
            public long BytesReceived { get; set; }
            public long BytesSent { get; set; }
            public bool IsConnected { get; set; }
            public NetworkInterfaceType Type { get; set; }
            public string TypeDescription { get; set; } = string.Empty;

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// 网络统计快照
        /// </summary>
        private class NetworkSnapshot
        {
            public long BytesReceived { get; set; }
            public long BytesSent { get; set; }
            public DateTime Timestamp { get; set; }
        }

        #endregion

        #region 私有字段

        // 使用线程安全的字典存储历史统计数据
        private static readonly ConcurrentDictionary<string, NetworkSnapshot> PreviousStats = new();
        private static readonly object LockObject = new();
        private static volatile bool _isInitialized = false;

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取所有网络接口
        /// </summary>
        /// <param name="includeLoopback">是否包含环回接口</param>
        /// <returns>网络接口列表</returns>
        public static List<BasicNetworkInterface> GetNetworkInterfaces(bool includeLoopback = false)
        {
            var interfaces = new List<BasicNetworkInterface>();

            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var ni in networkInterfaces)
                {
                    // 根据参数决定是否跳过环回接口
                    if (!includeLoopback && ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        continue;
                    }

                    var basicInterface = CreateBasicInterface(ni);
                    if (basicInterface != null)
                    {
                        interfaces.Add(basicInterface);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error4Ctx($"获取网络接口时发生错误: {ex.Message}");
            }

            return interfaces;
        }

        /// <summary>
        /// 根据接口名称计算网速
        /// </summary>
        /// <param name="interfaceName">接口名称</param>
        /// <returns>网速信息</returns>
        public static NetworkSpeed CalculateSpeedByName(string interfaceName)
        {
            if (string.IsNullOrWhiteSpace(interfaceName))
            {
                Log.Warning("接口名称不能为空");
                return new NetworkSpeed();
            }

            var interfaces = GetNetworkInterfaces();
            var targetInterface = interfaces.FirstOrDefault(i =>
                string.Equals(i.Name, interfaceName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(i.Id, interfaceName, StringComparison.OrdinalIgnoreCase));

            if (targetInterface == null)
            {
                Log.Warning($"未找到接口: {interfaceName}");
                return new NetworkSpeed();
            }

            return CalculateSpeed(targetInterface);
        }

        /// <summary>
        /// 根据接口ID计算网速
        /// </summary>
        /// <param name="interfaceId">接口ID</param>
        /// <returns>网速信息</returns>
        public static NetworkSpeed CalculateSpeedById(string interfaceId)
        {
            return CalculateSpeedByName(interfaceId);
        }

        /// <summary>
        /// 计算所有网络接口的总网速
        /// </summary>
        /// <returns>总网速信息</returns>
        public static NetworkSpeed CalculateTotalSpeed()
        {
            var currentInterfaces = GetNetworkInterfaces();
            var now = DateTime.Now;
            var totalSpeed = new NetworkSpeed();

            // 确保初始化
            EnsureInitialized(currentInterfaces, now);

            foreach (var currentInterface in currentInterfaces)
            {
                var speed = CalculateSpeed(currentInterface, now);
                totalSpeed.DownloadSpeed += speed.DownloadSpeed;
                totalSpeed.UploadSpeed += speed.UploadSpeed;
            }

            Log.Info($"总网速 - 下载: {totalSpeed.DownloadSpeedText}, 上传: {totalSpeed.UploadSpeedText}");
            return totalSpeed;
        }

        /// <summary>
        /// 重置统计数据
        /// </summary>
        public static void ResetStatistics()
        {
            lock (LockObject)
            {
                Log.Info("重置网络统计数据");
                PreviousStats.Clear();
                _isInitialized = false;
            }
        }

        /// <summary>
        /// 获取接口的历史统计数据
        /// </summary>
        /// <param name="interfaceId">接口ID</param>
        /// <returns>是否存在历史数据</returns>
        public static bool HasHistoricalData(string interfaceId)
        {
            return !string.IsNullOrEmpty(interfaceId) && PreviousStats.ContainsKey(interfaceId);
        }

        /// <summary>
        /// 获取已连接的网络接口
        /// </summary>
        /// <returns>已连接的网络接口列表</returns>
        public static List<BasicNetworkInterface> GetConnectedNetworkInterfaces()
        {
            return GetNetworkInterfaces()
                .Where(i => i.IsConnected)
                .ToList();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 创建基础网络接口信息
        /// </summary>
        /// <param name="ni">网络接口</param>
        /// <returns>基础网络接口信息</returns>
        private static BasicNetworkInterface CreateBasicInterface(NetworkInterface ni)
        {
            try
            {
                // 获取IPv4统计信息
                var stats = ni.GetIPv4Statistics();

                return new BasicNetworkInterface
                {
                    Name = ni.Name,
                    Description = ni.Description,
                    Id = ni.Id,
                    BytesReceived = stats.BytesReceived,
                    BytesSent = stats.BytesSent,
                    IsConnected = ni.OperationalStatus == OperationalStatus.Up,
                    Type = ni.NetworkInterfaceType,
                    TypeDescription = NetworkHelper.GetInterfaceTypeDescription(ni.NetworkInterfaceType)
                };
            }
            catch (Exception ex)
            {
                Log.Warning($"创建接口 {ni.Name} 基础信息时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 计算指定接口的网速
        /// </summary>
        /// <param name="currentInterface">当前接口信息</param>
        /// <param name="now">当前时间（可选）</param>
        /// <returns>网速信息</returns>
        private static NetworkSpeed CalculateSpeed(BasicNetworkInterface currentInterface, DateTime? now = null)
        {
            if (currentInterface == null)
            {
                return new NetworkSpeed();
            }

            var timestamp = now ?? DateTime.Now;
            var key = currentInterface.Id;
            var speed = new NetworkSpeed();

            Log.Debug($"计算接口 {currentInterface.Name} 的网速...");

            // 确保初始化
            if (!_isInitialized)
            {
                EnsureInitialized(new[] { currentInterface }, timestamp);
                return speed;
            }

            // 获取或创建前一次的快照
            var currentSnapshot = new NetworkSnapshot
            {
                BytesReceived = currentInterface.BytesReceived,
                BytesSent = currentInterface.BytesSent,
                Timestamp = timestamp
            };

            if (PreviousStats.TryGetValue(key, out var previousSnapshot))
            {
                var timeDiff = (timestamp - previousSnapshot.Timestamp).TotalSeconds;

                if (NetworkHelper.IsValidTimeInterval(timeDiff))
                {
                    // 计算速度
                    speed.DownloadSpeed = NetworkHelper.CalculateSpeed(
                        currentSnapshot.BytesReceived,
                        previousSnapshot.BytesReceived,
                        timeDiff);

                    speed.UploadSpeed = NetworkHelper.CalculateSpeed(
                        currentSnapshot.BytesSent,
                        previousSnapshot.BytesSent,
                        timeDiff);

                    Log.Debug(
                        $"接口 {currentInterface.Name} - 下载: {speed.DownloadSpeedText}, 上传: {speed.UploadSpeedText}");
                }
                else
                {
                    Log.Debug($"时间间隔过短 ({timeDiff:F2}秒)，跳过速度计算");
                }
            }
            else
            {
                Log.Debug($"接口 {currentInterface.Name} 首次记录数据");
            }

            // 更新快照
            PreviousStats.AddOrUpdate(key, currentSnapshot, (k, v) => currentSnapshot);

            return speed;
        }

        /// <summary>
        /// 确保系统已初始化
        /// </summary>
        /// <param name="interfaces">接口列表</param>
        /// <param name="timestamp">时间戳</param>
        private static void EnsureInitialized(IEnumerable<BasicNetworkInterface> interfaces, DateTime timestamp)
        {
            if (_isInitialized)
            {
                return;
            }

            lock (LockObject)
            {
                if (_isInitialized)
                {
                    return;
                }

                Log.Info("初始化网络监控统计数据");

                foreach (var iface in interfaces)
                {
                    if (iface != null)
                    {
                        PreviousStats.TryAdd(iface.Id, new NetworkSnapshot
                        {
                            BytesReceived = iface.BytesReceived,
                            BytesSent = iface.BytesSent,
                            Timestamp = timestamp
                        });
                    }
                }

                _isInitialized = true;
                Log.Info($"已初始化 {PreviousStats.Count} 个网络接口的统计数据");
            }
        }

        /// <summary>
        /// 清理过期的统计数据
        /// </summary>
        /// <param name="maxAge">最大保留时间</param>
        public static void CleanupOldStatistics(TimeSpan maxAge)
        {
            var cutoffTime = DateTime.Now - maxAge;
            var expiredKeys = new List<string>();

            foreach (var kvp in PreviousStats)
            {
                if (kvp.Value.Timestamp < cutoffTime)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                PreviousStats.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                Log.Info($"清理了 {expiredKeys.Count} 个过期的网络统计记录");
            }
        }

        #endregion
    }
}