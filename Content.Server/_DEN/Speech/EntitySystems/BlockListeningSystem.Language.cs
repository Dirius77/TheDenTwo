using Content.Server.Speech.Components;
using Content.Shared._DEN.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class BlockListeningSystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<BlockListeningComponent, ListenLanguageAttemptEvent>(OnListenLanguageAttempt);
    }

    private void OnListenLanguageAttempt(EntityUid uid,
        BlockListeningComponent component,
        ListenLanguageAttemptEvent args)
    {
        args.Cancel();
    }
}
