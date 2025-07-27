using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Common.Logger;
using Shell.ViewModel;
using Shell.Models;

namespace Shell.UserControls
{
    /// <summary>
    /// Interaction logic for NetworkMonitorView.xaml
    /// </summary>
    public partial class NetworkMonitorView : System.Windows.Controls.UserControl
    {
        private NetworkMonitorViewModel _viewModel;
        private NetViewSettings _settings;

        public NetworkMonitorView()
        {
            InitializeComponent();
            Log.Info("NetworkMonitorView 初始化");

            // 创建并设置ViewModel
            _viewModel = new NetworkMonitorViewModel();
            DataContext = _viewModel;

            // 获取设置实例
            _settings = NetViewSettings.Instance;

            // 订阅控件生命周期事件
            Loaded += NetworkMonitorView_Loaded;
            Unloaded += NetworkMonitorView_Unloaded;

            // 订阅设置变化事件
            _settings.PropertyChanged += Settings_PropertyChanged;
        }

        private void NetworkMonitorView_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Info("NetworkMonitorView 加载完成");
            ApplySettings();
        }

        private void NetworkMonitorView_Unloaded(object sender, RoutedEventArgs e)
        {
            Log.Info("NetworkMonitorView 卸载");
            
            // 取消订阅设置变化事件
            _settings.PropertyChanged -= Settings_PropertyChanged;
            
            // 释放ViewModel资源
            _viewModel?.Dispose();
        }

        private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 当设置发生变化时，重新应用设置
            System.Windows.Application.Current.Dispatcher.Invoke(ApplySettings);
        }

        private void ApplySettings()
        {
            try
            {
                // 应用背景颜色
                var backgroundColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_settings.BackgroundColor);
                MainBorder.Background = new SolidColorBrush(backgroundColor);

                // 应用文字颜色
                var textColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_settings.TextColor);
                var textBrush = new SolidColorBrush(textColor);
                UploadSpeedText.Foreground = textBrush;
                DownloadSpeedText.Foreground = textBrush;

                // 应用布局方向
                if (_settings.LayoutDirection == LayoutDirection.Horizontal)
                {
                    SpeedPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
                    DownloadGrid.Margin = new Thickness(15, 0, 0, 0); // 横向时添加间距
                }
                else
                {
                    SpeedPanel.Orientation = System.Windows.Controls.Orientation.Vertical;
                    DownloadGrid.Margin = new Thickness(0, 5, 0, 0); // 纵向时添加间距
                }

                // 通知ViewModel应用设置（单位显示等）
                if (_viewModel is ISettingsAware settingsAware)
                {
                    settingsAware.ApplySettings(_settings);
                }

                Log.Info("设置已应用到NetworkMonitorView");
            }
            catch (Exception ex)
            {
                Log.Error4Ctx($"应用设置失败: {ex.Message}");
            }
        }
    }

    // 接口用于通知ViewModel应用设置
    public interface ISettingsAware
    {
        void ApplySettings(NetViewSettings settings);
    }
}