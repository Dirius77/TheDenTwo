using Content.Server.Vocalization.Components;
using Content.Shared.Random.Helpers;

namespace Content.Server.Vocalization.Systems;

public sealed partial class DatasetVocalizationSystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<DatasetVocalizerComponent, TryVocalizeLanguageEvent>(OnTryVocalizeLanguage);
    }

    private void OnTryVocalizeLanguage(Entity<DatasetVocalizerComponent> ent, ref TryVocalizeLanguageEvent args)
    {
        if (args.Handled)
            return;

        var dataset = _protoMan.Index(ent.Comp.Dataset);

        args.Message = _random.Pick(dataset);
        args.Handled = true;
    }
}
