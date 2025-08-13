using System.IO;
using Microsoft.Web.WebView2.Core;

namespace Shell.Utils;

public class WebView2Helper
{
    public static async Task<bool> IsWebView2RuntimeInstalled()
    {
        try
        {
            // 试着创建一个环境，不会真正显示控件
            await CoreWebView2Environment.CreateAsync();
            return true;
        }
        catch (FileNotFoundException)
        {
            return false; // 缺 DLL 或运行时
        }
        catch (WebView2RuntimeNotFoundException)
        {
            return false; // 没有安装运行时
        }
        catch (Exception)
        {
            return false;
        }
    }
}