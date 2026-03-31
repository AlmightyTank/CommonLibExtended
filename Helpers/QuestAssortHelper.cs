using CommonLibExtended.Constants;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Spt.Templates;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Models;

namespace CommonLibExtended.Helpers;

[Injectable(InjectionType.Singleton)]
public sealed class QuestAssortHelper
{
    private const string StartedStatus = "Started";
    private const string SuccessStatus = "Success";
    private const string FailStatus = "Fail";

    private readonly DebugLogHelper _debug;
    private readonly DatabaseService? _databaseService;


    public QuestAssortHelper(
        DebugLogHelper debugLogHelper,
        DatabaseService databaseService)
    {
        _debug = debugLogHelper;
        _databaseService = databaseService;
    }

    public void Process(ItemModificationRequest request)
    {
        if (!request.Extras.AddToQuestAssorts || request.Extras.QuestAssorts.Length == 0)
        {
            return;
        }

        var tables = _databaseService.GetTables();

        foreach (var config in request.Extras.QuestAssorts)
        {
            if (config == null)
            {
                continue;
            }

            ProcessSingle(request, config, tables.Traders, tables.Templates.Quests);
        }
    }

    private void ProcessSingle(
        ItemModificationRequest request,
        QuestAssortConfig config,
        Dictionary<MongoId, Trader> traders,
        Dictionary<MongoId, Quest> quests)
    {
        var traderId = ResolveTraderId(config.TraderId, traders);

        if (string.IsNullOrWhiteSpace(traderId))
        {
            _debug.LogError("QuestAssortHelper", $"Invalid trader '{config.TraderId}'");
            return;
        }

        MongoId traderMongoId = traderId;

        var assortId = ResolveAssortId(request, traderId, config);
        if (string.IsNullOrWhiteSpace(assortId))
        {
            _debug.LogError("QuestAssortHelper", $"Could not resolve assortId for trader '{traderId}'");
            return;
        }

        EnsureBuckets(traderMongoId, traders);
        AddToBucket(traderMongoId, assortId, config.QuestId, config.Status, traders);
        AddRewardDisplay(request, config.QuestId, traderId, assortId, quests);

        _debug.LogService("QuestAssortHelper", $"Added {assortId} -> {config.QuestId} ({config.Status})");
    }

    // -------------------------
    // RESOLUTION
    // -------------------------

    private static string ResolveTraderId(string traderIdOrAlias, Dictionary<MongoId, Trader> traders)
    {
        if (string.IsNullOrWhiteSpace(traderIdOrAlias))
        {
            return string.Empty;
        }

        MongoId traderMongoId = traderIdOrAlias;
        if (traders.ContainsKey(traderMongoId))
        {
            return traderIdOrAlias;
        }

        if (Maps.TraderMap.TryGetValue(traderIdOrAlias, out var mapped))
        {
            return mapped;
        }

        return string.Empty;
    }

    private static string ResolveAssortId(ItemModificationRequest request, string traderId, QuestAssortConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.AssortId))
        {
            return config.AssortId;
        }

        if (request.Config.Traders != null &&
            request.Config.Traders.TryGetValue(traderId, out var entries) &&
            entries.Count > 0)
        {
            return entries.Keys.First();
        }

        return string.Empty;
    }

    // -------------------------
    // BUCKETS
    // -------------------------

    private void EnsureBuckets(MongoId traderId, Dictionary<MongoId, Trader> traders)
    {
        var trader = traders.GetValueOrDefault(traderId);

        if (trader?.QuestAssort == null)
        {
            _debug.LogError("QuestAssortHelper", $"Trader '{traderId}' missing QuestAssort");
            return;
        }

        trader.QuestAssort.TryAdd("started", new Dictionary<MongoId, MongoId>());
        trader.QuestAssort.TryAdd("success", new Dictionary<MongoId, MongoId>());
        trader.QuestAssort.TryAdd("fail", new Dictionary<MongoId, MongoId>());
    }

    private static void AddToBucket(
        MongoId traderId,
        string assortId,
        string questId,
        string? status,
        Dictionary<MongoId, Trader> traders)
    {
        var trader = traders[traderId];

        var bucketKey = NormalizeStatus(status).ToLowerInvariant();

        var bucket = trader.QuestAssort[bucketKey];

        MongoId assortMongoId = assortId;
        MongoId questMongoId = questId;

        bucket[assortMongoId] = questMongoId;
    }

    // -------------------------
    // REWARD DISPLAY
    // -------------------------

    private void AddRewardDisplay(
        ItemModificationRequest request,
        string questId,
        string traderId,
        string assortId,
        Dictionary<MongoId, Quest> quests)
    {
        MongoId questMongoId = questId;

        if (!quests.TryGetValue(questMongoId, out var quest))
        {
            _debug.LogError("QuestAssortHelper", $"Quest '{questId}' not found");
            return;
        }

        quest.Rewards ??= [];
        quest.Rewards["Success"] ??= [];

        var loyaltyLevel = ResolveLoyaltyLevel(request, traderId, assortId);

        var exists = quest.Rewards["Success"].Any(x =>
            x.Type == RewardType.AssortmentUnlock &&
            x.TraderId == traderId &&
            x.Target == assortId);

        if (exists)
        {
            return;
        }

        quest.Rewards["Success"].Add(new()
        {
            Type = RewardType.AssortmentUnlock,
            TraderId = traderId,
            Target = assortId,
            Index = quest.Rewards["Success"].Count,
            LoyaltyLevel = loyaltyLevel,
            Items = BuildRewardItems(request)
        });

        _debug.LogService(
            "QuestAssortHelper",
            $"RewardDisplay added for {questId} (assort {assortId}, trader {traderId}, loyalty {loyaltyLevel})");
    }

    // -------------------------
    // UTILS
    // -------------------------

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return SuccessStatus;
        }

        return status.ToLowerInvariant() switch
        {
            "started" => StartedStatus,
            "fail" => FailStatus,
            _ => SuccessStatus
        };
    }

    private static int ResolveLoyaltyLevel(ItemModificationRequest request, string traderId, string assortId)
    {
        if (request.Config.Traders != null &&
            request.Config.Traders.TryGetValue(traderId, out var traderEntries) &&
            traderEntries.TryGetValue(assortId, out var assortConfig) &&
            assortConfig?.ConfigBarterSettings != null)
        {
            return assortConfig.ConfigBarterSettings.LoyalLevel;
        }

        return 0;
    }

    private static List<Item> BuildRewardItems(ItemModificationRequest request)
    {
        var itemId = !string.IsNullOrWhiteSpace(request.ItemId)
            ? request.ItemId
            : Guid.NewGuid().ToString("N")[..24];

        var itemTpl = !string.IsNullOrWhiteSpace(request.Config?.ItemTplToClone)
            ? request.Config.ItemTplToClone
            : request.ItemId;

        return
        [
            new Item
        {
            Id = itemId,
            Template = itemTpl
        }
        ];
    }
}