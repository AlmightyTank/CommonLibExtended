using CommonLibExtended.Constants;
using CommonLibExtended.Core;
using CommonLibExtended.Models;
using CommonLibExtended.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Models;

namespace CommonLibExtended.Helpers;

[Injectable]
public sealed class PresetTraderOfferHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService,
    CLESettings settings,
    PresetBuildHelper presetBuildHelper,
    BuiltPresetCache builtPresetCache,
    PresetRegistryService presetRegistryService,
    TraderOfferHelper traderOfferBuilder)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;
    private readonly DatabaseService _databaseService = databaseService;
    private readonly CLESettings _settings = settings;
    private readonly PresetBuildHelper _presetBuildHelper = presetBuildHelper;
    private readonly BuiltPresetCache _builtPresetCache = builtPresetCache;
    private readonly PresetRegistryService _presetRegistryService = presetRegistryService;
    private readonly TraderOfferHelper _traderOfferBuilder = traderOfferBuilder;

    public void Process(ItemModificationRequest request)
    {
        if (request?.Extras?.PresetTraders == null || request.Extras.PresetTraders.Count == 0)
        {
            return;
        }

        foreach (var (traderKey, assortEntries) in request.Extras.PresetTraders)
        {
            var traderId = ResolveTraderId(traderKey);
            if (string.IsNullOrWhiteSpace(traderId))
            {
                _debugLogHelper.LogError("PresetTraderOffer", $"Could not resolve trader '{traderKey}'");
                continue;
            }

            if (assortEntries == null || assortEntries.Count == 0)
            {
                continue;
            }

            foreach (var (sourceAssortId, config) in assortEntries)
            {
                if (config == null || string.IsNullOrWhiteSpace(sourceAssortId) || string.IsNullOrWhiteSpace(config.PresetId))
                {
                    continue;
                }

                var preset = _presetRegistryService.GetById(config.PresetId);
                if (preset == null)
                {
                    _debugLogHelper.LogError(
                        "PresetTraderOffer",
                        $"Preset {config.PresetId} not found in preset registry for item {request.ItemId}");
                    continue;
                }

                var builtPreset = AddPresetOfferToTrader(traderId, sourceAssortId, preset, config);
                if (builtPreset != null)
                {
                    _builtPresetCache.Store(config.PresetId, sourceAssortId, builtPreset);
                }
            }
        }
    }

    private BuiltPresetResult? AddPresetOfferToTrader(
        string traderId,
        string sourceAssortId,
        Preset preset,
        PresetTraderConfig config)
    {
        if (!_databaseService.GetTraders().TryGetValue(traderId, out var trader) || trader?.Assort == null)
        {
            _debugLogHelper.LogError("PresetTraderOffer", $"Trader {traderId} not found or assort is null");
            return null;
        }

        var builtPreset = _presetBuildHelper.BuildForTrader(preset, sourceAssortId, "PresetTraderOffer");
        if (builtPreset == null)
        {
            _debugLogHelper.LogError(
                "PresetTraderOffer",
                $"Failed to build preset {preset.Id} for trader {traderId} assort {sourceAssortId}");
            return null;
        }

        var offerId = builtPreset.RootBuiltItemId?.ToString();
        if (string.IsNullOrWhiteSpace(offerId))
        {
            _debugLogHelper.LogError(
                "PresetTraderOffer",
                $"Built preset {preset.Id} returned invalid RootBuiltItemId for trader {traderId}");
            return null;
        }

        var success = _traderOfferBuilder.ApplyOffer(
            trader.Assort,
            offerId,
            builtPreset.Items,
            config.Barters,
            config.LoyalLevelItems,
            "PresetTraderOffer",
            $"sourceAssort={sourceAssortId}, preset={preset.Id}, trader={traderId}");

        return success ? builtPreset : null;
    }

    private string? ResolveTraderId(string? traderKey)
    {
        if (_settings.ForceAllItemsToDefaultTrader)
        {
            return _settings.DefaultTraderId;
        }

        if (string.IsNullOrWhiteSpace(traderKey))
        {
            return _settings.DefaultTraderId;
        }

        if (Maps.TraderMap.TryGetValue(traderKey.ToLowerInvariant(), out var traderId))
        {
            return traderId;
        }

        return traderKey;
    }
}