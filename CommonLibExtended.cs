using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;

namespace CommonLibExtended;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.amightytank.commonlibextended";
    public override string Name { get; init; } = "CommonLibExtended";
    public override string Author { get; init; } = "AmightyTank";
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("~4.0.1");
    public override string License { get; init; } = "MIT";
    public override bool? IsBundleMod { get; init; } = false;

    public override Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new Range("~2.0.0") }
    };

    public override string? Url { get; init; }
    public override List<string>? Contributors { get; init; }
    public override List<string>? Incompatibilities { get; init; }
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 3)]
public sealed class CommonLibExtended : IOnLoad
{
    public async Task OnLoad()
    {
        await Task.CompletedTask;
    }
}