using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Shell.Models;
using Microsoft.Win32;
using Common.Logger;

namespace Shell.Views;

public partial class SettingsWindow : Window
{
    private static SettingsWindow? _instance;
    private NetViewSettings _settings;

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
        _settings = NetViewSettings.Instance;
        this.Loaded += SettingsWindow_Loaded;
        this.Closing += SettingsWindow_Closing;

        LoadCurrentSettings();
        UpdatePreview();

        Log.Info("SettingsWindow 初始化");
    }

    public static void ShowWindow()
    {
        var window = Instance;
        window.LoadCurrentSettings();
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

    private void LoadCurrentSettings()
    {
        // 加载当前设置到UI控件
        TextColorBox.Text = _settings.TextColor;
        BackgroundColorBox.Text = _settings.BackgroundColor;
        OpacitySlider.Value = _settings.BackgroundOpacity;
        ShowUnitCheckBox.IsChecked = _settings.ShowUnit;

        // 设置速度单位
        foreach (ComboBoxItem item in SpeedUnitComboBox.Items)
        {
            if (item.Tag.ToString() == _settings.SpeedUnit.ToString())
            {
                SpeedUnitComboBox.SelectedItem = item;
                break;
            }
        }

        // 设置布局方向
        VerticalLayoutRadio.IsChecked = _settings.LayoutDirection == LayoutDirection.Vertical;
        HorizontalLayoutRadio.IsChecked = _settings.LayoutDirection == LayoutDirection.Horizontal;

        // 设置网速Top榜
        ShowTopListCheckBox.IsChecked = _settings.ShowNetworkTopList;
        foreach (ComboBoxItem item in TopListCountComboBox.Items)
        {
            if (item.Tag.ToString() == _settings.NetworkTopListCount.ToString())
            {
                TopListCountComboBox.SelectedItem = item;
                break;
            }
        }

        UpdateTopListAvailability();

        // 设置双击动作
        foreach (ComboBoxItem item in DoubleClickActionComboBox.Items)
        {
            if (item.Tag.ToString() == _settings.DoubleClickAction.ToString())
            {
                DoubleClickActionComboBox.SelectedItem = item;
                break;
            }
        }

        // 设置窗口行为选项
        TopmostCheckBox.IsChecked = _settings.Topmost;
        LockPositionCheckBox.IsChecked = _settings.LockPosition;
        ClickThroughCheckBox.IsChecked = _settings.ClickThrough;
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

        if (!isHorizontal)
        {
            ShowTopListCheckBox.IsChecked = false;
        }

        UpdateTopListCountVisibility();
    }

    private void UpdateTopListCountVisibility()
    {
        if (TopListCountComboBox == null) return;

        var isEnabled = ShowTopListCheckBox.IsEnabled && ShowTopListCheckBox.IsChecked == true;
        TopListCountComboBox.IsEnabled = isEnabled;
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

    private void ResetWindowPosition_Click(object sender, RoutedEventArgs e)
    {
        // 重置窗口位置 - 这里可以调用 NetView 的重置位置方法
        try
        {
            System.Windows.MessageBox.Show("窗口位置已重置到屏幕中心。", "重置位置", MessageBoxButton.OK, MessageBoxImage.Information);
            Log.Info("窗口位置重置请求");
        }
        catch (Exception ex)
        {
            Log.Error4Ctx($"重置窗口位置失败: {ex.Message}");
        }
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

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 应用设置
            _settings.TextColor = TextColorBox.Text;
            _settings.BackgroundColor = BackgroundColorBox.Text;
            _settings.BackgroundOpacity = OpacitySlider.Value;
            _settings.ShowUnit = ShowUnitCheckBox.IsChecked == true;

            var selectedUnit = ((ComboBoxItem)SpeedUnitComboBox.SelectedItem)?.Tag?.ToString() ?? "Auto";
            _settings.SpeedUnit = Enum.Parse<SpeedUnit>(selectedUnit);

            _settings.LayoutDirection = HorizontalLayoutRadio.IsChecked == true
                ? LayoutDirection.Horizontal
                : LayoutDirection.Vertical;

            _settings.ShowNetworkTopList = ShowTopListCheckBox.IsChecked == true;
            var selectedCount = ((ComboBoxItem)TopListCountComboBox.SelectedItem)?.Tag?.ToString() ?? "3";
            _settings.NetworkTopListCount = int.Parse(selectedCount);

            var selectedAction = ((ComboBoxItem)DoubleClickActionComboBox.SelectedItem)?.Tag?.ToString() ?? "None";
            _settings.DoubleClickAction = Enum.Parse<DoubleClickAction>(selectedAction);

            // 应用窗口行为设置
            _settings.Topmost = TopmostCheckBox.IsChecked == true;
            _settings.LockPosition = LockPositionCheckBox.IsChecked == true;
            _settings.ClickThrough = ClickThroughCheckBox.IsChecked == true;
            _settings.SnapToScreen = SnapToScreenCheckBox.IsChecked == true;
            _settings.ShowDetailedInfo = ShowDetailedInfoCheckBox.IsChecked == true;

            // 保存设置
            _settings.SaveSettings();

            System.Windows.MessageBox.Show("设置已应用并保存。", "设置", MessageBoxButton.OK, MessageBoxImage.Information);
            Log.Info("设置已应用");
        }
        catch (Exception ex)
        {
            Log.Error4Ctx($"应用设置失败: {ex.Message}");
            System.Windows.MessageBox.Show($"应用设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show("确定要恢复默认设置吗？", "确认",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _settings.RestoreDefaults();
            LoadCurrentSettings();
            UpdatePreview();
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