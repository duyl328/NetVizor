using System.Windows;
using System.Windows.Controls;
using Common.Logger;
using Shell.ViewModel;

namespace Shell.UserControls
{
    /// <summary>
    /// Interaction logic for NetworkMonitorView.xaml
    /// </summary>
    public partial class NetworkMonitorView : UserControl
    {
        private NetworkMonitorViewModel _viewModel;

        public NetworkMonitorView()
        {
            InitializeComponent();
            Log.Info("NetworkMonitorView 初始化");

            // 创建并设置ViewModel
            _viewModel = new NetworkMonitorViewModel();
            DataContext = _viewModel;

            // 订阅控件生命周期事件
            Loaded += NetworkMonitorView_Loaded;
            Unloaded += NetworkMonitorView_Unloaded;
        }

        private void NetworkMonitorView_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Info("NetworkMonitorView 加载完成");
        }

        private void NetworkMonitorView_Unloaded(object sender, RoutedEventArgs e)
        {
            Log.Info("NetworkMonitorView 卸载");
            // 释放ViewModel资源
            _viewModel?.Dispose();
        }
    }
}
