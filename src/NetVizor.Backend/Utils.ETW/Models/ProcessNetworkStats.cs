using Utils.ETW.Etw;

namespace Utils.ETW.Models;

/// <summary>
/// 进程网络统计信息
/// </summary>
public class ProcessNetworkStats
{
    /// <summary>
    /// 进程ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// 进程名称
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// 应用程序信息
    /// </summary>
    public ApplicationInfo? ApplicationInfo { get; set; }

    /// <summary>
    /// 当前上传速度 (字节/秒)
    /// </summary>
    public double CurrentUploadSpeed { get; set; }

    /// <summary>
    /// 当前下载速度 (字节/秒)
    /// </summary>
    public double CurrentDownloadSpeed { get; set; }

    /// <summary>
    /// 总网速 (上传+下载) (字节/秒)
    /// </summary>
    public double TotalSpeed => CurrentUploadSpeed + CurrentDownloadSpeed;

    /// <summary>
    /// 格式化的上传速度
    /// </summary>
    public string FormattedUploadSpeed => FormatSpeed(CurrentUploadSpeed);

    /// <summary>
    /// 格式化的下载速度
    /// </summary>
    public string FormattedDownloadSpeed => FormatSpeed(CurrentDownloadSpeed);

    /// <summary>
    /// 格式化的总速度
    /// </summary>
    public string FormattedTotalSpeed => FormatSpeed(TotalSpeed);

    /// <summary>
    /// 上次更新时间
    /// </summary>
    public DateTime LastUpdateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 活跃连接数
    /// </summary>
    public int ActiveConnectionCount { get; set; }

    /// <summary>
    /// 格式化网速显示 - 固定4字符宽度
    /// </summary>
    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond == 0)
            return "0  B";

        // 确定单位和数值
        string unit;
        double value;

        if (bytesPerSecond >= 1024 * 1024 * 1024) // >= 1GB
        {
            value = bytesPerSecond / (1024 * 1024 * 1024);
            unit = "GB";
        }
        else if (bytesPerSecond >= 1024 * 1024) // >= 1MB
        {
            value = bytesPerSecond / (1024 * 1024);
            unit = "MB";
        }
        else if (bytesPerSecond >= 1024) // >= 1KB
        {
            value = bytesPerSecond / 1024;
            unit = "KB";
        }
        else // < 1KB
        {
            value = bytesPerSecond;
            unit = "B";
        }

        // 格式化为4字符（不包含单位）
        string numStr;
        if (value >= 1000) // >= 1000，只显示整数
        {
            numStr = ((int)Math.Round(value)).ToString();
        }
        else if (value >= 100) // 100-999，显示1位小数或整数
        {
            if (value % 1 < 0.1) // 基本是整数
                numStr = ((int)value).ToString();
            else
                numStr = value.ToString("F1");
        }
        else if (value >= 10) // 10-99，显示1-2位小数
        {
            if (value % 1 < 0.01) // 基本是整数
                numStr = value.ToString("F0");
            else if (value % 1 < 0.1) // 只需1位小数
                numStr = value.ToString("F1");
            else
                numStr = value.ToString("F2");
        }
        else // < 10，显示2位小数
        {
            numStr = value.ToString("F2");
        }

        // 确保数字部分不超过4个字符
        if (numStr.Length > 4)
        {
            if (numStr.Contains('.'))
            {
                // 有小数点，尝试减少小数位数
                var parts = numStr.Split('.');
                var integerPart = parts[0];
                var decimalPart = parts[1];

                if (integerPart.Length >= 4)
                {
                    // 整数部分已经4位或以上，不要小数
                    numStr = integerPart.Substring(0, 4);
                }
                else
                {
                    // 调整小数位数
                    var maxDecimalDigits = 3 - integerPart.Length; // 减去小数点占用的1位
                    if (maxDecimalDigits > 0 && decimalPart.Length > maxDecimalDigits)
                    {
                        decimalPart = decimalPart.Substring(0, maxDecimalDigits);
                        numStr = $"{integerPart}.{decimalPart}";
                    }
                }
            }
            else
            {
                // 没有小数点，截断到4位
                numStr = numStr.Substring(0, 4);
            }
        }

        return $"{numStr,4}{unit}";
    }
}

/// <summary>
/// 网速Top榜信息
/// </summary>
public class NetworkTopListInfo
{
    /// <summary>
    /// Top进程列表
    /// </summary>
    public List<ProcessNetworkStats> TopProcesses { get; set; } = new();

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否有数据（现在总是有数据，因为确保固定数量的显示）
    /// </summary>
    public bool HasData => TopProcesses.Count > 0;
}