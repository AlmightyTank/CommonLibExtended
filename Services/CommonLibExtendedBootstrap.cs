using CommonLibExtended.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Logging;
using SPTarkov.Server.Core.Models.Utils;
using System.Reflection;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;

namespace CommonLibExtended.Services;

[Injectable(InjectionType.Singleton)]
public sealed class CommonLibExtendedBootstrap(
    ISptLogger<CommonLibExtendedBootstrap> logger,
    JsonConfigLoader jsonConfigLoader,
    ItemModificationService itemModificationService,
    CompatibilityService compatibilityService,
    ModPathHelper modPathHelper)
{
    private readonly ISptLogger<CommonLibExtendedBootstrap> _logger = logger;
    private readonly JsonConfigLoader _jsonConfigLoader = jsonConfigLoader;
    private readonly ItemModificationService _itemModificationService = itemModificationService;
    private readonly CompatibilityService _compatibilityService = compatibilityService;
    private readonly ModPathHelper _modPathHelper = modPathHelper;

    public async Task CreateCustomItems(Assembly assembly, string? relativePath = null)
    {
        var defaultDir = Path.Combine("db", "CustomItems");
        var finalRelativePath = relativePath ?? defaultDir;

        await CreateCustomItems(assembly, [finalRelativePath]);
    }

    public async Task CreateCustomItems(Assembly assembly, params string[] relativePaths)
    {
        try
        {
            var modRoot = _modPathHelper.GetModRoot(assembly);

            if (relativePaths == null || relativePaths.Length == 0)
            {
                _logger.Warning("No relative paths supplied to CreateCustomItems");
                return;
            }

            foreach (var relativePath in relativePaths)
            {
                var fullPath = _modPathHelper.GetFullPath(assembly, relativePath);

                if (!Directory.Exists(fullPath) && !File.Exists(fullPath))
                {
                    _logger.Error($"Directory or file not found at {fullPath}");
                    return;
                }
            }

            _compatibilityService.Initialize();

            var requests = _jsonConfigLoader.LoadFromRelativePaths(assembly, relativePaths);

            if (requests.Count == 0)
            {
                _logger.Warning($"No valid item configs found for mod root {modRoot}");
                return;
            }

            var processedCount = 0;

            foreach (var request in requests)
            {
                try
                {
                    request.Config.Validate(request.ItemId);
                    request.Extras.Validate(request.ItemId);

                    _itemModificationService.Process(request);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed processing {request.ItemId} from {request.FilePath}: {ex}");
                }
            }

            _compatibilityService.ProcessCompatibilityInfo();

            if (_logger.IsLogEnabled(LogLevel.Debug))
            {
                _logger.Debug($"Processed {processedCount} custom item request(s) from {relativePaths.Length} path(s)");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading configs: {ex}");
        }

        await Task.CompletedTask;
    }
}