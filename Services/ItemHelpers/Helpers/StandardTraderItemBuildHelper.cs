using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace CommonLibExtended.Services.ItemHelpers.Helpers;

[Injectable]
public sealed class StandardTraderItemBuildHelper(DebugLogHelper debugLogHelper)
{
    private readonly DebugLogHelper _debugLogHelper = debugLogHelper;

    public Item? BuildSingleItemForTrader(
        ItemModificationRequest request,
        string assortId,
        TraderOfferSettings? settings = null)
    {
        if (request == null)
        {
            _debugLogHelper.LogError("StandardTraderItemBuildHelper", "request is null");
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.ItemId))
        {
            _debugLogHelper.LogError("StandardTraderItemBuildHelper", "request.ItemId is null or empty");
            return null;
        }

        if (string.IsNullOrWhiteSpace(assortId))
        {
            _debugLogHelper.LogError(
                "StandardTraderItemBuildHelper",
                $"assortId is null or empty for item {request.ItemId}");
            return null;
        }

        var rootItem = CreateRootItem(request.ItemId, assortId, settings);

        _debugLogHelper.LogService(
            "StandardTraderItemBuildHelper",
            $"Built standard single-item trader root for item={request.ItemId}, assortId={assortId}");

        return rootItem;
    }

    private static Item CreateRootItem(
        string templateId,
        string assortId,
        TraderOfferSettings? settings)
    {
        return new Item
        {
            Id = new MongoId(assortId),
            Template = new MongoId(templateId),
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                StackObjectsCount = settings?.StackObjectsCount > 0
                    ? settings.StackObjectsCount
                    : null,

                UnlimitedCount = settings?.UnlimitedCount,

                BuyRestrictionMax = settings?.UnlimitedCount == true
                    ? 0
                    : Math.Max(0, settings?.BuyRestrictionMax ?? 0)
            }
        };
    }
}