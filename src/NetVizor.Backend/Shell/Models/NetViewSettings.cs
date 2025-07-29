using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.IO;
using Common.Logger;

namespace Shell.Models;

public class NetViewSettings : INotifyPropertyChanged
{
    private static NetViewSettings? _instance;
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "NetVizor", "settings.json");

    public static NetViewSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = LoadSettings();
            }
            return _instance;
        }
    }

    // 外观设置
    private string _textColor = "#333333";
    public string TextColor
    {
        get => _textColor;
        set { _textColor = value; OnPropertyChanged(); }
    }

    private string _backgroundColor = "#F0FFFFFF";
    public string BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; OnPropertyChanged(); }
    }

    private double _backgroundOpacity = 94.0;
    public double BackgroundOpacity
    {
        get => _backgroundOpacity;
        set { _backgroundOpacity = value; OnPropertyChanged(); }
    }

    // 显示设置
    private SpeedUnit _speedUnit = SpeedUnit.Auto;
    public SpeedUnit SpeedUnit
    {
        get => _speedUnit;
        set { _speedUnit = value; OnPropertyChanged(); }
    }

    private bool _showUnit = true;
    public bool ShowUnit
    {
        get => _showUnit;
        set { _showUnit = value; OnPropertyChanged(); }
    }

    private LayoutDirection _layoutDirection = LayoutDirection.Vertical;
    public LayoutDirection LayoutDirection
    {
        get => _layoutDirection;
        set { _layoutDirection = value; OnPropertyChanged(); }
    }

    // 交互设置
    private DoubleClickAction _doubleClickAction = DoubleClickAction.None;
    public DoubleClickAction DoubleClickAction
    {
        get => _doubleClickAction;
        set { _doubleClickAction = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsFilePath, json);
            
            Log.Info("设置已保存");
        }
        catch (Exception ex)
        {
            Log.Error4Ctx($"保存设置失败: {ex.Message}");
        }
    }

    private static NetViewSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var options = new JsonSerializerOptions
                {
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                
                var settings = JsonSerializer.Deserialize<NetViewSettings>(json, options);
                if (settings != null)
                {
                    Log.Info("设置已加载");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"加载设置失败，使用默认设置: {ex.Message}");
        }

        Log.Info("使用默认设置");
        return new NetViewSettings();
    }

    public void RestoreDefaults()
    {
        TextColor = "#333333";
        BackgroundColor = "#F0FFFFFF";
        BackgroundOpacity = 94.0;
        SpeedUnit = SpeedUnit.Auto;
        ShowUnit = true;
        LayoutDirection = LayoutDirection.Vertical;
        DoubleClickAction = DoubleClickAction.None;
        
        Log.Info("设置已恢复默认值");
    }
}

public enum SpeedUnit
{
    Auto,
    Bytes,
    KB,
    MB
}

public enum LayoutDirection
{
    Vertical,
    Horizontal
}

public enum DoubleClickAction
{
    None,
    TrafficAnalysis,
    Settings
}