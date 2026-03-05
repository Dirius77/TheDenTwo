using Content.Server.Vocalization.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Vocalization.Systems;

/// <inheritdoc cref="DatasetVocalizerComponent"/>
public sealed partial class DatasetVocalizationSystem : EntitySystem // DEN: Made partial
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<DatasetVocalizerComponent, TryVocalizeEvent>(OnTryVocalize); // DEN: Obsolete for OnTryVocalizeLanguage

        InitializeLanguage();
    }

    [Obsolete("Obsolete, use OnTryVocalizeLanguage instead.", true)] // DEN: Languages
    private void OnTryVocalize(Entity<DatasetVocalizerComponent> ent, ref TryVocalizeEvent args)
    {
        if (args.Handled)
            return;

        var dataset = _protoMan.Index(ent.Comp.Dataset);

        args.Message = _random.Pick(dataset);
        args.Handled = true;
    }
}
