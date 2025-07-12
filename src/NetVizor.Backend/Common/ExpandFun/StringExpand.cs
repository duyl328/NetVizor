using Common.Logger;

namespace Common.ExpandFun;

public static class StringExpand
{
    /// <summary>
    ///     检测是否是 url
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool IsValidUrl(this string str)
    {
        return Uri.TryCreate(str, UriKind.Absolute, out _);
    }

    public static int ToInt(this string? str, int defaultValue = default)
    {
        return TryToInt(str) ?? defaultValue;
    }

    public static int? TryToInt(this string? str)
    {
        if (String.IsNullOrWhiteSpace(str))
        {
            Log.Error($"数据为空, 返回默认值 {null} .");
            return null;
        }

        try
        {
            return int.Parse(str);
        }
        catch (Exception e)
        {
            Log.Error($"数据 {str} 转换失败, 返回默认值 {null} .");
            return null;
        }
    }

    public static double ToDouble(this string? str, double defaultValue = default)
    {
        return TryToDouble(str) ?? defaultValue;
    }

    public static double? TryToDouble(this string? str)
    {
        if (String.IsNullOrWhiteSpace(str))
        {
            Log.Error($"数据为空, 返回默认值 {null} .");
            return null;
        }

        try
        {
            return double.Parse(str);
        }
        catch (Exception e)
        {
            Log.Error($"数据 {str} 转换失败, 返回默认值 {null} .");
            return null;
        }
    }
}