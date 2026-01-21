using System.Linq;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.Prototypes;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._DEN.Language.Systems;

public sealed partial class LanguageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;

    public static readonly ProtoId<LanguageTagPrototype> VerbalTag = "Verbal";

    public override void Initialize()
    {
        SubscribeLocalEvent<ObfuscateLanguageEvent>(OnObfuscateLanguage);
        SubscribeLocalEvent<LanguageCommunicatorComponent, DetermineUnderstandingEvent>(OnDetermineUnderstanding);
    }

    public void OnObfuscateLanguage(ObfuscateLanguageEvent args)
    {
        if (args.Handled)
            return;

        args.ObfuscatedMessage = ObfuscateMessageWithLanguage(args.OriginalMessage, args.Language);
        args.Handled = true;
    }

    public void OnDetermineUnderstanding(EntityUid target, LanguageCommunicatorComponent communicatorComponent, DetermineUnderstandingEvent args)
    {
        if (args.Handled)
            return;

        if (communicatorComponent.UnderstoodLanguages.Contains(args.Language))
            args.Understands = true;
    }

    public LanguagePrototype? GetCurrentLanguage(EntityUid entity)
    {
        if (!TryComp<LanguageCommunicatorComponent>(entity, out var languageCommunicator))
            return null;

        // No language selected, switch to one that they speak, assuming they still speak one.
        if (languageCommunicator.CurrentLanguage is null && languageCommunicator.SpokenLanguages.Count != 0)
            languageCommunicator.CurrentLanguage = languageCommunicator.SpokenLanguages.First();

        return !_prototypes.Resolve(languageCommunicator.CurrentLanguage, out var languageProto) ? null : languageProto;
    }

    public string ObfuscateMessageWithLanguage(string message, LanguagePrototype language)
    {
        return language.ObfuscationEffect.Apply(message, _gameTicker.RoundId);
    }

    public string WrapLanguageBasedMessage(LocId wrapper, string message, )
}
