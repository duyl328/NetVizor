using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using Common.Logger;
using Common.Utils;

namespace Shell.ViewModel;

public class NetworkMonitorViewModel : INotifyPropertyChanged
{
    private readonly DispatcherTimer _updateTimer;
    private List<NetworkMonitorHelper1.NetworkInterface> _interfaces;
    private const string SELECT_ALL_TEXT = "选择全部";
    private readonly bool _isDesignMode;

    #region Properties

    private ObservableCollection<NetworkInterfaceItem> _networkInterfaces;

    public ObservableCollection<NetworkInterfaceItem> NetworkInterfaces
    {
        get => _networkInterfaces;
        set
        {
            _networkInterfaces = value;
            OnPropertyChanged();
        }
    }

    private NetworkInterfaceItem _selectedInterface;

    public NetworkInterfaceItem SelectedInterface
    {
        get => _selectedInterface;
        set
        {
            _selectedInterface = value;
            OnPropertyChanged();
            OnSelectedInterfaceChanged();
        }
    }

    private string _downloadSpeed = "0.00 B/s";

    public string DownloadSpeed
    {
        get => _downloadSpeed;
        set
        {
            _downloadSpeed = value;
            OnPropertyChanged();
        }
    }

    private string _uploadSpeed = "0.00 B/s";

    public string UploadSpeed
    {
        get => _uploadSpeed;
        set
        {
            _uploadSpeed = value;
            OnPropertyChanged();
        }
    }

    private string _totalSpeed = "0.00 B/s";

    public string TotalSpeed
    {
        get => _totalSpeed;
        set
        {
            _totalSpeed = value;
            OnPropertyChanged();
        }
    }

    private string _statusText = "请选择一个网络接口";

    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; }

    #endregion

    #region Constructor

    public NetworkMonitorViewModel()
    {
        // 检查是否在设计模式
        _isDesignMode =
            System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());

        NetworkInterfaces = new ObservableCollection<NetworkInterfaceItem>();
        RefreshCommand = new RelayCommand(RefreshNetworkInterfaces);

        if (_isDesignMode)
        {
            // 设计时数据
            LoadDesignTimeData();
        }
        else
        {
            // 运行时数据
            Log.Info("NetworkMonitorViewModel 初始化");

            // 初始化定时器
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(1);
            _updateTimer.Tick += UpdateTimer_Tick;

            // 初始化数据
            LoadNetworkInterfaces();
            _updateTimer.Start();
        }
    }

    #endregion

    #region Methods

    private void LoadDesignTimeData()
    {
        // 设计时假数据
        NetworkInterfaces.Clear();

        // 添加"选择全部"选项
        NetworkInterfaces.Add(new NetworkInterfaceItem
        {
            Name = SELECT_ALL_TEXT,
            Description = "显示所有网络接口的总网速",
            TypeDescription = "",
            IsConnected = true,
            IsSelectAll = true
        });

        // 添加假的网络接口
        NetworkInterfaces.Add(new NetworkInterfaceItem
        {
            Name = "以太网",
            Description = "Realtek PCIe GBE Family Controller",
            TypeDescription = "以太网",
            IsConnected = true,
            IsSelectAll = false
        });

        NetworkInterfaces.Add(new NetworkInterfaceItem
        {
            Name = "Wi-Fi",
            Description = "Intel(R) Wi-Fi 6 AX201 160MHz",
            TypeDescription = "无线网络",
            IsConnected = true,
            IsSelectAll = false
        });

        NetworkInterfaces.Add(new NetworkInterfaceItem
        {
            Name = "蓝牙网络连接",
            Description = "Bluetooth Device (Personal Area Network)",
            TypeDescription = "蓝牙",
            IsConnected = false,
            IsSelectAll = false
        });

        NetworkInterfaces.Add(new NetworkInterfaceItem
        {
            Name = "VMware Network Adapter",
            Description = "VMware Virtual Ethernet Adapter",
            TypeDescription = "虚拟网卡",
            IsConnected = true,
            IsSelectAll = false
        });

        // 设置默认选中项
        SelectedInterface = NetworkInterfaces[1]; // 选中以太网

        // 设置假的网速数据
        DownloadSpeed = "15.67 MB/s";
        UploadSpeed = "2.34 MB/s";
        TotalSpeed = "18.01 MB/s";
        StatusText = "正在监控: 以太网 (以太网) - 已连接";
    }

    private void LoadNetworkInterfaces()
    {
        if (_isDesignMode)
        {
            LoadDesignTimeData();
            return;
        }

        try
        {
            // 获取所有网络接口
            _interfaces = NetworkMonitorHelper1.GetNetworkInterfaces();

            // 清除现有项目
            NetworkInterfaces.Clear();

            // 添加"选择全部"选项
            NetworkInterfaces.Add(new NetworkInterfaceItem
            {
                Name = SELECT_ALL_TEXT,
                Description = "显示所有网络接口的总网速",
                TypeDescription = "",
                IsConnected = true,
                IsSelectAll = true
            });

            // 添加所有非环回接口
            foreach (var iface in _interfaces)
            {
                NetworkInterfaces.Add(new NetworkInterfaceItem
                {
                    Name = iface.Name,
                    Description = iface.Description,
                    TypeDescription = iface.TypeDescription,
                    IsConnected = iface.IsConnected,
                    IsSelectAll = false,
                    NetworkInterface = iface
                });
            }

            // 默认选择第一项（选择全部）
            if (NetworkInterfaces.Count > 0)
            {
                SelectedInterface = NetworkInterfaces[0];
            }
            else
            {
                StatusText = "未找到可用的网络接口";
                Log.Warning("未找到任何网络接口");
            }
        }
        catch (Exception ex)
        {
            Log.Error4Ctx($"加载网络接口时发生错误: {ex.Message}");
            StatusText = $"加载失败: {ex.Message}";
        }
    }

    private void OnSelectedInterfaceChanged()
    {
        if (SelectedInterface == null) return;

        if (_isDesignMode)
        {
            // 设计时假数据更新
            UpdateDesignTimeSpeed();
            return;
        }

        Log.Info($"用户切换了网络接口选择: {SelectedInterface.Name}");

        // 重置网速显示
        DownloadSpeed = "0.00 B/s";
        UploadSpeed = "0.00 B/s";
        TotalSpeed = "0.00 B/s";

        // 重置统计数据，以便立即开始新的统计
        NetworkMonitorHelper1.ResetStatistics();
    }

    private void UpdateDesignTimeSpeed()
    {
        if (SelectedInterface == null) return;

        // 根据选择的接口显示不同的假数据
        if (SelectedInterface.IsSelectAll)
        {
            DownloadSpeed = "25.89 MB/s";
            UploadSpeed = "3.45 MB/s";
            TotalSpeed = "29.34 MB/s";
            StatusText = "显示所有网络接口的总网速";
        }
        else
        {
            switch (SelectedInterface.Name)
            {
                case "以太网":
                    DownloadSpeed = "15.67 MB/s";
                    UploadSpeed = "2.34 MB/s";
                    TotalSpeed = "18.01 MB/s";
                    StatusText = "正在监控: 以太网 (以太网) - 已连接";
                    break;
                case "Wi-Fi":
                    DownloadSpeed = "8.45 MB/s";
                    UploadSpeed = "1.23 MB/s";
                    TotalSpeed = "9.68 MB/s";
                    StatusText = "正在监控: Wi-Fi (无线网络) - 已连接";
                    break;
                case "蓝牙网络连接":
                    DownloadSpeed = "0.00 B/s";
                    UploadSpeed = "0.00 B/s";
                    TotalSpeed = "0.00 B/s";
                    StatusText = "正在监控: 蓝牙网络连接 (蓝牙) - 未连接";
                    break;
                case "VMware Network Adapter":
                    DownloadSpeed = "1.77 MB/s";
                    UploadSpeed = "0.88 KB/s";
                    TotalSpeed = "1.78 MB/s";
                    StatusText = "正在监控: VMware Network Adapter (虚拟网卡) - 已连接";
                    break;
                default:
                    DownloadSpeed = "0.00 B/s";
                    UploadSpeed = "0.00 B/s";
                    TotalSpeed = "0.00 B/s";
                    StatusText = $"正在监控: {SelectedInterface.Name}";
                    break;
            }
        }
    }

    private void UpdateTimer_Tick(object sender, EventArgs e)
    {
        if (_isDesignMode) return;

        if (SelectedInterface == null) return;

        try
        {
            NetworkMonitorHelper1.NetworkSpeed speed;

            if (SelectedInterface.IsSelectAll)
            {
                // 计算所有接口的总网速
                speed = NetworkMonitorHelper1.CalculateTotalSpeed();
                StatusText = "显示所有网络接口的总网速";
            }
            else
            {
                // 计算指定接口的网速
                var networkInterface = SelectedInterface.NetworkInterface;
                if (networkInterface != null)
                {
                    speed = NetworkMonitorHelper1.CalculateSpeedByName(networkInterface.Name);

                    // 显示接口状态
                    string statusText = $"正在监控: {networkInterface.Name} ({networkInterface.TypeDescription})";
                    if (!networkInterface.IsConnected)
                    {
                        statusText += " - 未连接";
                    }

                    StatusText = statusText;
                }
                else
                {
                    return;
                }
            }

            // 更新界面显示
            DownloadSpeed = speed.DownloadSpeedText;
            UploadSpeed = speed.UploadSpeedText;
            TotalSpeed = speed.TotalSpeedText;
        }
        catch (Exception ex)
        {
            Log.Error4Ctx($"更新网速时发生错误: {ex.Message}");
            StatusText = $"更新失败: {ex.Message}";
        }
    }

    private void RefreshNetworkInterfaces()
    {
        if (_isDesignMode)
        {
            LoadDesignTimeData();
            StatusText = "接口列表已刷新 (设计时数据)";
            return;
        }

        Log.Info("用户点击了刷新按钮");

        // 停止定时器
        _updateTimer.Stop();

        // 重置统计数据
        NetworkMonitorHelper1.ResetStatistics();

        // 重新加载接口
        LoadNetworkInterfaces();

        // 重启定时器
        _updateTimer.Start();

        StatusText = "接口列表已刷新";
    }

    // 测试用的异步方法
    private static async Task TestNetworkMethods()
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

    #endregion

    #region INotifyPropertyChanged Implementation

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        if (_isDesignMode) return;

        Log.Info("NetworkMonitorViewModel 销毁");
        _updateTimer?.Stop();
    }

    #endregion
}

#region Helper Classes

public class NetworkInterfaceItem
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string TypeDescription { get; set; }
    public bool IsConnected { get; set; }
    public bool IsSelectAll { get; set; }
    public NetworkMonitorHelper1.NetworkInterface NetworkInterface { get; set; }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;

    public RelayCommand(Action execute, Func<bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object parameter) => _execute();
}

#endregion
