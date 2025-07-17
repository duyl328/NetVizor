namespace Utils.Firewall;

/// <summary>
/// 防火墙服务接口（用于依赖注入）
/// </summary>

/// <summary>
/// 防火墙服务接口，用于依赖注入和测试
/// 提供统一的防火墙管理功能，包括规则管理、状态控制和统计信息获取
/// </summary>
/// <remarks>
/// 这个接口抽象了底层防火墙API的实现细节，使得可以：
/// 1. 在不同的防火墙实现之间切换（Windows Defender、第三方防火墙等）
/// 2. 方便进行单元测试（通过Mock接口）
/// 3. 支持依赖注入，提高代码的可测试性和可维护性
/// </remarks>
public interface IFirewallService
{
    /// <summary>
    /// 异步获取防火墙状态信息
    /// </summary>
    /// <returns>包含防火墙各配置文件启用状态的 <see cref="FirewallStatus"/> 对象</returns>
    /// <remarks>
    /// 返回的状态包括：
    /// - Domain Profile（域配置文件）状态
    /// - Private Profile（专用配置文件）状态  
    /// - Public Profile（公用配置文件）状态
    /// </remarks>
    Task<FirewallStatus> GetStatusAsync();

    /// <summary>
    /// 异步获取防火墙规则列表
    /// </summary>
    /// <param name="filter">可选的规则过滤器，用于筛选特定条件的规则。如果为null，则返回所有规则</param>
    /// <returns>符合筛选条件的防火墙规则列表</returns>
    /// <remarks>
    /// 过滤器可以根据以下条件筛选规则：
    /// - 规则名称（支持模糊匹配）
    /// - 协议类型（TCP、UDP、ICMP等）
    /// - 方向（入站/出站）
    /// - 动作（允许/阻止）
    /// - 启用状态
    /// </remarks>
    Task<List<FirewallRule>> GetRulesAsync(RuleFilter? filter = null);

    /// <summary>
    /// 异步根据规则名称获取特定的防火墙规则
    /// </summary>
    /// <param name="ruleName">要查找的规则名称，大小写敏感</param>
    /// <returns>匹配的防火墙规则，如果未找到则返回null</returns>
    /// <exception cref="ArgumentNullException">当ruleName为null或空字符串时抛出</exception>
    Task<FirewallRule?> GetRuleAsync(string ruleName);

    /// <summary>
    /// 异步创建新的防火墙规则
    /// </summary>
    /// <param name="request">包含新规则详细信息的创建请求</param>
    /// <returns>如果规则创建成功返回true，否则返回false</returns>
    /// <remarks>
    /// 创建规则时需要注意：
    /// 1. 规则名称必须唯一，不能与现有规则重复
    /// 2. 端口范围格式：单个端口"80"，多个端口"80,443"，范围"1024-65535"
    /// 3. IP地址格式：单个IP"192.168.1.1"，网段"192.168.1.0/24"，多个IP用逗号分隔
    /// </remarks>
    /// <exception cref="ArgumentNullException">当request为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当规则名称已存在时抛出</exception>
    Task<bool> CreateRuleAsync(CreateRuleRequest request);

    /// <summary>
    /// 异步更新现有的防火墙规则
    /// </summary>
    /// <param name="request">包含更新信息的请求，必须指定要更新的规则名称</param>
    /// <returns>如果规则更新成功返回true，否则返回false</returns>
    /// <remarks>
    /// 更新操作会完全替换现有规则的配置
    /// 如果只需要启用/禁用规则，建议使用专门的启用/禁用方法
    /// </remarks>
    /// <exception cref="ArgumentNullException">当request为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当指定的规则不存在时抛出</exception>
    Task<bool> UpdateRuleAsync(UpdateRuleRequest request);

    /// <summary>
    /// 异步删除指定的防火墙规则
    /// </summary>
    /// <param name="ruleName">要删除的规则名称</param>
    /// <returns>如果规则删除成功返回true，否则返回false</returns>
    /// <remarks>
    /// 注意：删除操作是不可逆的，建议在删除前先备份重要规则
    /// 系统内置的默认规则通常不能被删除
    /// </remarks>
    /// <exception cref="ArgumentNullException">当ruleName为null或空字符串时抛出</exception>
    Task<bool> DeleteRuleAsync(string ruleName);

    /// <summary>
    /// 异步启用指定配置文件的防火墙
    /// </summary>
    /// <param name="profile">要启用的防火墙配置文件，默认为所有配置文件</param>
    /// <returns>如果启用成功返回true，否则返回false</returns>
    /// <remarks>
    /// Windows防火墙包含三个配置文件：
    /// - Domain: 当计算机连接到域时使用
    /// - Private: 当计算机连接到私有网络时使用
    /// - Public: 当计算机连接到公共网络时使用
    /// </remarks>
    Task<bool> EnableFirewallAsync(FirewallProfile profile = FirewallProfile.All);

    /// <summary>
    /// 异步禁用指定配置文件的防火墙
    /// </summary>
    /// <param name="profile">要禁用的防火墙配置文件，默认为所有配置文件</param>
    /// <returns>如果禁用成功返回true，否则返回false</returns>
    /// <remarks>
    /// 警告：禁用防火墙会降低系统安全性，请谨慎操作
    /// 建议只在必要的测试或故障排除场景中临时禁用
    /// </remarks>
    Task<bool> DisableFirewallAsync(FirewallProfile profile = FirewallProfile.All);

    /// <summary>
    /// 异步获取防火墙统计信息
    /// </summary>
    /// <returns>包含防火墙运行统计数据的 <see cref="FirewallStatistics"/> 对象</returns>
    /// <remarks>
    /// 统计信息包括：
    /// - 阻止的连接数量
    /// - 允许的连接数量  
    /// - 活跃规则数量
    /// - 最近活动的时间戳
    /// </remarks>
    Task<FirewallStatistics> GetStatisticsAsync();

    /// <summary>
    /// 异步阻止指定应用程序的网络访问
    /// </summary>
    /// <param name="applicationPath">应用程序的完整文件路径</param>
    /// <returns>如果阻止操作成功返回true，否则返回false</returns>
    /// <remarks>
    /// 此方法会创建一个阻止规则来禁止指定应用程序的所有网络连接
    /// 路径必须是可执行文件(.exe)的完整路径
    /// </remarks>
    /// <exception cref="ArgumentNullException">当applicationPath为null或空字符串时抛出</exception>
    /// <exception cref="FileNotFoundException">当指定的应用程序文件不存在时抛出</exception>
    Task<bool> BlockApplicationAsync(string applicationPath);

    /// <summary>
    /// 异步解除对指定应用程序的网络访问阻止
    /// </summary>
    /// <param name="applicationPath">应用程序的完整文件路径</param>
    /// <returns>如果解除阻止操作成功返回true，否则返回false</returns>
    /// <remarks>
    /// 此方法会删除之前为该应用程序创建的阻止规则
    /// 如果应用程序没有被阻止，操作仍会返回true
    /// </remarks>
    /// <exception cref="ArgumentNullException">当applicationPath为null或空字符串时抛出</exception>
    Task<bool> UnblockApplicationAsync(string applicationPath);
}
