using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;

namespace CommonLibExtended.Services;

[Injectable]
public sealed class ItemModificationService(
    DebugLogHelper debugLogHelper,
    QuestAssortHelper questAssortHelper,
    BuffHelper buffHelper,
    CraftHelper craftHelper,
    CompatibilityService compatibilityService,
    SlotCloneHelper slotCloneHelper,
    PresetTraderOfferHelper presetTraderOfferHelper,
    QuestRewardHelper questRewardHelper,
    EquipmentSlotHelper equipmentSlotHelper,
    CompatibilityCloneHelper compatibilityCloneHelper)
{
    public void Process(ItemModificationRequest request)
    {
        if (request == null)
        {
            debugLogHelper.Log(nameof(ItemModificationService), "Received null request");
            return;
        }

        if (request.Config == null)
        {
            debugLogHelper.LogError(nameof(ItemModificationService), $"Request config is null for {request.ItemId}");
            return;
        }

        if (request.Extras == null)
        {
            debugLogHelper.LogError(nameof(ItemModificationService), $"Request extras are null for {request.ItemId}");
            return;
        }

        debugLogHelper.Log(nameof(ItemModificationService), $"Processing {request.ItemId} from {request.FilePath}");

        if ((request.Extras?.AmmoCloneCompatibility == true)
        || (request.Extras?.WeaponCloneChamberCompatibility == true)
        || (request.Extras?.MagCloneCartridgeCompatibility == true))
        {
            compatibilityCloneHelper.Process(request);
            debugLogHelper.LogService("CompatibilityCloneHelper", $"Added clone compatibility for {request.ItemId}");
        }

        if (request.Extras?.CopySlot == true)
        {
            slotCloneHelper.Process(request);
            debugLogHelper.LogService("SlotCloneHelper", $"Copied slots for {request.ItemId}");
        }

        if (request.Extras?.PresetTraders is { Count: > 0 })
        {
            presetTraderOfferHelper.Process(request);
            debugLogHelper.LogService("CompatibilityService", $"Added preset traders for {request.ItemId}");
        }

        if (request.Extras?.AddBuffs == true && request.Extras?.Buffs is { Count: > 0 })
        {
            buffHelper.AddBuffs(request.Extras.Buffs);
            debugLogHelper.LogService("BuffHelper", $"Added buffs for {request.ItemId}");
        }

        if (request.Extras?.AddCrafts == true && request.Extras?.Crafts is { Length: > 0 })
        {
            craftHelper.AddCrafts(request.Extras.Crafts);
            debugLogHelper.LogService("CraftHelper", $"Added craft for {request.ItemId}");
        }

        if (request.Extras?.AmmoCloneCompatibility == true && !string.IsNullOrWhiteSpace(request.Config.ItemTplToClone))
        {
            compatibilityService.AddAmmoClone(request.ItemId, request.Config.ItemTplToClone);
            debugLogHelper.LogService("CompatibilityService", $"Added ammo clone compatibility for {request.ItemId} based on {request.Config.ItemTplToClone}");
        }

        if (request.Extras?.AddToPrimaryWeaponSlot == true || request.Extras?.AddToHolsterWeaponSlot == true)
        {
            equipmentSlotHelper.Process(request);
            debugLogHelper.LogService("CompatibilityService", $"Added {request.ItemId} to primary weapon slot");
        }

        if (request.Extras?.AddToQuestAssorts == true && request.Extras.QuestAssorts != null)
        {
            questAssortHelper.Process(request);
            debugLogHelper.LogService("QuestAssortHelper", $"Added quest assorts for {request.ItemId}");
        }

        if (request.Extras?.AddToQuestRewards == true && request.Extras?.QuestRewards is { Count: > 0 })
        {
            questRewardHelper.Process(request);
            debugLogHelper.LogService("QuestReward", $"Added quest rewards for {request.ItemId}");
        }
    }
}