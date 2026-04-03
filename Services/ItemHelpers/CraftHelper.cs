using CommonLibExtended.Helpers;
using CommonLibExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace CommonLibExtended.Services.ItemHelpers;

[Injectable]
public class CraftHelper(
    DebugLogHelper debugLogHelper,
    DatabaseService databaseService
)
{
    private readonly DatabaseService _databaseService = databaseService;

    public void AddCrafts(HideoutProduction[] crafts)
    {
        foreach (var craft in crafts)
        {
            if (craft == null)
            {
                continue;
            }

            if (_databaseService.GetHideout().Production.Recipes.Any(x => string.Equals(x.Id, craft.Id, StringComparison.OrdinalIgnoreCase)))
            {
                debugLogHelper.LogService("CraftHelper", $"Craft {craft.Id} already exists, skipping.");
                continue;
            }

            _databaseService.GetHideout().Production.Recipes.Add(craft);
        }
    }
}