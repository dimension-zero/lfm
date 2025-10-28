using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lfm.Core.Utilities;

/// <summary>
/// Helper for consistent JSON output formatting across commands
/// </summary>
public static class JsonOutputHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Writes an object as formatted JSON to console
    /// </summary>
    public static void WriteJsonToConsole<T>(T data)
    {
        var json = JsonSerializer.Serialize(data, DefaultOptions);
        Console.WriteLine(json);
    }

    /// <summary>
    /// Serializes an object to JSON string
    /// </summary>
    public static string SerializeToJson<T>(T data)
    {
        return JsonSerializer.Serialize(data, DefaultOptions);
    }

    /// <summary>
    /// Gets the default JSON serialization options
    /// </summary>
    public static JsonSerializerOptions GetJsonOptions() => DefaultOptions;
}
