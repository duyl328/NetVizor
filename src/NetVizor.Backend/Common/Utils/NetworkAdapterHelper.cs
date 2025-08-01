using System.Management;
using System;


namespace Common.Utils;

public class NetworkAdapterHelper
{
    /// <summary>
    /// 通过 GUID 获取网卡信息
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public static NetworkInfo? GetNetworkInfoByGuid(string guid)
    {
        try
        {
            // 注意：WMI中的网卡ID格式，通常是网卡GUID，但带大括号和大小写等，处理一下格式
            string formattedGuid = guid;
            if (!guid.StartsWith("{"))
                formattedGuid = "{" + guid + "}";
            if (!guid.EndsWith("}"))
                formattedGuid = formattedGuid + "}";

            string query = $"SELECT * FROM Win32_NetworkAdapter WHERE GUID = '{formattedGuid}'";
            using (var searcher = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject mo in searcher.Get())
                {
                    return new NetworkInfo
                    {
                        Name = mo["Name"]?.ToString(),
                        Description = mo["Description"]?.ToString(),
                        MACAddress = mo["MACAddress"]?.ToString(),
                        NetConnectionID = mo["NetConnectionID"]?.ToString(),
                        Speed = mo["Speed"] != null ? Convert.ToInt64(mo["Speed"]) : 0,
                        Status = mo["Status"]?.ToString()
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error getting network info: " + ex.Message);
        }

        return null;
    }
}

public class NetworkInfo
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? MACAddress { get; set; }
    public string? NetConnectionID { get; set; }
    public long Speed { get; set; }
    public string? Status { get; set; }
}