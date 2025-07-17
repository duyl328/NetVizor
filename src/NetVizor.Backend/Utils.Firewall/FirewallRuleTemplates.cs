namespace Utils.Firewall;

/// <summary>
/// 防火墙规则模板
/// </summary>
/// <summary>
/// 防火墙规则模板静态类
/// 提供常用的防火墙规则模板，简化规则创建过程
/// </summary>
/// <remarks>
/// 这个类包含了一系列预定义的规则模板，涵盖了常见的网络服务和应用场景：
/// - Web服务器规则（HTTP/HTTPS）
/// - 远程桌面规则
/// - 数据库服务器规则
/// - 网络诊断规则（Ping）
/// 
/// 使用模板的好处：
/// 1. 减少手动配置错误
/// 2. 确保规则的一致性和安全性
/// 3. 提高开发效率
/// 4. 便于维护和更新
/// </remarks>
public static class FirewallRuleTemplates
{
    /// <summary>
    /// 创建Web服务器防火墙规则模板
    /// </summary>
    /// <param name="name">规则名称，建议使用描述性名称如"WebServer-HTTP"</param>
    /// <param name="port">Web服务端口号，默认为80（HTTP）。HTTPS通常使用443</param>
    /// <returns>配置完成的Web服务器规则创建请求</returns>
    /// <remarks>
    /// 此模板创建的规则特点：
    /// - 协议：TCP（Web服务基于TCP协议）
    /// - 方向：入站（允许外部访问Web服务）
    /// - 动作：允许（permit traffic）
    /// - 配置文件：所有（Domain、Private、Public）
    /// - 状态：启用
    /// 
    /// 常用端口：
    /// - HTTP: 80
    /// - HTTPS: 443
    /// - HTTP备用: 8080, 8443
    /// </remarks>
    /// <example>
    /// <code>
    /// // 创建HTTP服务规则
    /// var httpRule = FirewallRuleTemplates.CreateWebServerRule("MyWebSite-HTTP", 80);
    /// await firewallService.CreateRuleAsync(httpRule);
    /// 
    /// // 创建HTTPS服务规则
    /// var httpsRule = FirewallRuleTemplates.CreateWebServerRule("MyWebSite-HTTPS", 443);
    /// await firewallService.CreateRuleAsync(httpsRule);
    /// </code>
    /// </example>
    public static CreateRuleRequest CreateWebServerRule(string name, int port = 80)
    {
        return new CreateRuleRequest
        {
            Name = name,
            Description = $"Allow inbound traffic on port {port} for web server",
            Protocol = ProtocolType.TCP, // Web服务使用TCP协议
            LocalPorts = port.ToString(), // 指定监听端口
            Direction = RuleDirection.Inbound, // 允许外部连接到本地服务
            Action = RuleAction.Allow, // 允许连接
            Profiles = FirewallProfile.All, // 在所有网络配置文件中生效
            Enabled = true // 规则创建后立即启用
        };
    }

    /// <summary>
    /// 创建远程桌面连接防火墙规则模板
    /// </summary>
    /// <param name="name">规则名称，建议使用"RemoteDesktop"或"RDP"</param>
    /// <returns>配置完成的远程桌面规则创建请求</returns>
    /// <remarks>
    /// 此模板创建的规则特点：
    /// - 协议：TCP
    /// - 端口：3389（RDP标准端口）
    /// - 方向：入站
    /// - 动作：允许
    /// - 配置文件：Domain和Private（出于安全考虑，不包括Public）
    /// - 状态：启用
    /// 
    /// 安全注意事项：
    /// 1. 远程桌面规则不应在公共网络配置文件中启用
    /// 2. 建议配合强密码和网络级身份验证使用
    /// 3. 在生产环境中考虑更改默认端口号
    /// 4. 建议限制允许连接的IP地址范围
    /// </remarks>
    /// <example>
    /// <code>
    /// var rdpRule = FirewallRuleTemplates.CreateRemoteDesktopRule("Allow-RDP");
    /// await firewallService.CreateRuleAsync(rdpRule);
    /// </code>
    /// </example>
    public static CreateRuleRequest CreateRemoteDesktopRule(string name)
    {
        return new CreateRuleRequest
        {
            Name = name,
            Description = "Allow Remote Desktop Protocol",
            Protocol = ProtocolType.TCP, // RDP使用TCP协议
            LocalPorts = "3389", // RDP默认端口
            Direction = RuleDirection.Inbound, // 允许外部RDP连接
            Action = RuleAction.Allow, // 允许连接
            Profiles = FirewallProfile.Domain | FirewallProfile.Private, // 仅在受信任网络中启用
            Enabled = true // 立即启用
        };
    }

    /// <summary>
    /// 创建SQL Server数据库服务防火墙规则模板
    /// </summary>
    /// <param name="name">规则名称，建议包含实例名称如"SQLServer-Production"</param>
    /// <param name="port">SQL Server端口号，默认为1433（默认实例）</param>
    /// <returns>配置完成的SQL Server规则创建请求</returns>
    /// <remarks>
    /// 此模板创建的规则特点：
    /// - 协议：TCP
    /// - 端口：可配置（默认1433）
    /// - 方向：入站
    /// - 动作：允许
    /// - 配置文件：仅Domain（企业环境）
    /// - 状态：启用
    /// 
    /// SQL Server端口说明：
    /// - 默认实例：1433
    /// - 命名实例：动态端口或自定义固定端口
    /// - SQL Browser服务：1434（UDP）
    /// 
    /// 安全建议：
    /// 1. 仅在域环境中启用，避免在公共网络暴露数据库
    /// 2. 使用强密码和SQL Server身份验证
    /// 3. 考虑使用VPN或专用网络连接
    /// 4. 定期更新和打补丁
    /// </remarks>
    /// <example>
    /// <code>
    /// // 默认实例
    /// var sqlRule = FirewallRuleTemplates.CreateSQLServerRule("SQLServer-Default");
    /// 
    /// // 命名实例使用自定义端口
    /// var sqlNamedRule = FirewallRuleTemplates.CreateSQLServerRule("SQLServer-Instance1", 1435);
    /// 
    /// await firewallService.CreateRuleAsync(sqlRule);
    /// await firewallService.CreateRuleAsync(sqlNamedRule);
    /// </code>
    /// </example>
    public static CreateRuleRequest CreateSQLServerRule(string name, int port = 1433)
    {
        return new CreateRuleRequest
        {
            Name = name,
            Description = $"Allow SQL Server traffic on port {port}",
            Protocol = ProtocolType.TCP, // SQL Server使用TCP协议
            LocalPorts = port.ToString(), // SQL Server监听端口
            Direction = RuleDirection.Inbound, // 允许客户端连接到数据库
            Action = RuleAction.Allow, // 允许连接
            Profiles = FirewallProfile.Domain, // 仅在域环境中启用（安全考虑）
            Enabled = true // 立即启用
        };
    }

    /// <summary>
    /// 创建ICMP Ping防火墙规则模板
    /// </summary>
    /// <param name="name">规则名称，建议使用"ICMP-Echo"或"Allow-Ping"</param>
    /// <param name="allowPing">是否允许Ping，true为允许，false为阻止</param>
    /// <returns>配置完成的ICMP规则创建请求</returns>
    /// <remarks>
    /// 此模板创建的规则特点：
    /// - 协议：ICMPv4
    /// - ICMP类型：8（Echo Request）
    /// - 方向：入站
    /// - 动作：根据allowPing参数决定（允许/阻止）
    /// - 配置文件：所有
    /// - 状态：启用
    /// 
    /// ICMP类型说明：
    /// - Type 8: Echo Request (ping请求)
    /// - Type 0: Echo Reply (ping回复)
    /// - "*": 表示所有代码
    /// 
    /// 使用场景：
    /// 1. 网络诊断和故障排除
    /// 2. 监控系统的连通性检查
    /// 3. 网络拓扑发现
    /// 
    /// 安全考虑：
    /// - 在生产环境中可能需要禁用ping以避免网络扫描
    /// - 某些安全策略要求阻止ICMP流量
    /// </remarks>
    /// <example>
    /// <code>
    /// // 允许ping
    /// var allowPingRule = FirewallRuleTemplates.CreatePingRule("Allow-ICMP-Ping", true);
    /// 
    /// // 阻止ping
    /// var blockPingRule = FirewallRuleTemplates.CreatePingRule("Block-ICMP-Ping", false);
    /// 
    /// await firewallService.CreateRuleAsync(allowPingRule);
    /// </code>
    /// </example>
    public static CreateRuleRequest CreatePingRule(string name, bool allowPing = true)
    {
        return new CreateRuleRequest
        {
            Name = name,
            Description = allowPing ? "Allow ICMP Echo Request" : "Block ICMP Echo Request",
            Protocol = ProtocolType.ICMPV4, // IPv4的ICMP协议
            IcmpTypesAndCodes = "8:*", // Type 8: Echo Request, Code: Any
            Direction = RuleDirection.Inbound, // 处理入站ping请求
            Action = allowPing ? RuleAction.Allow : RuleAction.Block, // 根据参数决定动作
            Profiles = FirewallProfile.All, // 在所有网络配置文件中生效
            Enabled = true // 立即启用规则
        };
    }
}
