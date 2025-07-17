namespace Utils.Firewall;

/// <summary>
/// Windows防火墙管理API接口
/// 定义了完整的Windows防火墙管理功能，包括状态管理、规则操作、配置文件管理、日志监控等
/// 实现此接口的类应提供对Windows防火墙的全面控制能力
/// </summary>
public interface IFirewallApi
{
    #region 防火墙状态管理

    /// <summary>
    /// 获取防火墙的完整状态信息
    /// 包括所有配置文件的状态、规则统计、启用状态等详细信息
    /// </summary>
    /// <returns>
    /// 包含防火墙完整状态的FirewallStatus对象，包括：
    /// - 整体启用状态
    /// - 各配置文件的详细状态
    /// - 规则数量统计（总数、已启用、入站、出站）
    /// </returns>
    FirewallStatus GetFirewallStatus();

    /// <summary>
    /// 启用指定配置文件的防火墙
    /// 此操作通常需要管理员权限
    /// </summary>
    /// <param name="profile">
    /// 要启用的防火墙配置文件，可以是：
    /// - FirewallProfile.Domain（域网络）
    /// - FirewallProfile.Private（私有网络）
    /// - FirewallProfile.Public（公共网络）
    /// - FirewallProfile.All（所有配置文件，默认值）
    /// </param>
    /// <returns>操作成功返回true，失败返回false（可能由于权限不足或系统错误）</returns>
    bool EnableFirewall(FirewallProfile profile = FirewallProfile.All);

    /// <summary>
    /// 禁用指定配置文件的防火墙
    /// 此操作通常需要管理员权限，禁用防火墙会降低系统安全性
    /// </summary>
    /// <param name="profile">
    /// 要禁用的防火墙配置文件，可以是：
    /// - FirewallProfile.Domain（域网络）
    /// - FirewallProfile.Private（私有网络）
    /// - FirewallProfile.Public（公共网络）
    /// - FirewallProfile.All（所有配置文件，默认值）
    /// </param>
    /// <returns>操作成功返回true，失败返回false（可能由于权限不足或系统错误）</returns>
    bool DisableFirewall(FirewallProfile profile = FirewallProfile.All);

    /// <summary>
    /// 获取指定配置文件的详细状态信息
    /// 包括启用状态、默认动作、通知设置等配置细节
    /// </summary>
    /// <param name="profile">
    /// 要查询的配置文件：
    /// - FirewallProfile.Domain（域网络配置文件）
    /// - FirewallProfile.Private（私有网络配置文件）
    /// - FirewallProfile.Public（公共网络配置文件）
    /// </param>
    /// <returns>
    /// 包含配置文件详细状态的ProfileStatus对象，包括：
    /// - 启用状态
    /// - 是否阻止所有入站流量
    /// - 通知设置
    /// - 单播响应设置
    /// - 默认入站和出站动作
    /// </returns>
    ProfileStatus GetProfileStatus(FirewallProfile profile);

    /// <summary>
    /// 设置指定配置文件的状态和配置
    /// 允许批量修改配置文件的各项设置
    /// </summary>
    /// <param name="profile">要修改的配置文件</param>
    /// <param name="status">
    /// 包含要设置的状态信息的ProfileStatus对象，可设置：
    /// - 是否启用防火墙
    /// - 是否阻止所有入站流量
    /// - 通知开关
    /// - 单播响应设置
    /// - 默认入站和出站动作
    /// </param>
    /// <returns>操作成功返回true，失败返回false</returns>
    bool SetProfileStatus(FirewallProfile profile, ProfileStatus status);

    #endregion

    #region 规则查询

    /// <summary>
    /// 获取系统中的所有防火墙规则
    /// 包括系统预定义规则和用户自定义规则
    /// </summary>
    /// <returns>
    /// 包含所有防火墙规则的列表，每个规则包含完整的配置信息
    /// 如果无法访问防火墙（如权限不足），可能返回空列表
    /// </returns>
    List<FirewallRule> GetAllRules();

    /// <summary>
    /// 根据指定的过滤条件获取防火墙规则
    /// 支持多种条件的组合过滤，提供灵活的规则查询能力
    /// </summary>
    /// <param name="filter">
    /// 过滤条件对象，可包含以下条件：
    /// - NamePattern: 规则名称模式匹配
    /// - Direction: 流量方向（入站/出站）
    /// - Enabled: 启用状态
    /// - Profile: 适用的配置文件
    /// - Protocol: 协议类型
    /// - Action: 规则动作（允许/阻止）
    /// - ApplicationName: 应用程序名称
    /// - Grouping: 规则组名
    /// - Port: 端口号
    /// </param>
    /// <returns>符合过滤条件的防火墙规则列表</returns>
    List<FirewallRule> GetRulesByFilter(RuleFilter filter);

    /// <summary>
    /// 根据规则名称获取特定的防火墙规则
    /// 规则名称在系统中应该是唯一的
    /// </summary>
    /// <param name="ruleName">要查找的规则名称，区分大小写</param>
    /// <returns>
    /// 找到的防火墙规则对象，如果规则不存在则返回null
    /// </returns>
    FirewallRule? GetRuleByName(string ruleName);

    /// <summary>
    /// 获取指定应用程序的所有相关防火墙规则
    /// 用于查看某个应用程序的网络访问权限设置
    /// </summary>
    /// <param name="applicationPath">
    /// 应用程序的完整路径，例如：
    /// "C:\Program Files\MyApp\MyApp.exe"
    /// 路径应使用反斜杠分隔符，区分大小写
    /// </param>
    /// <returns>与指定应用程序相关的所有防火墙规则列表</returns>
    List<FirewallRule> GetRulesByApplication(string applicationPath);

    /// <summary>
    /// 获取指定端口和协议的所有相关防火墙规则
    /// 用于查看某个端口的访问控制设置
    /// </summary>
    /// <param name="port">端口号，范围1-65535</param>
    /// <param name="protocol">
    /// 协议类型，可选值：
    /// - ProtocolType.TCP
    /// - ProtocolType.UDP
    /// - ProtocolType.Any（默认值，匹配所有协议）
    /// </param>
    /// <returns>与指定端口和协议相关的所有防火墙规则列表</returns>
    List<FirewallRule> GetRulesByPort(int port, ProtocolType protocol = ProtocolType.Any);

    /// <summary>
    /// 获取指定规则组的所有防火墙规则
    /// 规则组用于将相关规则进行逻辑分组管理
    /// </summary>
    /// <param name="groupName">
    /// 规则组名称，例如：
    /// - "Core Networking"（核心网络）
    /// - "Remote Desktop"（远程桌面）
    /// - 自定义组名
    /// </param>
    /// <returns>属于指定组的所有防火墙规则列表</returns>
    List<FirewallRule> GetRulesByGroup(string groupName);

    #endregion

    #region 规则管理

    /// <summary>
    /// 创建新的防火墙规则
    /// 此操作通常需要管理员权限
    /// </summary>
    /// <param name="request">
    /// 创建规则的请求对象，必须包含以下信息：
    /// - Name: 规则名称（必须唯一）
    /// - Direction: 流量方向（入站/出站）
    /// - Action: 规则动作（允许/阻止）
    /// - 其他可选配置如端口、地址、应用程序等
    /// </param>
    /// <returns>
    /// 创建成功返回true，失败返回false
    /// 失败原因可能包括：权限不足、规则名称重复、配置无效等
    /// </returns>
    bool CreateRule(CreateRuleRequest request);

    /// <summary>
    /// 更新现有的防火墙规则
    /// 可以修改规则的各项配置，但规则必须已存在
    /// </summary>
    /// <param name="request">
    /// 更新规则的请求对象，必须包含：
    /// - CurrentName: 当前规则名称（用于定位规则）
    /// - 要修改的属性（只有非null的属性会被更新）
    /// </param>
    /// <returns>
    /// 更新成功返回true，失败返回false
    /// 失败原因可能包括：规则不存在、权限不足、配置无效等
    /// </returns>
    bool UpdateRule(UpdateRuleRequest request);

    /// <summary>
    /// 删除指定名称的防火墙规则
    /// 此操作不可逆，删除后规则无法恢复
    /// </summary>
    /// <param name="ruleName">要删除的规则名称，必须完全匹配</param>
    /// <returns>
    /// 删除成功返回true，失败返回false
    /// 如果规则不存在，通常也返回true（幂等操作）
    /// </returns>
    bool DeleteRule(string ruleName);

    /// <summary>
    /// 删除指定组的所有防火墙规则
    /// 批量删除操作，会删除组内的所有规则
    /// </summary>
    /// <param name="groupName">要删除的规则组名称</param>
    /// <returns>
    /// 所有规则都删除成功返回true
    /// 如果部分规则删除失败，返回false
    /// </returns>
    bool DeleteRulesByGroup(string groupName);

    /// <summary>
    /// 启用指定名称的防火墙规则
    /// 启用后规则将开始生效
    /// </summary>
    /// <param name="ruleName">要启用的规则名称</param>
    /// <returns>
    /// 启用成功返回true，失败返回false
    /// 如果规则已经启用，通常返回true
    /// </returns>
    bool EnableRule(string ruleName);

    /// <summary>
    /// 禁用指定名称的防火墙规则
    /// 禁用后规则将不再生效，但仍保留在系统中
    /// </summary>
    /// <param name="ruleName">要禁用的规则名称</param>
    /// <returns>
    /// 禁用成功返回true，失败返回false
    /// 如果规则已经禁用，通常返回true
    /// </returns>
    bool DisableRule(string ruleName);

    /// <summary>
    /// 切换指定规则的启用状态
    /// 如果规则当前启用则禁用，如果禁用则启用
    /// </summary>
    /// <param name="ruleName">要切换状态的规则名称</param>
    /// <returns>
    /// 切换成功返回true，失败返回false
    /// 如果规则不存在，返回false
    /// </returns>
    bool ToggleRule(string ruleName);

    #endregion

    #region 批量操作

    /// <summary>
    /// 批量创建多个防火墙规则
    /// 提供事务性的批量创建能力，提高操作效率
    /// </summary>
    /// <param name="requests">
    /// 创建规则请求列表，每个请求都应包含完整的规则配置
    /// 规则名称在列表中应该唯一
    /// </param>
    /// <returns>
    /// 所有规则都创建成功返回true
    /// 如果任何一个规则创建失败，返回false
    /// 注意：部分实现可能采用"尽力而为"策略，即使部分失败也继续处理其他规则
    /// </returns>
    bool CreateRules(List<CreateRuleRequest> requests);

    /// <summary>
    /// 批量删除多个防火墙规则
    /// 根据规则名称列表进行批量删除操作
    /// </summary>
    /// <param name="ruleNames">要删除的规则名称列表</param>
    /// <returns>
    /// 所有规则都删除成功返回true
    /// 如果任何一个规则删除失败，返回false
    /// 对于不存在的规则，通常视为删除成功
    /// </returns>
    bool DeleteRules(List<string> ruleNames);

    /// <summary>
    /// 批量启用多个防火墙规则
    /// 根据规则名称列表进行批量启用操作
    /// </summary>
    /// <param name="ruleNames">要启用的规则名称列表</param>
    /// <returns>
    /// 所有规则都启用成功返回true
    /// 如果任何一个规则启用失败，返回false
    /// </returns>
    bool EnableRules(List<string> ruleNames);

    /// <summary>
    /// 批量禁用多个防火墙规则
    /// 根据规则名称列表进行批量禁用操作
    /// </summary>
    /// <param name="ruleNames">要禁用的规则名称列表</param>
    /// <returns>
    /// 所有规则都禁用成功返回true
    /// 如果任何一个规则禁用失败，返回false
    /// </returns>
    bool DisableRules(List<string> ruleNames);

    #endregion

    #region 高级功能

    /// <summary>
    /// 将防火墙设置恢复为系统默认值
    /// 此操作会删除所有自定义规则和设置，恢复到Windows默认状态
    /// 操作不可逆，请谨慎使用
    /// </summary>
    /// <returns>
    /// 恢复成功返回true，失败返回false
    /// 此操作通常需要管理员权限
    /// </returns>
    bool RestoreDefaultSettings();

    /// <summary>
    /// 将所有防火墙规则导出到指定文件
    /// 导出格式通常为JSON，包含规则的完整配置信息
    /// </summary>
    /// <param name="filePath">
    /// 导出文件的完整路径，建议使用.json扩展名
    /// 如果文件已存在将被覆盖
    /// </param>
    /// <returns>
    /// 导出成功返回true，失败返回false
    /// 失败原因可能包括：路径无效、权限不足、磁盘空间不足等
    /// </returns>
    bool ExportRules(string filePath);

    /// <summary>
    /// 从指定文件导入防火墙规则
    /// 导入的规则将添加到现有规则中，不会覆盖现有规则
    /// </summary>
    /// <param name="filePath">
    /// 包含规则配置的文件路径，通常为JSON格式
    /// 文件格式应与ExportRules导出的格式兼容
    /// </param>
    /// <returns>
    /// 导入成功返回true，失败返回false
    /// 失败原因可能包括：文件不存在、格式错误、权限不足等
    /// </returns>
    bool ImportRules(string filePath);

    /// <summary>
    /// 获取防火墙规则的详细统计信息
    /// 提供规则数量分析、分类统计、热门应用和端口等信息
    /// </summary>
    /// <returns>
    /// 包含详细统计信息的FirewallStatistics对象，包括：
    /// - 总规则数、启用/禁用规则数
    /// - 按方向、协议、配置文件、动作分类的统计
    /// - 规则最多的应用程序Top列表
    /// - 使用最多的端口Top列表
    /// </returns>
    FirewallStatistics GetStatistics();

    /// <summary>
    /// 获取所有有防火墙规则的应用程序列表
    /// 返回系统中已配置防火墙规则的应用程序路径
    /// </summary>
    /// <returns>
    /// 应用程序完整路径的去重排序列表
    /// 可用于了解哪些应用程序有网络访问配置
    /// </returns>
    List<string> GetApplicationsWithRules();

    /// <summary>
    /// 获取当前系统的活动网络连接列表
    /// 显示当前正在进行的网络连接信息
    /// </summary>
    /// <returns>
    /// 活动连接的描述列表，格式可能包含：
    /// - 协议类型
    /// - 本地地址和端口
    /// - 远程地址和端口
    /// - 连接状态
    /// - 关联的进程信息
    /// </returns>
    List<string> GetActiveConnections();

    /// <summary>
    /// 阻止指定应用程序的所有网络访问
    /// 自动创建入站和出站阻止规则，完全阻止应用程序的网络通信
    /// </summary>
    /// <param name="applicationPath">
    /// 要阻止的应用程序完整路径
    /// 例如："C:\Program Files\MyApp\MyApp.exe"
    /// </param>
    /// <returns>
    /// 阻止成功返回true，失败返回false
    /// 成功时会创建两个规则：入站阻止和出站阻止
    /// </returns>
    bool BlockApplication(string applicationPath);

    /// <summary>
    /// 解除对指定应用程序的网络访问阻止
    /// 删除该应用程序的所有阻止规则，恢复其网络访问能力
    /// </summary>
    /// <param name="applicationPath">
    /// 要解除阻止的应用程序完整路径
    /// 路径必须与BlockApplication时使用的路径完全一致
    /// </param>
    /// <returns>
    /// 解除阻止成功返回true，失败返回false
    /// 将删除该应用程序的所有阻止类型规则
    /// </returns>
    bool UnblockApplication(string applicationPath);

    #endregion

    #region 配置文件管理

    /// <summary>
    /// 设置指定配置文件的默认入站动作
    /// 当没有匹配的规则时，系统将使用此默认动作处理入站流量
    /// </summary>
    /// <param name="profile">
    /// 目标配置文件：
    /// - FirewallProfile.Domain（域网络）
    /// - FirewallProfile.Private（私有网络）
    /// - FirewallProfile.Public（公共网络）
    /// </param>
    /// <param name="action">
    /// 默认动作：
    /// - RuleAction.Allow（允许）- 安全性较低但兼容性好
    /// - RuleAction.Block（阻止）- 安全性高但可能影响某些功能
    /// </param>
    /// <returns>设置成功返回true，失败返回false</returns>
    bool SetDefaultInboundAction(FirewallProfile profile, RuleAction action);

    /// <summary>
    /// 设置指定配置文件的默认出站动作
    /// 当没有匹配的规则时，系统将使用此默认动作处理出站流量
    /// </summary>
    /// <param name="profile">
    /// 目标配置文件：
    /// - FirewallProfile.Domain（域网络）
    /// - FirewallProfile.Private（私有网络）
    /// - FirewallProfile.Public（公共网络）
    /// </param>
    /// <param name="action">
    /// 默认动作：
    /// - RuleAction.Allow（允许）- 通常为默认设置
    /// - RuleAction.Block（阻止）- 高安全环境下使用
    /// </param>
    /// <returns>设置成功返回true，失败返回false</returns>
    bool SetDefaultOutboundAction(FirewallProfile profile, RuleAction action);

    /// <summary>
    /// 设置指定配置文件的通知开关
    /// 控制当防火墙阻止程序时是否显示通知
    /// </summary>
    /// <param name="profile">目标配置文件</param>
    /// <param name="enable">
    /// 是否启用通知：
    /// - true: 显示通知，用户体验好但可能干扰
    /// - false: 不显示通知，静默处理
    /// </param>
    /// <returns>设置成功返回true，失败返回false</returns>
    bool SetNotifications(FirewallProfile profile, bool enable);

    /// <summary>
    /// 设置指定配置文件的单播响应设置
    /// 控制是否允许对多播和广播流量进行单播响应
    /// 这个设置影响某些网络发现和文件共享功能
    /// </summary>
    /// <param name="profile">目标配置文件</param>
    /// <param name="enable">
    /// 是否允许单播响应：
    /// - true: 允许，提高网络兼容性
    /// - false: 禁用，提高安全性
    /// </param>
    /// <returns>设置成功返回true，失败返回false</returns>
    bool SetUnicastResponses(FirewallProfile profile, bool enable);

    #endregion

    #region 日志和监控

    /// <summary>
    /// 获取防火墙日志记录
    /// 从Windows防火墙日志文件中读取日志条目，用于安全分析和故障排除
    /// </summary>
    /// <param name="startTime">
    /// 开始时间（可选），如果指定，只返回此时间之后的日志
    /// 如果为null，返回所有可用日志
    /// </param>
    /// <param name="endTime">
    /// 结束时间（可选），如果指定，只返回此时间之前的日志
    /// 如果为null，返回到当前时间的所有日志
    /// </param>
    /// <returns>
    /// 日志条目字符串列表，每个条目包含：
    /// - 时间戳
    /// - 动作（允许/阻止/丢弃）
    /// - 协议
    /// - 源和目标IP地址
    /// - 端口信息
    /// - 规则名称等信息
    /// </returns>
    List<string> GetFirewallLogs(DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// 启用指定配置文件的防火墙日志记录
    /// 配置防火墙记录被阻止或允许的连接到指定日志文件
    /// </summary>
    /// <param name="profile">要启用日志的配置文件</param>
    /// <param name="logFilePath">
    /// 日志文件的完整路径，例如：
    /// "C:\Windows\System32\LogFiles\Firewall\pfirewall.log"
    /// 路径目录必须存在且有写入权限
    /// </param>
    /// <returns>
    /// 启用成功返回true，失败返回false
    /// 失败原因可能包括：路径无效、权限不足等
    /// </returns>
    bool EnableLogging(FirewallProfile profile, string logFilePath);

    /// <summary>
    /// 禁用指定配置文件的防火墙日志记录
    /// 停止记录防火墙活动到日志文件，可以节省磁盘空间和系统性能
    /// </summary>
    /// <param name="profile">要禁用日志的配置文件</param>
    /// <returns>
    /// 禁用成功返回true，失败返回false
    /// 已经禁用的日志记录通常返回true
    /// </returns>
    bool DisableLogging(FirewallProfile profile);

    #endregion
}
