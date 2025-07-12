using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logger;

namespace Common.Utils
{
    /// <summary>
    /// 增强的网络监控帮助类 - 获取详细的网络信息
    /// </summary>
    public static class EnhancedNetworkMonitor
    {
        #region 数据模型

        /// <summary>
        /// 详细的网络接口信息
        /// </summary>
        public class DetailedNetworkInterface
        {
            // 基本信息
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Id { get; set; } = string.Empty;
            public bool IsConnected { get; set; }
            public NetworkInterfaceType Type { get; set; }
            public string TypeDescription { get; set; } = string.Empty;

            // 物理信息
            public string PhysicalAddress { get; set; } = string.Empty;
            public long Speed { get; set; }
            public string SpeedText { get; set; } = string.Empty;

            // IP配置
            public List<string> IPAddresses { get; set; } = new();
            public List<string> SubnetMasks { get; set; } = new();
            public List<string> Gateways { get; set; } = new();
            public List<string> DnsServers { get; set; } = new();
            public List<string> IPv6Addresses { get; set; } = new();

            // 统计信息
            public long BytesReceived { get; set; }
            public long BytesSent { get; set; }
            public long PacketsReceived { get; set; }
            public long PacketsSent { get; set; }
            public long ErrorsReceived { get; set; }
            public long ErrorsSent { get; set; }

            public override string ToString()
            {
                return $"{Name} - {Description}";
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取所有网络接口的详细信息
        /// </summary>
        /// <param name="includeLoopback">是否包含环回接口</param>
        /// <returns>网络接口列表</returns>
        public static List<DetailedNetworkInterface> GetDetailedNetworkInterfaces(bool includeLoopback = false)
        {
            var interfaces = new List<DetailedNetworkInterface>();

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

                    var detailedInterface = CreateDetailedInterface(ni);
                    if (detailedInterface != null)
                    {
                        interfaces.Add(detailedInterface);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error4Ctx($"获取网络接口详细信息时发生错误: {ex.Message}");
            }

            return interfaces;
        }

        /// <summary>
        /// 获取已连接的网络接口
        /// </summary>
        /// <returns>已连接的网络接口列表</returns>
        public static List<DetailedNetworkInterface> GetConnectedNetworkInterfaces()
        {
            return GetDetailedNetworkInterfaces()
                .Where(i => i.IsConnected)
                .ToList();
        }

        /// <summary>
        /// 根据名称获取网络接口
        /// </summary>
        /// <param name="name">接口名称</param>
        /// <returns>网络接口信息，未找到则返回null</returns>
        public static DetailedNetworkInterface GetNetworkInterfaceByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return GetDetailedNetworkInterfaces()
                .FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取外网IP地址
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>外网IP信息</returns>
        public static async Task<ExternalIPInfo> GetExternalIPAsync(CancellationToken cancellationToken = default)
        {
            return await NetworkHelper.GetExternalIPAsync(cancellationToken);
        }

        /// <summary>
        /// 获取当前主要网络接口的详细信息
        /// </summary>
        /// <returns>主要网络接口信息，如果没有则返回null</returns>
        public static DetailedNetworkInterface GetPrimaryNetworkInterface()
        {
            var connectedInterfaces = GetConnectedNetworkInterfaces();

            // 优先选择有网关的以太网接口
            var primaryInterface = connectedInterfaces
                .Where(i => i.Type == NetworkInterfaceType.Ethernet && i.Gateways.Any())
                .FirstOrDefault();

            // 如果没有以太网，选择有网关的Wi-Fi接口
            if (primaryInterface == null)
            {
                primaryInterface = connectedInterfaces
                    .Where(i => i.Type == NetworkInterfaceType.Wireless80211 && i.Gateways.Any())
                    .FirstOrDefault();
            }

            // 如果还没有，选择任何有IP地址的接口
            if (primaryInterface == null)
            {
                primaryInterface = connectedInterfaces
                    .Where(i => i.IPAddresses.Any())
                    .FirstOrDefault();
            }

            return primaryInterface;
        }

        /// <summary>
        /// 获取完整的网络状态报告
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>网络状态报告</returns>
        public static async Task<string> GetNetworkStatusReportAsync(CancellationToken cancellationToken = default)
        {
            var sb = new StringBuilder();

            try
            {
                var interfaces = GetConnectedNetworkInterfaces();
                var externalIP = await GetExternalIPAsync(cancellationToken);
                var uptime = NetworkHelper.GetSystemUptime();

                // 报告头部
                sb.AppendLine("=== 网络状态详细报告 ===");
                sb.AppendLine($"报告生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"系统运行时间: {NetworkHelper.FormatUptime(uptime)}");
                sb.AppendLine($"外网IPv4地址: {externalIP.IPv4}");
                sb.AppendLine($"外网IPv6地址: {externalIP.IPv6}");
                sb.AppendLine($"活跃网络接口数: {interfaces.Count}");
                sb.AppendLine();

                // 接口详情
                foreach (var iface in interfaces)
                {
                    AppendInterfaceInfo(sb, iface);
                }

                // 统计汇总
                AppendNetworkSummary(sb, interfaces);
            }
            catch (Exception ex)
            {
                Log.Error4Ctx($"生成网络状态报告时发生错误: {ex.Message}");
                sb.AppendLine($"生成报告时发生错误: {ex.Message}");
            }

            return sb.ToString();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 创建详细的网络接口信息
        /// </summary>
        /// <param name="ni">网络接口</param>
        /// <returns>详细网络接口信息</returns>
        private static DetailedNetworkInterface CreateDetailedInterface(NetworkInterface ni)
        {
            try
            {
                var detailedInterface = new DetailedNetworkInterface
                {
                    // 基本信息
                    Name = ni.Name,
                    Description = ni.Description,
                    Id = ni.Id,
                    IsConnected = ni.OperationalStatus == OperationalStatus.Up,
                    Type = ni.NetworkInterfaceType,
                    TypeDescription = NetworkHelper.GetInterfaceTypeDescription(ni.NetworkInterfaceType),

                    // 物理信息
                    PhysicalAddress = NetworkHelper.FormatMacAddress(ni.GetPhysicalAddress()),
                    Speed = ni.Speed,
                    SpeedText = NetworkHelper.FormatInterfaceSpeed(ni.Speed)
                };

                // 获取统计信息
                PopulateStatistics(detailedInterface, ni);

                // 获取IP配置信息
                PopulateIPConfiguration(detailedInterface, ni);

                return detailedInterface;
            }
            catch (Exception ex)
            {
                Log.Warning($"创建接口 {ni.Name} 的详细信息时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 填充统计信息
        /// </summary>
        /// <param name="detailedInterface">详细接口信息</param>
        /// <param name="ni">网络接口</param>
        private static void PopulateStatistics(DetailedNetworkInterface detailedInterface, NetworkInterface ni)
        {
            try
            {
                var stats = ni.GetIPv4Statistics();
                detailedInterface.BytesReceived = stats.BytesReceived;
                detailedInterface.BytesSent = stats.BytesSent;
                detailedInterface.PacketsReceived = stats.UnicastPacketsReceived;
                detailedInterface.PacketsSent = stats.UnicastPacketsSent;
                detailedInterface.ErrorsReceived = stats.IncomingPacketsWithErrors;
                detailedInterface.ErrorsSent = stats.OutgoingPacketsWithErrors;
            }
            catch (Exception ex)
            {
                Log.Warning($"获取接口 {ni.Name} 统计信息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 填充IP配置信息
        /// </summary>
        /// <param name="detailedInterface">详细接口信息</param>
        /// <param name="ni">网络接口</param>
        private static void PopulateIPConfiguration(DetailedNetworkInterface detailedInterface, NetworkInterface ni)
        {
            try
            {
                var ipProps = ni.GetIPProperties();

                // 处理单播地址
                foreach (var unicast in ipProps.UnicastAddresses)
                {
                    switch (unicast.Address.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            detailedInterface.IPAddresses.Add(unicast.Address.ToString());
                            var mask = NetworkHelper.CalculateSubnetMask(unicast);
                            if (!string.IsNullOrEmpty(mask))
                            {
                                detailedInterface.SubnetMasks.Add(mask);
                            }

                            break;
                        case AddressFamily.InterNetworkV6:
                            detailedInterface.IPv6Addresses.Add(unicast.Address.ToString());
                            break;
                    }
                }

                // 处理网关
                foreach (var gateway in ipProps.GatewayAddresses)
                {
                    if (gateway.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        detailedInterface.Gateways.Add(gateway.Address.ToString());
                    }
                }

                // 处理DNS服务器
                foreach (var dns in ipProps.DnsAddresses)
                {
                    if (dns.AddressFamily == AddressFamily.InterNetwork)
                    {
                        detailedInterface.DnsServers.Add(dns.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"获取接口 {ni.Name} IP配置时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加接口信息到报告
        /// </summary>
        /// <param name="sb">字符串构建器</param>
        /// <param name="iface">接口信息</param>
        private static void AppendInterfaceInfo(StringBuilder sb, DetailedNetworkInterface iface)
        {
            sb.AppendLine($"接口名称: {iface.Name}");
            sb.AppendLine($"接口描述: {iface.Description}");
            sb.AppendLine($"连接类型: {iface.TypeDescription}");
            sb.AppendLine($"接口速度: {iface.SpeedText}");
            sb.AppendLine($"物理地址: {iface.PhysicalAddress}");
            sb.AppendLine($"连接状态: {(iface.IsConnected ? "已连接" : "未连接")}");

            if (iface.IPAddresses.Any())
            {
                sb.AppendLine($"IPv4地址: {string.Join(", ", iface.IPAddresses)}");
            }

            if (iface.SubnetMasks.Any())
            {
                sb.AppendLine($"子网掩码: {string.Join(", ", iface.SubnetMasks)}");
            }

            if (iface.Gateways.Any())
            {
                sb.AppendLine($"默认网关: {string.Join(", ", iface.Gateways)}");
            }

            if (iface.DnsServers.Any())
            {
                sb.AppendLine($"DNS服务器: {string.Join(", ", iface.DnsServers)}");
            }

            if (iface.IPv6Addresses.Any())
            {
                sb.AppendLine($"IPv6地址: {string.Join(", ", iface.IPv6Addresses)}");
            }

            sb.AppendLine($"已接收: {NetworkHelper.FormatBytes(iface.BytesReceived)} ({iface.PacketsReceived:N0} 包)");
            sb.AppendLine($"已发送: {NetworkHelper.FormatBytes(iface.BytesSent)} ({iface.PacketsSent:N0} 包)");

            if (iface.ErrorsReceived > 0 || iface.ErrorsSent > 0)
            {
                sb.AppendLine($"错误统计: 接收错误 {iface.ErrorsReceived:N0}, 发送错误 {iface.ErrorsSent:N0}");
            }

            sb.AppendLine();
        }

        /// <summary>
        /// 添加网络汇总信息
        /// </summary>
        /// <param name="sb">字符串构建器</param>
        /// <param name="interfaces">接口列表</param>
        private static void AppendNetworkSummary(StringBuilder sb, List<DetailedNetworkInterface> interfaces)
        {
            if (!interfaces.Any())
            {
                return;
            }

            sb.AppendLine("=== 网络统计汇总 ===");

            var totalBytesReceived = interfaces.Sum(i => i.BytesReceived);
            var totalBytesSent = interfaces.Sum(i => i.BytesSent);
            var totalPacketsReceived = interfaces.Sum(i => i.PacketsReceived);
            var totalPacketsSent = interfaces.Sum(i => i.PacketsSent);
            var totalErrors = interfaces.Sum(i => i.ErrorsReceived + i.ErrorsSent);

            sb.AppendLine($"总接收流量: {NetworkHelper.FormatBytes(totalBytesReceived)}");
            sb.AppendLine($"总发送流量: {NetworkHelper.FormatBytes(totalBytesSent)}");
            sb.AppendLine($"总数据包: 接收 {totalPacketsReceived:N0}, 发送 {totalPacketsSent:N0}");

            if (totalErrors > 0)
            {
                sb.AppendLine($"总错误数: {totalErrors:N0}");
            }

            var ethernetCount = interfaces.Count(i => i.Type == NetworkInterfaceType.Ethernet);
            var wifiCount = interfaces.Count(i => i.Type == NetworkInterfaceType.Wireless80211);

            sb.AppendLine(
                $"接口类型分布: 以太网 {ethernetCount}, Wi-Fi {wifiCount}, 其他 {interfaces.Count - ethernetCount - wifiCount}");
        }

        #endregion
    }
}