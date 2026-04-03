using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommonLibExtended.Helpers;

public static class DebugDumpHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static DebugDumpHelper()
    {
        Options.Converters.Add(new MongoIdConverter());
    }

    public static string DumpItems(object obj)
    {
        try
        {
            return JsonSerializer.Serialize(obj, Options);
        }
        catch (Exception ex)
        {
            return $"<Failed to serialize: {ex.Message}>";
        }
    }
}  