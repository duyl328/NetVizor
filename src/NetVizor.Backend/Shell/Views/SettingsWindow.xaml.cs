using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Shell.Models;
using Microsoft.Win32;
using Common.Logger;
using Data.Models;
using Data;
using System.Linq;

namespace Shell.Views;

public partial class SettingsWindow : Window
{
    private static SettingsWindow? _instance;
    private AppSetting _settings;

    public static SettingsWindow Instance
    {
        get
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new SettingsWindow();
            }

            return _instance;
        }
    }

    private SettingsWindow()
    {
        InitializeComponent();
        _settings = new AppSetting();
        this.Loaded += SettingsWindow_Loaded;
        this.Closing += SettingsWindow_Closing;

        Log.Info("SettingsWindow 初始化");
    }

    public static async void ShowWindow()
    {
        var window = Instance;
        await window.LoadCurrentSettingsAsync();
        window.UpdatePreview();
        window.Show();
        window.Activate();
        window.Focus();
    }

    private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Log.Info("SettingsWindow 加载完成");
    }

    private void SettingsWindow_Closing(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        this.Hide();
        Log.Info("SettingsWindow 隐藏");
    }

    private async Task LoadCurrentSettingsAsync()
    {
        try
        {
            _settings = await DatabaseManager.GetUserSettingsAsync();
        }
        catch (Exception ex)
        {
            Log.Error4Ctx($"加载数据库设置失败: {ex.Message}");
            _settings = new AppSetting(); // 使用默认设置
        }

        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        Log.Info(
            $"开始加载设置，数据库中的值: ShowNetworkTopList={_settings.ShowNetworkTopList}, NetworkTopListCount={_settings.NetworkTopListCount}");

        // 加载当前设置到UI控件
        TextColorBox.Text = _settings.TextColor;
        BackgroundColorBox.Text = _settings.BackgroundColor;
        OpacitySlider.Value = _settings.Opacity;
        ShowUnitCheckBox.IsChecked = _settings.ShowUnit;

        // 设置速度单位 - 需要转换数据库的int值到枚举
        var speedUnitTag = _settings.SpeedUnit switch
        {
            0 => "Bytes",
            1 => "KB",
            2 => "MB",
            _ => "Auto"
        };
        foreach (ComboBoxItem item in SpeedUnitComboBox.Items)
        {
            if (item.Tag.ToString() == speedUnitTag)
            {
                SpeedUnitComboBox.SelectedItem = item;
                break;
            }
        }

        // 设置布局方向 - 需要转换数据库的int值
        VerticalLayoutRadio.IsChecked = _settings.LayoutDirection == 1; // 1为纵向
        HorizontalLayoutRadio.IsChecked = _settings.LayoutDirection == 0; // 0为横向

        Log.Info(
            $"布局方向设置: LayoutDirection={_settings.LayoutDirection}, VerticalLayoutRadio.IsChecked={VerticalLayoutRadio.IsChecked}, HorizontalLayoutRadio.IsChecked={HorizontalLayoutRadio.IsChecked}");

        // 设置网速Top榜
        ShowTopListCheckBox.IsChecked = _settings.ShowNetworkTopList;
        Log.Info(
            $"设置ShowTopListCheckBox.IsChecked = {_settings.ShowNetworkTopList}, 实际值: {ShowTopListCheckBox.IsChecked}");

        foreach (ComboBoxItem item in TopListCountComboBox.Items)
        {
            if (item.Tag.ToString() == _settings.NetworkTopListCount.ToString())
            {
                TopListCountComboBox.SelectedItem = item;
                Log.Info($"设置TopListCountComboBox选中项为: {_settings.NetworkTopListCount}");
                break;
            }
        }

        Log.Info(
            $"加载网速排行榜设置完成: ShowNetworkTopList={_settings.ShowNetworkTopList}, NetworkTopListCount={_settings.NetworkTopListCount}, IsHorizontal={_settings.LayoutDirection == 0}");

        UpdateTopListAvailability();

        // 设置双击动作
        var doubleClickTag = _settings.DoubleClickAction switch
        {
            0 => "None",
            1 => "TrafficAnalysis",
            2 => "Settings",
            _ => "None"
        };
        foreach (ComboBoxItem item in DoubleClickActionComboBox.Items)
        {
            if (item.Tag.ToString() == doubleClickTag)
            {
                DoubleClickActionComboBox.SelectedItem = item;
                break;
            }
        }

        // 设置窗口行为选项
        TopmostCheckBox.IsChecked = _settings.IsTopmost;
        LockPositionCheckBox.IsChecked = _settings.IsPositionLocked;
        ClickThroughCheckBox.IsChecked = _settings.IsClickThrough;
        SnapToScreenCheckBox.IsChecked = _settings.SnapToScreen;
        ShowDetailedInfoCheckBox.IsChecked = _settings.ShowDetailedInfo;

        UpdateColorButtons();
    }

    private void UpdateColorButtons()
    {
        try
        {
            var textColor =
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_settings.TextColor);
            TextColorButton.Background = new SolidColorBrush(textColor);

            var backgroundColor =
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_settings
                    .BackgroundColor);
            BackgroundColorButton.Background = new SolidColorBrush(backgroundColor);
        }
        catch (Exception ex)
        {
            Log.Warning($"更新颜色按钮失败: {ex.Message}");
        }
    }

    private void SelectTextColor_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.ColorDialog();
        try
        {
            var currentColor =
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(TextColorBox.Text);
            dialog.Color =
                System.Drawing.Color.FromArgb(currentColor.A, currentColor.R, currentColor.G, currentColor.B);
        }
        catch
        {
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var color = dialog.Color;
            var colorString = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            TextColorBox.Text = colorString;
            TextColorButton.Background =
                new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
            UpdatePreview();
        }
    }

    private void SelectBackgroundColor_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.ColorDialog();
        try
        {
            var currentColor =
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(BackgroundColorBox
                    .Text);
            dialog.Color = System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B);
        }
        catch
        {
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var color = dialog.Color;
            // 保持透明度
            var alpha = (byte)(OpacitySlider.Value / 100.0 * 255);
            var colorString = $"#{alpha:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            BackgroundColorBox.Text = colorString;
            BackgroundColorButton.Background =
                new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
            UpdatePreview();
        }
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityText != null)
        {
            OpacityText.Text = $"{(int)e.NewValue}%";

            // 更新背景颜色的透明度
            try
            {
                var colorWithoutAlpha = BackgroundColorBox.Text;
                if (colorWithoutAlpha.Length == 9) // #AARRGGBB
                {
                    colorWithoutAlpha = "#" + colorWithoutAlpha.Substring(3); // 移除透明度部分
                }

                var alpha = (byte)(e.NewValue / 100.0 * 255);
                var color =
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                        .ConvertFromString(colorWithoutAlpha);
                var newColorString = $"#{alpha:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
                BackgroundColorBox.Text = newColorString;

                UpdatePreview();
            }
            catch (Exception ex)
            {
                Log.Warning($"更新透明度失败: {ex.Message}");
            }
        }
    }

    private void SpeedUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void ShowUnitCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdatePreview();
    }

    private void LayoutDirection_Changed(object sender, RoutedEventArgs e)
    {
        UpdateTopListAvailability();
        UpdatePreview();
    }

    private void ShowTopListCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        Log.Info($"ShowTopListCheckBox_Changed: IsChecked={ShowTopListCheckBox.IsChecked}");
        UpdateTopListCountVisibility();
    }

    private void TopListCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Top榜数量改变不需要预览更新
    }

    private void UpdateTopListAvailability()
    {
        if (ShowTopListCheckBox == null || TopListCountComboBox == null) return;

        var isHorizontal = HorizontalLayoutRadio.IsChecked == true;
        ShowTopListCheckBox.IsEnabled = isHorizontal;

        Log.Info(
            $"UpdateTopListAvailability: isHorizontal={isHorizontal}, ShowTopListCheckBox.IsEnabled={ShowTopListCheckBox.IsEnabled}");

        UpdateTopListCountVisibility();
    }

    private void UpdateTopListCountVisibility()
    {
        if (TopListCountComboBox == null) return;

        var isEnabled = ShowTopListCheckBox.IsEnabled && ShowTopListCheckBox.IsChecked == true;
        TopListCountComboBox.IsEnabled = isEnabled;

        Log.Info(
            $"UpdateTopListCountVisibility: ShowTopListCheckBox.IsEnabled={ShowTopListCheckBox.IsEnabled}, ShowTopListCheckBox.IsChecked={ShowTopListCheckBox.IsChecked}, TopListCountComboBox.IsEnabled={TopListCountComboBox.IsEnabled}");
    }

    private void DoubleClickActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 双击动作不需要预览更新
    }

    private void TopmostCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // 置顶设置变化，不需要预览更新
    }

    private void LockPositionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // 锁定位置设置变化，不需要预览更新
    }

    private void ClickThroughCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // 点击穿透设置变化，不需要预览更新
    }

    private void SnapToScreenCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // 屏幕边界设置变化，不需要预览更新
    }

    private void ShowDetailedInfoCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // 详细信息设置变化，不需要预览更新
    }

    private void UpdatePreview()
    {
        if (PreviewBorder == null || PreviewPanel == null) return;

        try
        {
            // 更新背景色
            var backgroundColor =
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(BackgroundColorBox
                    .Text);
            PreviewBorder.Background = new SolidColorBrush(backgroundColor);

            // 更新文字颜色
            var textColor =
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(TextColorBox.Text);
            var textBrush = new SolidColorBrush(textColor);
            PreviewUploadText.Foreground = textBrush;
            PreviewDownloadText.Foreground = textBrush;

            // 更新布局方向
            PreviewPanel.Orientation = HorizontalLayoutRadio.IsChecked == true
                ? System.Windows.Controls.Orientation.Horizontal
                : System.Windows.Controls.Orientation.Vertical;

            // 更新显示文本（根据单位设置）
            var showUnit = ShowUnitCheckBox.IsChecked == true;
            var selectedUnit = ((ComboBoxItem)SpeedUnitComboBox.SelectedItem)?.Tag?.ToString() ?? "Auto";

            string uploadText, downloadText;
            switch (selectedUnit)
            {
                case "Bytes":
                    uploadText = showUnit ? "1,280,000 B/s" : "1,280,000";
                    downloadText = showUnit ? "16,445,440 B/s" : "16,445,440";
                    break;
                case "KB":
                    uploadText = showUnit ? "1,250 KB/s" : "1,250";
                    downloadText = showUnit ? "16,060 KB/s" : "16,060";
                    break;
                case "MB":
                    uploadText = showUnit ? "1.25 MB/s" : "1.25";
                    downloadText = showUnit ? "15.67 MB/s" : "15.67";
                    break;
                default: // Auto
                    uploadText = showUnit ? "1.25 MB/s" : "1.25";
                    downloadText = showUnit ? "15.67 MB/s" : "15.67";
                    break;
            }

            PreviewUploadText.Text = uploadText;
            PreviewDownloadText.Text = downloadText;

            // 调整横向布局的间距
            if (HorizontalLayoutRadio.IsChecked == true)
            {
                PreviewDownloadText.Margin = new Thickness(15, 0, 0, 0);
            }
            else
            {
                PreviewDownloadText.Margin = new Thickness(0, 0, 0, 0);
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"更新预览失败: {ex.Message}");
        }
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Info("开始应用设置");
            Log.Info(
                $"应用前UI状态: ShowTopListCheckBox.IsChecked={ShowTopListCheckBox.IsChecked}, TopListCountComboBox.SelectedItem={(TopListCountComboBox.SelectedItem as ComboBoxItem)?.Tag}");

            // 应用设置到AppSetting对象
            _settings.TextColor = TextColorBox.Text;
            _settings.BackgroundColor = BackgroundColorBox.Text;
            _settings.Opacity = (int)OpacitySlider.Value;
            _settings.ShowUnit = ShowUnitCheckBox.IsChecked == true;

            // 转换速度单位
            var selectedUnit = ((ComboBoxItem)SpeedUnitComboBox.SelectedItem)?.Tag?.ToString() ?? "KB";
            _settings.SpeedUnit = selectedUnit switch
            {
                "Bytes" => 0,
                "KB" => 1,
                "MB" => 2,
                _ => 1 // 默认KB
            };

            // 转换布局方向
            _settings.LayoutDirection = HorizontalLayoutRadio.IsChecked == true ? 0 : 1; // 0为横向，1为纵向

            // 保存网速Top榜设置 - 只在横向布局时保存为true，纵向布局时保存实际的UI状态但应用时会被忽略
            var uiShowTopList = ShowTopListCheckBox.IsChecked == true;
            var selectedCountTag = ((ComboBoxItem)TopListCountComboBox.SelectedItem)?.Tag?.ToString() ?? "3";
            var uiTopListCount = int.Parse(selectedCountTag);

            Log.Info(
                $"准备保存: UI状态 ShowTopList={uiShowTopList}, Count={uiTopListCount}, 当前布局方向={_settings.LayoutDirection}");

            _settings.ShowNetworkTopList = uiShowTopList;
            _settings.NetworkTopListCount = uiTopListCount;

            Log.Info(
                $"保存到_settings: ShowNetworkTopList={_settings.ShowNetworkTopList}, NetworkTopListCount={_settings.NetworkTopListCount}, IsHorizontal={HorizontalLayoutRadio.IsChecked}");

            // 转换双击动作
            var selectedAction = ((ComboBoxItem)DoubleClickActionComboBox.SelectedItem)?.Tag?.ToString() ?? "None";
            _settings.DoubleClickAction = selectedAction switch
            {
                "None" => 0,
                "TrafficAnalysis" => 1,
                "Settings" => 2,
                _ => 0
            };

            // 应用窗口行为设置
            _settings.IsTopmost = TopmostCheckBox.IsChecked == true;
            _settings.IsPositionLocked = LockPositionCheckBox.IsChecked == true;
            _settings.IsClickThrough = ClickThroughCheckBox.IsChecked == true;
            _settings.SnapToScreen = SnapToScreenCheckBox.IsChecked == true;
            _settings.ShowDetailedInfo = ShowDetailedInfoCheckBox.IsChecked == true;

            // 设置更新时间
            _settings.UpdateTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            Log.Info(
                $"准备保存到数据库: ShowNetworkTopList={_settings.ShowNetworkTopList}, NetworkTopListCount={_settings.NetworkTopListCount}");

            // 保存设置到数据库
            await DatabaseManager.SaveUserSettingsAsync(_settings);

            Log.Info("数据库保存完成，开始验证");

            // 验证保存结果
            var verifySettings = await DatabaseManager.GetUserSettingsAsync();
            Log.Info(
                $"验证保存结果: ShowNetworkTopList={verifySettings.ShowNetworkTopList}, NetworkTopListCount={verifySettings.NetworkTopListCount}");

            // 通知NetView窗口应用新设置
            await ApplySettingsToNetView();

            System.Windows.MessageBox.Show("设置已应用并保存。", "设置", MessageBoxButton.OK, MessageBoxImage.Information);
            Log.Info("设置已应用");
        }
        catch (Exception ex)
        {
            Log.Error4Ctx($"应用设置失败: {ex.Message}");
            System.Windows.MessageBox.Show($"应用设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ApplySettingsToNetView()
    {
        try
        {
            // 同步到NetViewSettings（用于UI显示）
            var netViewSettings = Shell.Models.NetViewSettings.Instance;
            netViewSettings.ShowNetworkTopList = _settings.ShowNetworkTopList;
            netViewSettings.NetworkTopListCount = _settings.NetworkTopListCount;

            // 转换布局方向
            netViewSettings.LayoutDirection = _settings.LayoutDirection == 0
                ? Shell.Models.LayoutDirection.Horizontal
                : Shell.Models.LayoutDirection.Vertical;

            // 同步其他设置
            netViewSettings.TextColor = _settings.TextColor;
            netViewSettings.BackgroundColor = _settings.BackgroundColor;
            netViewSettings.BackgroundOpacity = _settings.Opacity;
            netViewSettings.ShowUnit = _settings.ShowUnit;
            netViewSettings.SpeedUnit = _settings.SpeedUnit switch
            {
                0 => Shell.Models.SpeedUnit.Bytes,
                1 => Shell.Models.SpeedUnit.KB,
                2 => Shell.Models.SpeedUnit.MB,
                _ => Shell.Models.SpeedUnit.Auto
            };
            netViewSettings.DoubleClickAction = _settings.DoubleClickAction switch
            {
                0 => Shell.Models.DoubleClickAction.None,
                1 => Shell.Models.DoubleClickAction.TrafficAnalysis,
                2 => Shell.Models.DoubleClickAction.Settings,
                _ => Shell.Models.DoubleClickAction.None
            };

            // 查找NetView窗口实例
            var netViewWindow = System.Windows.Application.Current.Windows.OfType<NetView>().FirstOrDefault();
            if (netViewWindow != null)
            {
                // 通过反射获取私有字段_appSettings
                var appSettingsField = typeof(NetView).GetField("_appSettings",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (appSettingsField != null)
                {
                    // 更新NetView的设置
                    appSettingsField.SetValue(netViewWindow, _settings);

                    // 应用窗口行为设置
                    netViewWindow.Topmost = _settings.IsTopmost;

                    // 应用点击穿透设置
                    var setClickThroughMethod = typeof(NetView).GetMethod("SetClickThrough",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    setClickThroughMethod?.Invoke(netViewWindow, new object[] { _settings.IsClickThrough });

                    // 应用屏幕边界约束设置
                    if (_settings.SnapToScreen && !_settings.IsPositionLocked)
                    {
                        var snapToScreenMethod = typeof(NetView).GetMethod("SnapToScreen",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        snapToScreenMethod?.Invoke(netViewWindow, null);
                    }

                    // 触发窗口重绘以应用显示详细信息设置
                    if (_settings.ShowDetailedInfo)
                    {
                        netViewWindow.InvalidateMeasure();
                    }

                    // 更新菜单项状态
                    UpdateNetViewMenuItems(netViewWindow);

                    // 强制刷新NetworkMonitorView中的TopList显示
                    await RefreshNetworkMonitorViewModel();

                    Log.Info("NetView设置已实时应用");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"应用设置到NetView失败: {ex.Message}");
        }
    }

    private void UpdateNetViewMenuItems(NetView netViewWindow)
    {
        try
        {
            // 通过反射更新菜单项状态
            var contextMenu = netViewWindow.ContextMenu;
            if (contextMenu?.Items != null)
            {
                foreach (var item in contextMenu.Items.OfType<MenuItem>())
                {
                    switch (item.Name)
                    {
                        case "TopmostMenuItem":
                            item.IsChecked = _settings.IsTopmost;
                            break;
                        case "LockPositionMenuItem":
                            item.IsChecked = _settings.IsPositionLocked;
                            break;
                        case "ClickThroughMenuItem":
                            item.IsChecked = _settings.IsClickThrough;
                            break;
                        case "SnapToScreenMenuItem":
                            item.IsChecked = _settings.SnapToScreen;
                            break;
                        case "DetailedInfoMenuItem":
                            item.IsChecked = _settings.ShowDetailedInfo;
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"更新NetView菜单项状态失败: {ex.Message}");
        }
    }

    private async Task RefreshNetworkMonitorViewModel()
    {
        try
        {
            // 直接通过NetViewSettings实例通知属性更改来触发UI更新
            var netViewSettings = Shell.Models.NetViewSettings.Instance;

            // 触发PropertyChanged事件来通知UI更新
            var propertyChangedMethod = typeof(Shell.Models.NetViewSettings)
                .GetMethod("OnPropertyChanged",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (propertyChangedMethod != null)
            {
                propertyChangedMethod.Invoke(netViewSettings, new object[] { "ShowNetworkTopList" });
                propertyChangedMethod.Invoke(netViewSettings, new object[] { "NetworkTopListCount" });
                propertyChangedMethod.Invoke(netViewSettings, new object[] { "LayoutDirection" });
                propertyChangedMethod.Invoke(netViewSettings, new object[] { "IsTopListAvailable" });
            }
            else
            {
                // 备用方案：保存设置到NetViewSettings的json文件
                netViewSettings.SaveSettings();
            }

            Log.Info("NetworkMonitorViewModel 排行榜显示已刷新");
        }
        catch (Exception ex)
        {
            Log.Warning($"刷新NetworkMonitorViewModel失败: {ex.Message}");
        }
    }

    private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show("确定要恢复默认设置吗？", "确认",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            // 恢复默认设置
            _settings = new AppSetting(); // 使用默认值
            LoadCurrentSettings();
            UpdatePreview();
        }
    }

    private void ResetWindowPosition_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 查找NetView窗口实例
            var netViewWindow = System.Windows.Application.Current.Windows.OfType<NetView>().FirstOrDefault();
            if (netViewWindow != null)
            {
                // 重置到屏幕右上角
                var workArea = SystemParameters.WorkArea;
                netViewWindow.Left = workArea.Right - netViewWindow.Width - 20;
                netViewWindow.Top = workArea.Top + 20;

                // 同时更新数据库中的位置信息
                _settings.WindowX = (int)netViewWindow.Left;
                _settings.WindowY = (int)netViewWindow.Top;

                System.Windows.MessageBox.Show("窗口位置已重置到屏幕右上角。", "重置位置", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Log.Info("NetView窗口位置已重置");
            }
            else
            {
                System.Windows.MessageBox.Show("未找到NetView窗口。", "重置位置", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            Log.Error4Ctx($"重置窗口位置失败: {ex.Message}");
            System.Windows.MessageBox.Show($"重置窗口位置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }

    // Title bar drag functionality
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            this.DragMove();
        }
    }

    // Force close method for application shutdown
    public void ForceClose()
    {
        this.Closing -= SettingsWindow_Closing;
        this.Close();
        _instance = null;
    }
}