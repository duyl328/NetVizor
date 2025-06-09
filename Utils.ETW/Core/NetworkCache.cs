using Utils.ETW.Models;

namespace Utils.ETW.Core;

/// <summary>
/// 网络信息缓存【全局 TCP、UDP 等连接信息存储】
/// </summary>
public sealed class NetworkCache
{
    private static readonly Lazy<NetworkCache> _instance = new(() => new NetworkCache());

    public static NetworkCache Instance => _instance.Value;

    /// <summary>
    /// 初始化字典大小 避免频繁扩容
    /// </summary>
    private static readonly int InitCapacity = 1_000;

    // 私有构造函数，禁止外部创建实例
    private NetworkCache()
    {
        // 初始化缓存等操作
    }

    /// <summary>
    /// 缓存字典
    /// </summary>
    private readonly Dictionary<string, NetworkModel> _cache = new(InitCapacity);

    // 设置缓存
    public void Set(string key, NetworkModel value)
    {
        lock (_cache)
        {
            _cache[key] = value;
        }
    }

    public NetworkModel? Get(string key)
    {
        lock (_cache)
        {
            return _cache.GetValueOrDefault(key);
        }
    }

    // 移除缓存
    public void Remove(string key)
    {
        lock (_cache)
        {
            _cache.Remove(key);
        }
    }
}
