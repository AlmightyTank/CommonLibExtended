using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonLibExtended.Services.ItemHelpers;

[Injectable]
public class BuffHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService
)
{

    private readonly DatabaseService _databaseService = databaseService;

    public void AddBuffs(Dictionary<string, Buff[]> buffs)
    {
        foreach (var (key, value) in buffs)
        {
            if (value == null || value.Length == 0)
            {
                continue;
            }

            if (_databaseService.GetGlobals().Configuration.Health.Effects.Stimulator.Buffs.TryGetValue(key, out var existing))
            {
                _databaseService.GetGlobals().Configuration.Health.Effects.Stimulator.Buffs[key] = existing
                    .Concat(value)
                    .Where(x => x != null)
                    .GroupBy(GetBuffMergeKey)
                    .Select(g => g.First())
                    .ToArray();
            }
            else
            {
                _databaseService.GetGlobals().Configuration.Health.Effects.Stimulator.Buffs[key] = value;
            }
        }
        
    }

    private static string GetBuffMergeKey(Buff buff)
    {
        var appliesTo = buff.AppliesTo == null
            ? string.Empty
            : string.Join(",", buff.AppliesTo);

        return string.Join("|",
            buff.BuffType ?? string.Empty,
            buff.Chance.ToString(System.Globalization.CultureInfo.InvariantCulture),
            buff.Delay.ToString(System.Globalization.CultureInfo.InvariantCulture),
            buff.Duration.ToString(System.Globalization.CultureInfo.InvariantCulture),
            buff.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            buff.AbsoluteValue.ToString(),
            buff.SkillName ?? string.Empty,
            appliesTo);
    }
}