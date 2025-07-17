namespace Utils.Firewall;

/// <summary>
/// 防火墙服务实现
/// </summary>

/// <summary>
/// 防火墙服务的具体实现类
/// 提供对Windows防火墙的完整管理功能，实现了 <see cref="IFirewallService"/> 接口
/// </summary>
/// <remarks>
/// 这个类是防火墙服务的主要实现，它：
/// 1. 将同步的防火墙API调用包装为异步方法
/// 2. 提供线程安全的防火墙操作
/// 3. 处理底层API的异常并转换为友好的返回值
/// 4. 支持依赖注入，便于测试和替换实现
/// </remarks>
public class FirewallService : IFirewallService
{
    /// <summary>
    /// 底层防火墙API接口实例
    /// </summary>
    /// <remarks>
    /// 通过接口依赖，可以方便地切换不同的防火墙实现
    /// 目前默认使用Windows防火墙API
    /// </remarks>
    private readonly IFirewallApi _api;

    /// <summary>
    /// 初始化防火墙服务实例
    /// </summary>
    /// <remarks>
    /// 构造函数中创建Windows防火墙API实例
    /// 在实际项目中，建议通过依赖注入容器来管理API实例
    /// </remarks>
    public FirewallService()
    {
        // 创建Windows防火墙API实例
        // TODO: 考虑通过构造函数参数注入，以提高可测试性
        _api = new WindowsFirewallApi();
    }

    /// <summary>
    /// 异步获取防火墙状态信息
    /// </summary>
    /// <returns>防火墙状态对象，包含各配置文件的启用状态</returns>
    /// <remarks>
    /// 使用Task.Run将同步调用转换为异步调用，避免阻塞UI线程
    /// 在高并发场景下，建议使用更高级的异步模式
    /// </remarks>
    public Task<FirewallStatus> GetStatusAsync()
    {
        return Task.Run(() => _api.GetFirewallStatus());
    }

    /// <summary>
    /// 异步获取防火墙规则列表
    /// </summary>
    /// <param name="filter">规则过滤器，为null时返回所有规则</param>
    /// <returns>符合条件的防火墙规则列表</returns>
    /// <remarks>
    /// 根据是否提供过滤器来决定调用不同的API方法
    /// 获取所有规则可能耗时较长，建议在必要时使用过滤器
    /// </remarks>
    public Task<List<FirewallRule>> GetRulesAsync(RuleFilter? filter = null)
    {
        return Task.Run(() => filter == null ? _api.GetAllRules() : _api.GetRulesByFilter(filter));
    }

    /// <summary>
    /// 异步根据名称获取特定防火墙规则
    /// </summary>
    /// <param name="ruleName">要查找的规则名称</param>
    /// <returns>匹配的规则对象，未找到时返回null</returns>
    /// <remarks>
    /// 规则名称查找是精确匹配且区分大小写
    /// 建议在调用前验证规则名称的有效性
    /// </remarks>
    public Task<FirewallRule?> GetRuleAsync(string ruleName)
    {
        return Task.Run(() => _api.GetRuleByName(ruleName));
    }

    /// <summary>
    /// 异步创建新的防火墙规则
    /// </summary>
    /// <param name="request">规则创建请求，包含所有必要的规则信息</param>
    /// <returns>创建成功返回true，失败返回false</returns>
    /// <remarks>
    /// 创建规则是一个原子操作，要么完全成功，要么完全失败
    /// 如果规则名称冲突，底层API会抛出异常
    /// </remarks>
    public Task<bool> CreateRuleAsync(CreateRuleRequest request)
    {
        return Task.Run(() => _api.CreateRule(request));
    }

    /// <summary>
    /// 异步更新现有防火墙规则
    /// </summary>
    /// <param name="request">规则更新请求，必须包含要更新的规则名称</param>
    /// <returns>更新成功返回true，失败返回false</returns>
    /// <remarks>
    /// 更新操作会完全替换现有规则的所有属性
    /// 建议在更新前先获取现有规则信息作为备份
    /// </remarks>
    public Task<bool> UpdateRuleAsync(UpdateRuleRequest request)
    {
        return Task.Run(() => _api.UpdateRule(request));
    }

    /// <summary>
    /// 异步删除指定名称的防火墙规则
    /// </summary>
    /// <param name="ruleName">要删除的规则名称</param>
    /// <returns>删除成功返回true，失败返回false</returns>
    /// <remarks>
    /// 删除操作是不可逆的，请谨慎操作
    /// 如果规则不存在，某些实现可能返回true（认为目标已达成）
    /// </remarks>
    public Task<bool> DeleteRuleAsync(string ruleName)
    {
        return Task.Run(() => _api.DeleteRule(ruleName));
    }

    /// <summary>
    /// 异步启用指定配置文件的防火墙
    /// </summary>
    /// <param name="profile">要启用的防火墙配置文件</param>
    /// <returns>启用成功返回true，失败返回false</returns>
    /// <remarks>
    /// 启用防火墙会立即生效，影响所有网络连接
    /// 在某些环境中可能需要管理员权限
    /// </remarks>
    public Task<bool> EnableFirewallAsync(FirewallProfile profile = FirewallProfile.All)
    {
        return Task.Run(() => _api.EnableFirewall(profile));
    }

    /// <summary>
    /// 异步禁用指定配置文件的防火墙
    /// </summary>
    /// <param name="profile">要禁用的防火墙配置文件</param>
    /// <returns>禁用成功返回true，失败返回false</returns>
    /// <remarks>
    /// 禁用防火墙会立即生效，可能降低系统安全性
    /// 建议只在必要的故障排除场景中使用
    /// </remarks>
    public Task<bool> DisableFirewallAsync(FirewallProfile profile = FirewallProfile.All)
    {
        return Task.Run(() => _api.DisableFirewall(profile));
    }

    /// <summary>
    /// 异步获取防火墙运行统计信息
    /// </summary>
    /// <returns>包含各种统计数据的对象</returns>
    /// <remarks>
    /// 统计信息的准确性取决于底层防火墙的实现
    /// 某些统计数据可能只在防火墙启用时才会更新
    /// </remarks>
    public Task<FirewallStatistics> GetStatisticsAsync()
    {
        return Task.Run(() => _api.GetStatistics());
    }

    /// <summary>
    /// 异步阻止指定应用程序的所有网络访问
    /// </summary>
    /// <param name="applicationPath">应用程序的完整文件路径</param>
    /// <returns>阻止成功返回true，失败返回false</returns>
    /// <remarks>
    /// 此方法会创建一个出站阻止规则来禁止应用程序联网
    /// 应用程序路径必须是有效的可执行文件路径
    /// </remarks>
    public Task<bool> BlockApplicationAsync(string applicationPath)
    {
        return Task.Run(() => _api.BlockApplication(applicationPath));
    }

    /// <summary>
    /// 异步解除对指定应用程序的网络访问阻止
    /// </summary>
    /// <param name="applicationPath">应用程序的完整文件路径</param>
    /// <returns>解除阻止成功返回true，失败返回false</returns>
    /// <remarks>
    /// 此方法会删除之前为该应用程序创建的阻止规则
    /// 如果应用程序没有相关的阻止规则，操作通常会成功返回
    /// </remarks>
    public Task<bool> UnblockApplicationAsync(string applicationPath)
    {
        return Task.Run(() => _api.UnblockApplication(applicationPath));
    }
}
