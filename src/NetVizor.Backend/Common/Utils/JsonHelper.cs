using System.Text.Json;

namespace Common.Utils;

public static class JsonHelper
{
    public static readonly JsonSerializerOptions CamelCaseOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string ToJson<T>(T value) =>
        JsonSerializer.Serialize(value, CamelCaseOptions);

    public static T? FromJson<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, CamelCaseOptions);
}
