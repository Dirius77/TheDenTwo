using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class BlockListeningSystem : EntitySystem // DEN: Make Partial
{
    public override void Initialize()
    {
        base.Initialize();

        // SubscribeLocalEvent<BlockListeningComponent, ListenAttemptEvent>(OnListenAttempt); // DEN: Language

        InitializeLanguage(); // DEN: Language
    }

    [Obsolete("See OnListenLanguageAttempt", true)] // DEN: Languages
    private void OnListenAttempt(EntityUid uid, BlockListeningComponent component, ListenAttemptEvent args)
    {
        args.Cancel();
    }
}
