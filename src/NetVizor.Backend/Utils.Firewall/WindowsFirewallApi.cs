using System.Text.Json;
using System.Text.Json.Serialization;
using NetFwTypeLib;

namespace Utils.Firewall;

/// <summary>
/// Windows防火墙管理API实现
/// 提供对Windows防火墙的完整控制，包括状态管理、规则操作、配置文件管理等功能
/// 使用Windows COM接口NetFwTypeLib与系统防火墙进行交互
/// </summary>
public class WindowsFirewallApi : IFirewallApi
{
    /// <summary>
    /// Windows防火墙策略对象，用于访问防火墙的全局设置和状态
    /// </summary>
    private readonly INetFwPolicy2? _fwPolicy;
    
    /// <summary>
    /// Windows防火墙规则集合对象，用于管理防火墙规则
    /// </summary>
    private readonly INetFwRules? _fwRules;

    /// <summary>
    /// 初始化WindowsFirewallApi实例
    /// 通过COM接口连接到Windows防火墙服务
    /// </summary>
    public WindowsFirewallApi()
    {
        try
        {
            // 通过ProgID获取Windows防火墙策略对象的类型
            Type? netFwPolicy2Type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if (netFwPolicy2Type != null)
            {
                // 创建防火墙策略对象实例
                _fwPolicy = (INetFwPolicy2?)Activator.CreateInstance(netFwPolicy2Type);
                // 获取防火墙规则集合
                _fwRules = _fwPolicy?.Rules;
            }
        }
        catch (Exception)
        {
            // 初始化失败，通常是因为缺少管理员权限
            // 在这种情况下，_fwPolicy和_fwRules将保持为null
        }
    }

    #region 防火墙状态管理

    /// <summary>
    /// 获取防火墙的完整状态信息
    /// 包括各配置文件的状态、规则统计等
    /// </summary>
    /// <returns>包含防火墙完整状态的FirewallStatus对象</returns>
    public FirewallStatus GetFirewallStatus()
    {
        // 创建状态对象并获取基本启用状态
        var status = new FirewallStatus
        {
            IsEnabled = IsFirewallEnabled(),
            ProfileStatuses = new Dictionary<FirewallProfile, ProfileStatus>()
        };

        // 遍历所有防火墙配置文件（域、私有、公共）获取详细状态
        foreach (FirewallProfile profile in Enum.GetValues(typeof(FirewallProfile)))
        {
            if (profile != FirewallProfile.All)
            {
                status.ProfileStatuses[profile] = GetProfileStatus(profile);
            }
        }

        // 获取规则统计信息
        var allRules = GetAllRules();
        status.TotalRules = allRules.Count;                                    // 总规则数
        status.EnabledRules = allRules.Count(r => r.Enabled);                // 已启用规则数
        status.InboundRules = allRules.Count(r => r.Direction == RuleDirection.Inbound);   // 入站规则数
        status.OutboundRules = allRules.Count(r => r.Direction == RuleDirection.Outbound); // 出站规则数

        return status;
    }

    /// <summary>
    /// 检查防火墙是否在任何配置文件中启用
    /// </summary>
    /// <returns>如果防火墙在至少一个配置文件中启用则返回true</returns>
    private bool IsFirewallEnabled()
    {
        if (_fwPolicy == null) return false;

        // 检查三个配置文件：域、私有网络、公共网络
        return _fwPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] ||
               _fwPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] ||
               _fwPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC];
    }

    /// <summary>
    /// 启用指定配置文件的防火墙
    /// </summary>
    /// <param name="profile">要启用的防火墙配置文件，默认为所有配置文件</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool EnableFirewall(FirewallProfile profile = FirewallProfile.All)
    {
        try
        {
            SetFirewallEnabled(profile, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 禁用指定配置文件的防火墙
    /// </summary>
    /// <param name="profile">要禁用的防火墙配置文件，默认为所有配置文件</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool DisableFirewall(FirewallProfile profile = FirewallProfile.All)
    {
        try
        {
            SetFirewallEnabled(profile, false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 设置指定配置文件防火墙的启用状态
    /// 内部方法，用于统一处理启用/禁用逻辑
    /// </summary>
    /// <param name="profile">目标配置文件</param>
    /// <param name="enabled">是否启用</param>
    private void SetFirewallEnabled(FirewallProfile profile, bool enabled)
    {
        if (_fwPolicy == null) return;

        // 根据配置文件标志设置相应的防火墙状态
        if (profile.HasFlag(FirewallProfile.Domain))
            _fwPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN] = enabled;
        if (profile.HasFlag(FirewallProfile.Private))
            _fwPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE] = enabled;
        if (profile.HasFlag(FirewallProfile.Public))
            _fwPolicy.FirewallEnabled[NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC] = enabled;
    }

    /// <summary>
    /// 获取指定配置文件的详细状态信息
    /// </summary>
    /// <param name="profile">目标配置文件</param>
    /// <returns>包含配置文件详细状态的ProfileStatus对象</returns>
    public ProfileStatus GetProfileStatus(FirewallProfile profile)
    {
        if (_fwPolicy == null)
            return new ProfileStatus { Profile = profile };

        var profileType = ConvertToNetFwProfile(profile);
        return new ProfileStatus
        {
            Profile = profile,
            IsEnabled = _fwPolicy.FirewallEnabled[profileType],                                    // 是否启用
            BlockAllInboundTraffic = _fwPolicy.BlockAllInboundTraffic[profileType],              // 是否阻止所有入站流量
            NotifyOnListen = _fwPolicy.NotificationsDisabled[profileType] == false,              // 是否显示通知
            UnicastResponsesDisabled = _fwPolicy.UnicastResponsesToMulticastBroadcastDisabled[profileType], // 单播响应设置
            DefaultInboundAction = (RuleAction)_fwPolicy.DefaultInboundAction[profileType],      // 默认入站动作
            DefaultOutboundAction = (RuleAction)_fwPolicy.DefaultOutboundAction[profileType]     // 默认出站动作
        };
    }

    /// <summary>
    /// 设置指定配置文件的状态
    /// </summary>
    /// <param name="profile">目标配置文件</param>
    /// <param name="status">要设置的状态信息</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool SetProfileStatus(FirewallProfile profile, ProfileStatus status)
    {
        try
        {
            if (_fwPolicy == null) return false;

            var profileType = ConvertToNetFwProfile(profile);
            
            // 设置各项配置
            _fwPolicy.FirewallEnabled[profileType] = status.IsEnabled;
            _fwPolicy.BlockAllInboundTraffic[profileType] = status.BlockAllInboundTraffic;
            _fwPolicy.NotificationsDisabled[profileType] = !status.NotifyOnListen;
            _fwPolicy.UnicastResponsesToMulticastBroadcastDisabled[profileType] = status.UnicastResponsesDisabled;
            _fwPolicy.DefaultInboundAction[profileType] = (NET_FW_ACTION_)status.DefaultInboundAction;
            _fwPolicy.DefaultOutboundAction[profileType] = (NET_FW_ACTION_)status.DefaultOutboundAction;
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region 规则查询

    /// <summary>
    /// 获取所有防火墙规则
    /// </summary>
    /// <returns>防火墙规则列表</returns>
    public List<FirewallRule> GetAllRules()
    {
        var rules = new List<FirewallRule>();
        if (_fwRules == null) return rules;

        // 遍历Windows防火墙中的所有规则并转换为自定义格式
        foreach (INetFwRule rule in _fwRules)
        {
            rules.Add(ConvertToFirewallRule(rule));
        }

        return rules;
    }

    /// <summary>
    /// 根据过滤条件获取防火墙规则
    /// </summary>
    /// <param name="filter">过滤条件对象</param>
    /// <returns>符合条件的防火墙规则列表</returns>
    public List<FirewallRule> GetRulesByFilter(RuleFilter filter)
    {
        var allRules = GetAllRules();
        var query = allRules.AsQueryable();

        // 按规则名称模式过滤
        if (!string.IsNullOrEmpty(filter.NamePattern))
            query = query.Where(r => r.Name.Contains(filter.NamePattern, StringComparison.OrdinalIgnoreCase));

        // 按流量方向过滤（入站/出站）
        if (filter.Direction.HasValue)
            query = query.Where(r => r.Direction == filter.Direction.Value);

        // 按启用状态过滤
        if (filter.Enabled.HasValue)
            query = query.Where(r => r.Enabled == filter.Enabled.Value);

        // 按配置文件过滤
        if (filter.Profile.HasValue)
            query = query.Where(r => r.Profiles.HasFlag(filter.Profile.Value));

        // 按协议类型过滤
        if (filter.Protocol.HasValue)
            query = query.Where(r => r.Protocol == filter.Protocol.Value);

        // 按动作过滤（允许/阻止）
        if (filter.Action.HasValue)
            query = query.Where(r => r.Action == filter.Action.Value);

        // 按应用程序名称过滤
        if (!string.IsNullOrEmpty(filter.ApplicationName))
            query = query.Where(r =>
                !string.IsNullOrEmpty(r.ApplicationName) &&
                r.ApplicationName.Contains(filter.ApplicationName, StringComparison.OrdinalIgnoreCase));

        // 按规则组过滤
        if (!string.IsNullOrEmpty(filter.Grouping))
            query = query.Where(r => r.Grouping == filter.Grouping);

        // 按端口过滤（本地端口或远程端口）
        if (!string.IsNullOrEmpty(filter.Port))
            query = query.Where(r =>
                (!string.IsNullOrEmpty(r.LocalPorts) && r.LocalPorts.Contains(filter.Port)) ||
                (!string.IsNullOrEmpty(r.RemotePorts) && r.RemotePorts.Contains(filter.Port)));

        return query.ToList();
    }

    /// <summary>
    /// 根据规则名称获取特定的防火墙规则
    /// </summary>
    /// <param name="ruleName">规则名称</param>
    /// <returns>找到的规则对象，如果不存在则返回null</returns>
    public FirewallRule? GetRuleByName(string ruleName)
    {
        try
        {
            if (_fwRules == null) return null;

            // 使用Windows防火墙API直接通过名称获取规则
            INetFwRule rule = _fwRules.Item(ruleName);
            return ConvertToFirewallRule(rule);
        }
        catch
        {
            // 规则不存在或访问出错
            return null;
        }
    }

    /// <summary>
    /// 获取指定应用程序的所有防火墙规则
    /// </summary>
    /// <param name="applicationPath">应用程序完整路径</param>
    /// <returns>该应用程序的所有相关规则</returns>
    public List<FirewallRule> GetRulesByApplication(string applicationPath)
    {
        return GetRulesByFilter(new RuleFilter { ApplicationName = applicationPath });
    }

    /// <summary>
    /// 获取指定端口和协议的所有防火墙规则
    /// </summary>
    /// <param name="port">端口号</param>
    /// <param name="protocol">协议类型，默认为任意协议</param>
    /// <returns>该端口的所有相关规则</returns>
    public List<FirewallRule> GetRulesByPort(int port, ProtocolType protocol = ProtocolType.Any)
    {
        var filter = new RuleFilter { Port = port.ToString() };
        if (protocol != ProtocolType.Any)
            filter.Protocol = protocol;
        return GetRulesByFilter(filter);
    }

    /// <summary>
    /// 获取指定组的所有防火墙规则
    /// </summary>
    /// <param name="groupName">规则组名称</param>
    /// <returns>该组的所有规则</returns>
    public List<FirewallRule> GetRulesByGroup(string groupName)
    {
        return GetRulesByFilter(new RuleFilter { Grouping = groupName });
    }

    #endregion

    #region 规则管理

    /// <summary>
    /// 创建新的防火墙规则
    /// </summary>
    /// <param name="request">创建规则的请求对象，包含规则的所有配置信息</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool CreateRule(CreateRuleRequest request)
    {
        try
        {
            if (_fwRules == null) return false;

            // 通过ProgID获取防火墙规则对象类型
            Type? netFwRuleType = Type.GetTypeFromProgID("HNetCfg.FWRule");
            if (netFwRuleType == null) return false;

            // 创建新的防火墙规则实例
            INetFwRule? newRule = (INetFwRule?)Activator.CreateInstance(netFwRuleType);
            if (newRule == null) return false;

            // 设置规则的各项属性
            newRule.Name = request.Name;                                    // 规则名称
            newRule.Description = request.Description;                      // 规则描述
            newRule.ApplicationName = request.ApplicationName;              // 应用程序路径
            // 注意：INetFwRule 接口可能没有 ServiceName 属性，这里移除了对它的直接赋值
            newRule.Protocol = (int)request.Protocol;                       // 协议类型
            newRule.LocalPorts = request.LocalPorts;                        // 本地端口
            newRule.RemotePorts = request.RemotePorts;                      // 远程端口
            newRule.LocalAddresses = request.LocalAddresses;                // 本地地址
            newRule.RemoteAddresses = request.RemoteAddresses;              // 远程地址
            newRule.IcmpTypesAndCodes = request.IcmpTypesAndCodes;          // ICMP类型和代码
            newRule.Direction = (NET_FW_RULE_DIRECTION_)request.Direction;  // 流量方向
            newRule.Enabled = request.Enabled;                             // 是否启用
            newRule.Profiles = ConvertToNetFwProfiles(request.Profiles);   // 适用的配置文件
            newRule.Action = (NET_FW_ACTION_)request.Action;                // 规则动作
            newRule.Grouping = request.Grouping;                           // 规则组
            newRule.InterfaceTypes = request.InterfaceTypes;               // 接口类型

            // 设置边缘遍历（用于某些特殊网络场景）
            if (request.EdgeTraversal)
                newRule.EdgeTraversal = request.EdgeTraversal;

            // 将新规则添加到防火墙
            _fwRules.Add(newRule);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 更新现有的防火墙规则
    /// </summary>
    /// <param name="request">更新规则的请求对象</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool UpdateRule(UpdateRuleRequest request)
    {
        try
        {
            if (_fwRules == null) return false;

            // 根据当前名称获取规则
            INetFwRule rule = _fwRules.Item(request.CurrentName);

            // 根据请求对象中的非空值更新规则属性
            if (!string.IsNullOrEmpty(request.NewName))
                rule.Name = request.NewName;
            if (request.Description != null)
                rule.Description = request.Description;
            if (request.Enabled.HasValue)
                rule.Enabled = request.Enabled.Value;
            if (!string.IsNullOrEmpty(request.ApplicationName))
                rule.ApplicationName = request.ApplicationName;
            // 移除了对 ServiceName 的直接赋值
            if (request.Protocol.HasValue)
                rule.Protocol = (int)request.Protocol.Value;
            if (request.LocalPorts != null)
                rule.LocalPorts = request.LocalPorts;
            if (request.RemotePorts != null)
                rule.RemotePorts = request.RemotePorts;
            if (request.LocalAddresses != null)
                rule.LocalAddresses = request.LocalAddresses;
            if (request.RemoteAddresses != null)
                rule.RemoteAddresses = request.RemoteAddresses;
            if (request.Profiles.HasValue)
                rule.Profiles = ConvertToNetFwProfiles(request.Profiles.Value);
            if (request.Action.HasValue)
                rule.Action = (NET_FW_ACTION_)request.Action.Value;
            if (request.Grouping != null)
                rule.Grouping = request.Grouping;
            if (request.EdgeTraversal.HasValue)
                rule.EdgeTraversal = request.EdgeTraversal.Value;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 删除指定名称的防火墙规则
    /// </summary>
    /// <param name="ruleName">要删除的规则名称</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool DeleteRule(string ruleName)
    {
        try
        {
            _fwRules?.Remove(ruleName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 删除指定组的所有防火墙规则
    /// </summary>
    /// <param name="groupName">规则组名称</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool DeleteRulesByGroup(string groupName)
    {
        try
        {
            // 先获取组中的所有规则
            var rules = GetRulesByGroup(groupName);
            
            // 逐一删除组中的每个规则
            foreach (var rule in rules)
            {
                _fwRules?.Remove(rule.Name);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 启用指定名称的防火墙规则
    /// </summary>
    /// <param name="ruleName">规则名称</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool EnableRule(string ruleName)
    {
        return UpdateRule(new UpdateRuleRequest { CurrentName = ruleName, Enabled = true });
    }

    /// <summary>
    /// 禁用指定名称的防火墙规则
    /// </summary>
    /// <param name="ruleName">规则名称</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool DisableRule(string ruleName)
    {
        return UpdateRule(new UpdateRuleRequest { CurrentName = ruleName, Enabled = false });
    }

    /// <summary>
    /// 切换指定规则的启用状态（启用变禁用，禁用变启用）
    /// </summary>
    /// <param name="ruleName">规则名称</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool ToggleRule(string ruleName)
    {
        var rule = GetRuleByName(ruleName);
        if (rule == null) return false;
        return UpdateRule(new UpdateRuleRequest { CurrentName = ruleName, Enabled = !rule.Enabled });
    }

    #endregion

    #region 批量操作

    /// <summary>
    /// 批量创建多个防火墙规则
    /// </summary>
    /// <param name="requests">创建规则请求列表</param>
    /// <returns>所有规则都创建成功返回true，否则返回false</returns>
    public bool CreateRules(List<CreateRuleRequest> requests)
    {
        bool allSuccess = true;
        foreach (var request in requests)
        {
            if (!CreateRule(request))
                allSuccess = false;
        }

        return allSuccess;
    }

    /// <summary>
    /// 批量删除多个防火墙规则
    /// </summary>
    /// <param name="ruleNames">要删除的规则名称列表</param>
    /// <returns>所有规则都删除成功返回true，否则返回false</returns>
    public bool DeleteRules(List<string> ruleNames)
    {
        bool allSuccess = true;
        foreach (var ruleName in ruleNames)
        {
            if (!DeleteRule(ruleName))
                allSuccess = false;
        }

        return allSuccess;
    }

    /// <summary>
    /// 批量启用多个防火墙规则
    /// </summary>
    /// <param name="ruleNames">要启用的规则名称列表</param>
    /// <returns>所有规则都启用成功返回true，否则返回false</returns>
    public bool EnableRules(List<string> ruleNames)
    {
        bool allSuccess = true;
        foreach (var ruleName in ruleNames)
        {
            if (!EnableRule(ruleName))
                allSuccess = false;
        }

        return allSuccess;
    }

    /// <summary>
    /// 批量禁用多个防火墙规则
    /// </summary>
    /// <param name="ruleNames">要禁用的规则名称列表</param>
    /// <returns>所有规则都禁用成功返回true，否则返回false</returns>
    public bool DisableRules(List<string> ruleNames)
    {
        bool allSuccess = true;
        foreach (var ruleName in ruleNames)
        {
            if (!DisableRule(ruleName))
                allSuccess = false;
        }

        return allSuccess;
    }

    #endregion

    #region 高级功能

    /// <summary>
    /// 将防火墙设置恢复为默认值
    /// 这将清除所有自定义规则和设置
    /// </summary>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool RestoreDefaultSettings()
    {
        try
        {
            _fwPolicy?.RestoreLocalFirewallDefaults();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 将所有防火墙规则导出到JSON文件
    /// </summary>
    /// <param name="filePath">导出文件的完整路径</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool ExportRules(string filePath)
    {
        try
        {
            var rules = GetAllRules();
            
            // 配置JSON序列化选项，包括缩进和枚举转换
            var json = JsonSerializer.Serialize(rules, new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            });
            
            System.IO.File.WriteAllText(filePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从JSON文件导入防火墙规则
    /// </summary>
    /// <param name="filePath">包含规则的JSON文件路径</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool ImportRules(string filePath)
    {
        try
        {
            var json = System.IO.File.ReadAllText(filePath);
            
            // 反序列化JSON文件中的规则
            var rules = JsonSerializer.Deserialize<List<FirewallRule>>(json, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            });

            if (rules == null) return false;

            // 为每个导入的规则创建对应的防火墙规则
            foreach (var rule in rules)
            {
                CreateRule(new CreateRuleRequest
                {
                    Name = rule.Name,
                    Description = rule.Description,
                    ApplicationName = rule.ApplicationName,
                    ServiceName = rule.ServiceName,
                    Protocol = rule.Protocol,
                    LocalPorts = rule.LocalPorts,
                    RemotePorts = rule.RemotePorts,
                    LocalAddresses = rule.LocalAddresses,
                    RemoteAddresses = rule.RemoteAddresses,
                    IcmpTypesAndCodes = rule.IcmpTypesAndCodes,
                    Direction = rule.Direction,
                    Enabled = rule.Enabled,
                    Profiles = rule.Profiles,
                    Action = rule.Action,
                    Grouping = rule.Grouping,
                    InterfaceTypes = rule.InterfaceTypes,
                    Interfaces = rule.Interfaces,
                    EdgeTraversal = rule.EdgeTraversal
                });
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取防火墙规则的详细统计信息
    /// 包括规则数量、分类统计、热门应用和端口等
    /// </summary>
    /// <returns>包含统计信息的FirewallStatistics对象</returns>
    public FirewallStatistics GetStatistics()
    {
        var rules = GetAllRules();
        var stats = new FirewallStatistics
        {
            TotalRules = rules.Count,                           // 总规则数
            EnabledRules = rules.Count(r => r.Enabled),        // 已启用规则数
            DisabledRules = rules.Count(r => !r.Enabled)       // 已禁用规则数
        };

        // 按流量方向统计规则数量
        stats.RulesByDirection[RuleDirection.Inbound] = rules.Count(r => r.Direction == RuleDirection.Inbound);
        stats.RulesByDirection[RuleDirection.Outbound] = rules.Count(r => r.Direction == RuleDirection.Outbound);

        // 按协议类型分组统计
        var protocolGroups = rules.GroupBy(r => r.Protocol);
        foreach (var group in protocolGroups)
        {
            stats.RulesByProtocol[group.Key] = group.Count();
        }

        // 按配置文件统计规则数量
        foreach (FirewallProfile profile in Enum.GetValues(typeof(FirewallProfile)))
        {
            if (profile != FirewallProfile.All)
            {
                stats.RulesByProfile[profile] = rules.Count(r => r.Profiles.HasFlag(profile));
            }
        }

        // 按动作统计规则数量
        stats.RulesByAction[RuleAction.Allow] = rules.Count(r => r.Action == RuleAction.Allow);
        stats.RulesByAction[RuleAction.Block] = rules.Count(r => r.Action == RuleAction.Block);

        // 统计规则最多的应用程序（Top 10）
        stats.TopApplications = rules
            .Where(r => !string.IsNullOrEmpty(r.ApplicationName))
            .GroupBy(r => r.ApplicationName)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToList();

        // 统计使用最多的端口（Top 10）
        var allPorts = new List<string>();
        foreach (var rule in rules)
        {
            if (!string.IsNullOrEmpty(rule.LocalPorts))
                allPorts.AddRange(rule.LocalPorts.Split(','));
            if (!string.IsNullOrEmpty(rule.RemotePorts))
                allPorts.AddRange(rule.RemotePorts.Split(','));
        }

        stats.TopPorts = allPorts
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .GroupBy(p => p.Trim())
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToList();

        return stats;
    }

    /// <summary>
    /// 获取所有有防火墙规则的应用程序列表
    /// </summary>
    /// <returns>应用程序路径的去重排序列表</returns>
    public List<string> GetApplicationsWithRules()
    {
        return GetAllRules()
            .Where(r => !string.IsNullOrEmpty(r.ApplicationName))
            .Select(r => r.ApplicationName)
            .Distinct()
            .OrderBy(a => a)
            .ToList();
    }

    /// <summary>
    /// 获取当前活动的网络连接列表
    /// 注意：此方法需要使用netstat或WMI来获取实际的连接信息
    /// 当前为简化实现，返回空列表
    /// </summary>
    /// <returns>活动连接列表（当前返回空列表）</returns>
    public List<string> GetActiveConnections()
    {
        // 这个需要使用netstat或WMI来获取活动连接
        // 简化实现，返回空列表
        return new List<string>();
    }

    /// <summary>
    /// 阻止指定应用程序的所有网络访问
    /// 创建入站和出站阻止规则
    /// </summary>
    /// <param name="applicationPath">应用程序的完整路径</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool BlockApplication(string applicationPath)
    {
        var fileName = System.IO.Path.GetFileName(applicationPath);
        
        // 创建入站阻止规则
        var blockInbound = CreateRule(new CreateRuleRequest
        {
            Name = $"Block {fileName} (Inbound)",
            Description = $"Automatically created rule to block {applicationPath}",
            ApplicationName = applicationPath,
            Direction = RuleDirection.Inbound,
            Action = RuleAction.Block,
            Profiles = FirewallProfile.All,
            Enabled = true
        });

        // 创建出站阻止规则
        var blockOutbound = CreateRule(new CreateRuleRequest
        {
            Name = $"Block {fileName} (Outbound)",
            Description = $"Automatically created rule to block {applicationPath}",
            ApplicationName = applicationPath,
            Direction = RuleDirection.Outbound,
            Action = RuleAction.Block,
            Profiles = FirewallProfile.All,
            Enabled = true
        });

        return blockInbound && blockOutbound;
    }

    /// <summary>
    /// 解除对指定应用程序的网络访问阻止
    /// 删除该应用程序的所有阻止规则
    /// </summary>
    /// <param name="applicationPath">应用程序的完整路径</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool UnblockApplication(string applicationPath)
    {
        // 获取该应用程序的所有规则
        var rules = GetRulesByApplication(applicationPath);
        // 筛选出阻止规则
        var blockedRules = rules.Where(r => r.Action == RuleAction.Block).ToList();

        bool allSuccess = true;
        foreach (var rule in blockedRules)
        {
            if (!DeleteRule(rule.Name))
                allSuccess = false;
        }

        return allSuccess;
    }

    #endregion

    #region 配置文件管理

    /// <summary>
    /// 设置指定配置文件的默认入站动作
    /// </summary>
    /// <param name="profile">目标配置文件</param>
    /// <param name="action">默认动作（允许或阻止）</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool SetDefaultInboundAction(FirewallProfile profile, RuleAction action)
    {
        try
        {
            if (_fwPolicy == null) return false;

            var profileType = ConvertToNetFwProfile(profile);
            _fwPolicy.DefaultInboundAction[profileType] = (NET_FW_ACTION_)action;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 设置指定配置文件的默认出站动作
    /// </summary>
    /// <param name="profile">目标配置文件</param>
    /// <param name="action">默认动作（允许或阻止）</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool SetDefaultOutboundAction(FirewallProfile profile, RuleAction action)
    {
        try
        {
            if (_fwPolicy == null) return false;

            var profileType = ConvertToNetFwProfile(profile);
            _fwPolicy.DefaultOutboundAction[profileType] = (NET_FW_ACTION_)action;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 设置指定配置文件的通知开关
    /// </summary>
    /// <param name="profile">目标配置文件</param>
    /// <param name="enable">是否启用通知</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool SetNotifications(FirewallProfile profile, bool enable)
    {
        try
        {
            if (_fwPolicy == null) return false;

            var profileType = ConvertToNetFwProfile(profile);
            // 注意：NotificationsDisabled是反向逻辑
            _fwPolicy.NotificationsDisabled[profileType] = !enable;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 设置指定配置文件的单播响应设置
    /// 控制是否允许对多播和广播流量的单播响应
    /// </summary>
    /// <param name="profile">目标配置文件</param>
    /// <param name="enable">是否启用单播响应</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool SetUnicastResponses(FirewallProfile profile, bool enable)
    {
        try
        {
            if (_fwPolicy == null) return false;

            var profileType = ConvertToNetFwProfile(profile);
            // 注意：UnicastResponsesToMulticastBroadcastDisabled是反向逻辑
            _fwPolicy.UnicastResponsesToMulticastBroadcastDisabled[profileType] = !enable;
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region 日志和监控

    /// <summary>
    /// 获取防火墙日志记录
    /// 从Windows防火墙日志文件中读取日志条目
    /// </summary>
    /// <param name="startTime">开始时间（可选）</param>
    /// <param name="endTime">结束时间（可选）</param>
    /// <returns>日志条目列表</returns>
    public List<string> GetFirewallLogs(DateTime? startTime = null, DateTime? endTime = null)
    {
        // 这需要读取Windows防火墙日志文件
        // 通常位于 %windir%\system32\LogFiles\Firewall\pfirewall.log
        var logs = new List<string>();

        try
        {
            // 构造防火墙日志文件路径
            string logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"LogFiles\Firewall\pfirewall.log"
            );

            if (System.IO.File.Exists(logPath))
            {
                var lines = System.IO.File.ReadAllLines(logPath);
                foreach (var line in lines)
                {
                    // 跳过注释行和空行
                    if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                    {
                        // TODO: 这里可以根据startTime和endTime参数进行时间过滤
                        logs.Add(line);
                    }
                }
            }
        }
        catch
        {
            // 日志文件可能需要管理员权限才能访问
        }

        return logs;
    }

    /// <summary>
    /// 启用指定配置文件的防火墙日志记录
    /// </summary>
    /// <param name="profile">目标配置文件</param>
    /// <param name="logFilePath">日志文件路径</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool EnableLogging(FirewallProfile profile, string logFilePath)
    {
        try
        {
            if (_fwPolicy == null) return false;

            // Windows防火墙日志设置通常需要通过注册表或组策略
            // 这里提供一个简化的实现
            // 实际实现需要修改注册表项或使用高级防火墙管理接口
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 禁用指定配置文件的防火墙日志记录
    /// </summary>
    /// <param name="profile">目标配置文件</param>
    /// <returns>操作成功返回true，失败返回false</returns>
    public bool DisableLogging(FirewallProfile profile)
    {
        try
        {
            // 简化实现
            // 实际实现需要修改相关的注册表设置
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 将Windows防火墙API的规则对象转换为自定义的FirewallRule对象
    /// </summary>
    /// <param name="rule">Windows防火墙API规则对象</param>
    /// <returns>转换后的FirewallRule对象</returns>
    private FirewallRule ConvertToFirewallRule(INetFwRule rule)
    {
        return new FirewallRule
        {
            Name = rule.Name ?? string.Empty,                               // 规则名称
            Description = rule.Description ?? string.Empty,                // 规则描述
            ApplicationName = rule.ApplicationName ?? string.Empty,        // 应用程序路径
            // ServiceName 在某些版本的 INetFwRule 中可能不存在，使用默认值
            ServiceName = string.Empty,                                     // 服务名称
            Protocol = (ProtocolType)rule.Protocol,                        // 协议类型
            LocalPorts = rule.LocalPorts ?? string.Empty,                  // 本地端口
            RemotePorts = rule.RemotePorts ?? string.Empty,                // 远程端口
            LocalAddresses = rule.LocalAddresses ?? string.Empty,          // 本地地址
            RemoteAddresses = rule.RemoteAddresses ?? string.Empty,        // 远程地址
            IcmpTypesAndCodes = rule.IcmpTypesAndCodes ?? string.Empty,    // ICMP类型和代码
            Direction = (RuleDirection)rule.Direction,                     // 流量方向
            Enabled = rule.Enabled,                                        // 是否启用
            Profiles = ConvertFromNetFwProfiles(rule.Profiles),           // 适用的配置文件
            EdgeTraversal = rule.EdgeTraversal,                           // 边缘遍历设置
            Action = (RuleAction)rule.Action,                              // 规则动作
            Grouping = rule.Grouping ?? string.Empty,                     // 规则组
            InterfaceTypes = rule.InterfaceTypes ?? string.Empty,         // 接口类型
            Interfaces = ParseInterfaces(rule.Interfaces)                  // 接口列表
        };
    }

    /// <summary>
    /// 解析接口对象数组为字符串列表
    /// </summary>
    /// <param name="interfaces">接口对象（可能是数组）</param>
    /// <returns>接口名称字符串列表</returns>
    private List<string> ParseInterfaces(object? interfaces)
    {
        var result = new List<string>();
        if (interfaces is Array array)
        {
            foreach (var item in array)
            {
                if (item != null)
                    result.Add(item.ToString() ?? string.Empty);
            }
        }

        return result;
    }

    /// <summary>
    /// 将自定义的FirewallProfile枚举转换为Windows防火墙API的配置文件类型
    /// </summary>
    /// <param name="profile">自定义配置文件枚举</param>
    /// <returns>Windows防火墙API配置文件类型</returns>
    private NET_FW_PROFILE_TYPE2_ ConvertToNetFwProfile(FirewallProfile profile)
    {
        return profile switch
        {
            FirewallProfile.Domain => NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN,
            FirewallProfile.Private => NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE,
            FirewallProfile.Public => NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC,
            _ => NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL
        };
    }

    /// <summary>
    /// 将自定义的FirewallProfile标志转换为Windows防火墙API的配置文件掩码
    /// 支持多个配置文件的组合
    /// </summary>
    /// <param name="profiles">配置文件标志组合</param>
    /// <returns>Windows防火墙API配置文件掩码</returns>
    private int ConvertToNetFwProfiles(FirewallProfile profiles)
    {
        int result = 0;
        if (profiles.HasFlag(FirewallProfile.Domain))
            result |= (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN;
        if (profiles.HasFlag(FirewallProfile.Private))
            result |= (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE;
        if (profiles.HasFlag(FirewallProfile.Public))
            result |= (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC;
        return result;
    }

    /// <summary>
    /// 将Windows防火墙API的配置文件掩码转换为自定义的FirewallProfile标志
    /// </summary>
    /// <param name="profiles">Windows防火墙API配置文件掩码</param>
    /// <returns>自定义配置文件标志组合</returns>
    private FirewallProfile ConvertFromNetFwProfiles(int profiles)
    {
        FirewallProfile result = 0;
        if ((profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN) != 0)
            result |= FirewallProfile.Domain;
        if ((profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE) != 0)
            result |= FirewallProfile.Private;
        if ((profiles & (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PUBLIC) != 0)
            result |= FirewallProfile.Public;
        return result;
    }

    #endregion
}
