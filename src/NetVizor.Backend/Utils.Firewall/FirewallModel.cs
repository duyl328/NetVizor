namespace Utils.Firewall;

/// <summary>
/// 防火墙配置文件类型
/// </summary>
[Flags]
public enum FirewallProfile
{
    Domain = 1,
    Private = 2,
    Public = 4,
    All = Domain | Private | Public
}

/// <summary>
/// 防火墙规则方向
/// </summary>
public enum RuleDirection
{
    Inbound = 1,
    Outbound = 2
}

/// <summary>
/// 防火墙规则动作
/// </summary>
public enum RuleAction
{
    Allow = 1,
    Block = 0
}

/// <summary>
/// 协议类型
/// </summary>
public enum ProtocolType
{
    Any = 256,
    ICMPV4 = 1,
    IGMP = 2,
    TCP = 6,
    UDP = 17,
    IPv6 = 41,
    IPv6Route = 43,
    IPv6Frag = 44,
    GRE = 47,
    ICMPv6 = 58,
    IPv6NoNxt = 59,
    IPv6Opts = 60,
    VRRP = 112,
    PGM = 113,
    L2TP = 115
}

/// <summary>
/// 防火墙状态信息
/// </summary>
public class FirewallStatus
{
    public bool IsEnabled { get; set; }
    public Dictionary<FirewallProfile, ProfileStatus> ProfileStatuses { get; set; } = new();
    public int TotalRules { get; set; }
    public int EnabledRules { get; set; }
    public int InboundRules { get; set; }
    public int OutboundRules { get; set; }
    public DateTime LastModified { get; set; }
}

/// <summary>
/// 配置文件状态
/// </summary>
public class ProfileStatus
{
    public FirewallProfile Profile { get; set; }
    public bool IsEnabled { get; set; }
    public bool BlockAllInboundTraffic { get; set; }
    public bool NotifyOnListen { get; set; }
    public bool UnicastResponsesDisabled { get; set; }
    public RuleAction DefaultInboundAction { get; set; }
    public RuleAction DefaultOutboundAction { get; set; }
}

/// <summary>
/// 防火墙规则详细信息
/// </summary>
public class FirewallRule
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public ProtocolType Protocol { get; set; }
    public string LocalPorts { get; set; } = string.Empty;
    public string RemotePorts { get; set; } = string.Empty;
    public string LocalAddresses { get; set; } = string.Empty;
    public string RemoteAddresses { get; set; } = string.Empty;
    public string IcmpTypesAndCodes { get; set; } = string.Empty;
    public RuleDirection Direction { get; set; }
    public bool Enabled { get; set; }
    public FirewallProfile Profiles { get; set; }
    public bool EdgeTraversal { get; set; }
    public RuleAction Action { get; set; }
    public string Grouping { get; set; } = string.Empty;
    public string InterfaceTypes { get; set; } = string.Empty;
    public List<string> Interfaces { get; set; } = new();
    public DateTime? CreationDate { get; set; }
    public DateTime? ModificationDate { get; set; }

    // 高级属性
    public bool EdgeTraversalAllowed { get; set; }
    public bool LooseSourceMapping { get; set; }
    public bool LocalOnlyMapping { get; set; }
    public string RemoteMachineAuthorizationList { get; set; } = string.Empty;
    public string RemoteUserAuthorizationList { get; set; } = string.Empty;
    public string EmbeddedContext { get; set; } = string.Empty;
    public int Flags { get; set; }
    public bool SecureFlags { get; set; }
}

/// <summary>
/// 创建防火墙规则的请求
/// </summary>
public class CreateRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public ProtocolType Protocol { get; set; } = ProtocolType.Any;
    public string LocalPorts { get; set; } = string.Empty;
    public string RemotePorts { get; set; } = string.Empty;
    public string LocalAddresses { get; set; } = "*";
    public string RemoteAddresses { get; set; } = "*";
    public string IcmpTypesAndCodes { get; set; } = string.Empty;
    public RuleDirection Direction { get; set; }
    public bool Enabled { get; set; } = true;
    public FirewallProfile Profiles { get; set; } = FirewallProfile.All;
    public RuleAction Action { get; set; } = RuleAction.Allow;
    public string Grouping { get; set; } = string.Empty;
    public string InterfaceTypes { get; set; } = "All";
    public List<string> Interfaces { get; set; } = new();

    // 高级选项
    public bool EdgeTraversal { get; set; } = false;
    public bool EdgeTraversalAllowed { get; set; } = false;
    public string RemoteMachineAuthorizationList { get; set; } = string.Empty;
    public string RemoteUserAuthorizationList { get; set; } = string.Empty;
}

/// <summary>
/// 更新防火墙规则的请求
/// </summary>
public class UpdateRuleRequest
{
    public string CurrentName { get; set; } = string.Empty;
    public string? NewName { get; set; }
    public string? Description { get; set; }
    public bool? Enabled { get; set; }
    public string? ApplicationName { get; set; }
    public string? ServiceName { get; set; }
    public ProtocolType? Protocol { get; set; }
    public string? LocalPorts { get; set; }
    public string? RemotePorts { get; set; }
    public string? LocalAddresses { get; set; }
    public string? RemoteAddresses { get; set; }
    public FirewallProfile? Profiles { get; set; }
    public RuleAction? Action { get; set; }
    public string? Grouping { get; set; }
    public bool? EdgeTraversal { get; set; }
}

/// <summary>
/// 防火墙规则查询过滤器
/// </summary>
public class RuleFilter
{
    public string? NamePattern { get; set; }
    public RuleDirection? Direction { get; set; }
    public bool? Enabled { get; set; }
    public FirewallProfile? Profile { get; set; }
    public ProtocolType? Protocol { get; set; }
    public RuleAction? Action { get; set; }
    public string? ApplicationName { get; set; }
    public string? Grouping { get; set; }
    public string? Port { get; set; }
}

/// <summary>
/// 防火墙统计信息
/// </summary>
public class FirewallStatistics
{
    public int TotalRules { get; set; }
    public int EnabledRules { get; set; }
    public int DisabledRules { get; set; }
    public Dictionary<RuleDirection, int> RulesByDirection { get; set; } = new();
    public Dictionary<ProtocolType, int> RulesByProtocol { get; set; } = new();
    public Dictionary<FirewallProfile, int> RulesByProfile { get; set; } = new();
    public Dictionary<RuleAction, int> RulesByAction { get; set; } = new();
    public List<string> TopApplications { get; set; } = new();
    public List<string> TopPorts { get; set; } = new();
}
