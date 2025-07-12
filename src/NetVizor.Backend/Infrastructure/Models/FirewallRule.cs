using NetFwTypeLib;

namespace NetVizor.Infrastructure.Models
{
    // 用于定义防火墙规则的方向
    public enum FirewallDirection
    {
        In,
        Out
    }

    // 用于定义防火墙规则的操作
    public enum FirewallAction
    {
        Block,
        Allow
    }

    public class FirewallRule
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? GroupName { get; set; }
        public string ApplicationName { get; set; } = ""; // "*" 表示所有程序
        public string Protocol { get; set; } = "TCP"; // TCP, UDP, ANY
        public string LocalPorts { get; set; } = "*"; // "80,443" 或 "*"
        public string RemotePorts { get; set; } = "*";
        public FirewallDirection Direction { get; set; } = FirewallDirection.In;
        public FirewallAction Action { get; set; } = FirewallAction.Allow;
        public bool Enabled { get; set; } = true;
    }
}