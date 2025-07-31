using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utils;

public static class JsonHelper
{
    public static readonly JsonSerializerOptions CamelCaseOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new IpAddressConverter(), new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
        },
    };

    public static string ToJson<T>(T value) =>
        JsonSerializer.Serialize(value, CamelCaseOptions);

    public static T? FromJson<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, CamelCaseOptions);
}

/*
 IPAddress.ScopeId 属性在某些情况下（如 IPv4 类型）是不被支持的，会抛出异常。
这是 .NET 的设计问题，访问 ScopeId 只有在 IPv6 地址中才有意义，在 IPv4 中访问它就会抛出这个 10045 错误。
 */
public class IpAddressConverter : JsonConverter<IPAddress>
{
    public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var ipString = reader.GetString() ?? String.Empty;
        return IPAddress.Parse(ipString);
    }

    public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value?.ToString());
    }
}