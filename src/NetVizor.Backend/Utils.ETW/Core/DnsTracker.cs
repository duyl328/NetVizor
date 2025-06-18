using Utils.ETW.EtwTracker;

namespace Utils.ETW.Core;

public class DnsTracker : INetTracker
{
    public void SetupEtwHandlers(EtwNetworkCapture networkCapture)
    {
        networkCapture.OnDnsEvent += dnsEvent =>
        {
            
            Console.WriteLine($"[DNS] 查询: {dnsEvent.QueryName} ({dnsEvent.QueryType})");
        };
    }
}