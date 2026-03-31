using System.Reflection;
using System.Text.Json;
using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using WTTServerCommonLib.Models;

namespace CommonLibExtended.Services;

[Injectable]
public sealed class JsonConfigLoader(
    ModPathHelper modPathHelper,
    DebugLogHelper debugLogHelper)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public List<ItemModificationRequest> LoadFromRelativePaths(Assembly modAssembly, params string[] relativePaths)
    {
        var results = new List<ItemModificationRequest>();

        foreach (var relativePath in relativePaths)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                continue;
            }

            var fullPath = modPathHelper.GetFullPath(modAssembly, relativePath);
            results.AddRange(LoadFromFullPath(fullPath));
        }

        return results;
    }

    public List<ItemModificationRequest> LoadFromRelativePath(Assembly modAssembly, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return [];
        }

        var fullPath = modPathHelper.GetFullPath(modAssembly, relativePath);
        return LoadFromFullPath(fullPath);
    }

    private List<ItemModificationRequest> LoadFromFullPath(string fullPath)
    {
        var results = new List<ItemModificationRequest>();

        if (File.Exists(fullPath))
        {
            debugLogHelper.Log(nameof(JsonConfigLoader), $"Loading single file: {fullPath}");
            LoadFile(fullPath, results);
            debugLogHelper.Log(nameof(JsonConfigLoader), $"Loaded {results.Count} item request(s) from file {fullPath}");
            return results;
        }

        if (!Directory.Exists(fullPath))
        {
            debugLogHelper.LogError(nameof(JsonConfigLoader), $"Path not found: {fullPath}");
            return results;
        }

        var jsonFiles = Directory.GetFiles(fullPath, "*.json", SearchOption.AllDirectories);

        debugLogHelper.Log(nameof(JsonConfigLoader), $"Found {jsonFiles.Length} json file(s) in {fullPath}");

        foreach (var file in jsonFiles)
        {
            var before = results.Count;
            LoadFile(file, results);
            var added = results.Count - before;

            debugLogHelper.Log(nameof(JsonConfigLoader), $"Loaded {added} item request(s) from {file}");
        }

        debugLogHelper.Log(nameof(JsonConfigLoader), $"Total loaded item request(s): {results.Count}");

        return results;
    }

    private void LoadFile(string file, List<ItemModificationRequest> results)
    {
        try
        {
            var json = File.ReadAllText(file);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                debugLogHelper.LogError(nameof(JsonConfigLoader), $"Root JSON is not an object in {file}");
                return;
            }

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                var itemId = property.Name;
                var raw = property.Value.GetRawText();

                var config = JsonSerializer.Deserialize<ItemModificationConfig>(raw, JsonOptions);
                var extras = JsonSerializer.Deserialize<ItemModificationExtras>(raw, JsonOptions);

                if (string.IsNullOrWhiteSpace(itemId))
                {
                    debugLogHelper.LogError(nameof(JsonConfigLoader), $"Blank item id in {file}");
                    continue;
                }

                if (config == null)
                {
                    debugLogHelper.LogError(nameof(JsonConfigLoader), $"Null WTT config for item {itemId} in {file}");
                    continue;
                }

                results.Add(new ItemModificationRequest
                {
                    ItemId = itemId,
                    Config = config,
                    Extras = extras ?? new ItemModificationExtras(),
                    FilePath = file
                });
            }
        }
        catch (Exception ex)
        {
            debugLogHelper.LogError(nameof(JsonConfigLoader), $"Error loading file {file}: {ex}");
        }
    }
}