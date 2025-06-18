namespace Utils.ETW;

/// <summary>
/// 全局网络信息
/// </summary>
public sealed class GlobalNet
{
    private static readonly Lazy<GlobalNet> _instance = new Lazy<GlobalNet>(() => new GlobalNet());

    public static GlobalNet Instance => _instance.Value;
    
    

    // 私有构造函数，防止外部实例化
    private GlobalNet()
    {
    }
}
