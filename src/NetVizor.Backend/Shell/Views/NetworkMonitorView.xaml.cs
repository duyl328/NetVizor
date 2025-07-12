using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Common.Logger;
using Common.Utils;

namespace Shell.Views
{
    /// <summary>
    /// Interaction logic for NetworkMonitorView.xaml
    /// </summary>
    public partial class NetworkMonitorView : UserControl
    {
        private DispatcherTimer updateTimer;
        private List<NetworkMonitorHelper1.NetworkInterface> interfaces;
        private const string SELECT_ALL_TEXT = "选择全部";

        public NetworkMonitorView()
        {
            InitializeComponent();
            Log.Info("NetworkMonitorControl 初始化");

            // 初始化定时器
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(1); // 每秒更新一次
            updateTimer.Tick += UpdateTimer_Tick;

            // 控件加载完成后初始化
            Loaded += NetworkMonitorControl_Loaded;
            Unloaded += NetworkMonitorControl_Unloaded;

            // NewMethod();
        }

        private static async Task NewMethod()
        {
            // 获取详细信息
            var interfaces = EnhancedNetworkMonitor.GetDetailedNetworkInterfaces();
            var json = JsonHelper.ToJson(interfaces);
            Console.WriteLine(json);
// 获取完整报告
            var report = await EnhancedNetworkMonitor.GetNetworkStatusReportAsync();
            Console.WriteLine(report);

// 获取外网IP
            var externalIP = await EnhancedNetworkMonitor.GetExternalIPAsync();
            Console.WriteLine(externalIP);
        }

        private void NetworkMonitorControl_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Info("NetworkMonitorControl 加载完成");
            LoadNetworkInterfaces();
            updateTimer.Start();
        }

        private void NetworkMonitorControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Log.Info("NetworkMonitorControl 卸载");
            updateTimer.Stop();
        }

        private void LoadNetworkInterfaces()
        {
            try
            {
                // 获取所有网络接口
                interfaces = NetworkMonitorHelper1.GetNetworkInterfaces();

                // 创建下拉框项目列表
                var comboItems = new List<object>();

                // 添加"选择全部"选项
                comboItems.Add(new
                {
                    Name = SELECT_ALL_TEXT,
                    Description = "显示所有网络接口的总网速",
                    TypeDescription = "",
                    IsConnected = true
                });

                // 添加所有非环回接口
                foreach (var iface in interfaces)
                {
                    // 添加所有接口到下拉框（环回接口已经在工具类中过滤）
                    comboItems.Add(iface);
                }

                // 更新下拉框
                cmbInterfaces.ItemsSource = comboItems;

                // 默认选择第一项（选择全部）
                if (comboItems.Count > 0)
                {
                    cmbInterfaces.SelectedIndex = 0;
                }
                else
                {
                    txtStatus.Text = "未找到可用的网络接口";
                    Log.Warning("未找到任何网络接口");
                }
            }
            catch (Exception ex)
            {
                Log.Error4Ctx($"加载网络接口时发生错误: {ex.Message}");
                txtStatus.Text = $"加载失败: {ex.Message}";
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (cmbInterfaces.SelectedItem == null)
            {
                return;
            }

            try
            {
                dynamic selectedItem = cmbInterfaces.SelectedItem;
                NetworkMonitorHelper1.NetworkSpeed speed;

                if (selectedItem.Name == SELECT_ALL_TEXT)
                {
                    // 计算所有接口的总网速
                    speed = NetworkMonitorHelper1.CalculateTotalSpeed();
                    txtStatus.Text = "显示所有网络接口的总网速";
                }
                else
                {
                    // 计算指定接口的网速
                    var networkInterface = selectedItem as NetworkMonitorHelper1.NetworkInterface;
                    if (networkInterface != null)
                    {
                        speed = NetworkMonitorHelper1.CalculateSpeedByName(networkInterface.Name);

                        // 显示接口状态
                        string statusText = $"正在监控: {networkInterface.Name} ({networkInterface.TypeDescription})";
                        if (!networkInterface.IsConnected)
                        {
                            statusText += " - 未连接";
                        }

                        txtStatus.Text = statusText;
                    }
                    else
                    {
                        return;
                    }
                }

                // 更新界面显示
                Dispatcher.Invoke(() =>
                {
                    txtDownloadSpeed.Text = speed.DownloadSpeedText;
                    txtUploadSpeed.Text = speed.UploadSpeedText;
                    txtTotalSpeed.Text = speed.TotalSpeedText;
                });
            }
            catch (Exception ex)
            {
                Log.Error4Ctx($"更新网速时发生错误: {ex.Message}");
                txtStatus.Text = $"更新失败: {ex.Message}";
            }
        }

        private void cmbInterfaces_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Info("用户切换了网络接口选择");

            // 重置网速显示
            txtDownloadSpeed.Text = "0.00 B/s";
            txtUploadSpeed.Text = "0.00 B/s";
            txtTotalSpeed.Text = "0.00 B/s";

            // 重置统计数据，以便立即开始新的统计
            NetworkMonitorHelper1.ResetStatistics();

            if (cmbInterfaces.SelectedItem != null)
            {
                dynamic selectedItem = cmbInterfaces.SelectedItem;
                Log.Info($"选择了: {selectedItem.Name}");
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("用户点击了刷新按钮");

            // 停止定时器
            updateTimer.Stop();

            // 重置统计数据
            NetworkMonitorHelper1.ResetStatistics();

            // 重新加载接口
            LoadNetworkInterfaces();

            // 重启定时器
            updateTimer.Start();

            txtStatus.Text = "接口列表已刷新";
        }
    }
}