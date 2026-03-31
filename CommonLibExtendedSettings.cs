using SPTarkov.DI.Annotations;

namespace CommonLibExtended;

[Injectable]
public class CommonLibExtendedSettings
{
    public bool ForceAllItemsToDefaultTrader { get; set; } = false;

    public string DefaultTraderId { get; set; } = "54cb57776803fa99248b456e";
}