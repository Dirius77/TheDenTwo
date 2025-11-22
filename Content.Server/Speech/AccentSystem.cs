using System.Text.RegularExpressions;
using Content.Shared._DEN.Language.Prototypes;
using Content.Shared.Chat;
using Content.Shared.Speech;

namespace Content.Server.Speech;

public sealed class AccentSystem : EntitySystem
{
    public static readonly Regex SentenceRegex = new(@"(?<=[\.!\?‽])(?![\.!\?‽])", RegexOptions.Compiled);

    public override void Initialize()
    {
        SubscribeLocalEvent<TransformSpeechEvent>(AccentHandler);
    }

    private void AccentHandler(TransformSpeechEvent args)
    {
        // DEN: TransformSpeechEvent is a handled event now.
        if (args.Handled)
            return;

        var accentEvent = new AccentGetEvent(args.Sender, args.Message);

        RaiseLocalEvent(args.Sender, accentEvent, true);
        args.Message = accentEvent.Message;
        args.Handled = true; // DEN
    }
}
