using System.Linq;
using Content.Server.Animals.Components;
using Content.Server.Radio;
using Content.Server.Vocalization.Systems;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared._DEN.Speech;
using Content.Shared.Animals.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Animals.Systems;

public sealed partial class ParrotMemorySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedLanguageSystem _language = default!;
    [Dependency] private EntityQuery<AudibleComponent> _audibleQuery = default!;

    private void InitializeLanguage()
    {
        SubscribeLocalEvent<ParrotListenerComponent, ListenLanguageEvent>(OnLanguageListen);
        SubscribeLocalEvent<ParrotListenerComponent, HeadsetRadioReceiveLanguageRelayEvent>(OnHeadsetReceiveLanguage);
        SubscribeLocalEvent<ParrotMemoryComponent, TryVocalizeLanguageEvent>(OnTryVocalizeLanguage);
    }

    private void OnLanguageListen(Entity<ParrotListenerComponent> entity, ref ListenLanguageEvent args)
    {
        if (args.Channel != ChatChannel.Local)
            return;
        TryLearnLanguage(entity.Owner, args.LanguageEnt, args.Message, args.Source);
    }

    private void OnHeadsetReceiveLanguage(Entity<ParrotListenerComponent> entity,
        ref HeadsetRadioReceiveLanguageRelayEvent args)
    {
        var message = args.RelayedEvent.Message;
        var languageEnt = args.RelayedEvent.LanguageEnt;
        var source = args.RelayedEvent.MessageSource;

        TryLearnLanguage(entity.Owner, languageEnt, message, source);
    }

    private void OnTryVocalizeLanguage(Entity<ParrotMemoryComponent> entity, ref TryVocalizeLanguageEvent args)
    {
        if (args.Handled)
            return;

        if (entity.Comp.SpeechMemories.Count == 0)
            return;

        var memory = _random.Pick(entity.Comp.SpeechMemories);

        args.Message = memory.Message;
        var language = GetEntity(memory.Language);
        if (TryComp<LanguageComponent>(language, out var langComp))
            args.Language = (language, langComp);

        args.Handled = true;
    }

    private void TryLearnLanguage(Entity<ParrotMemoryComponent?, ParrotListenerComponent?> entity,
        Entity<LanguageComponent> languageEnt,
        ComplexChatMessage incomingMessage,
        EntityUid source)
    {
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2))
            return;

        if (!_audibleQuery.HasComponent(languageEnt))
            return;

        if (!_whitelist.CheckBoth(source, entity.Comp2.Blacklist, entity.Comp2.Whitelist))
            return;

        if (source.Equals(entity) || _mobState.IsIncapacitated(entity))
            return;

        if (_gameTiming.CurTime < entity.Comp1.NextLearnInterval)
            return;

        var dialogParts = incomingMessage.Parts.Where(part => part.Item1 == ChatPart.Dialog)
            .Select(part => part.Item2)
            .ToList();

        if (dialogParts.Count == 0)
            return;

        var message = _random.Pick(dialogParts).Trim();

        if (string.IsNullOrWhiteSpace(message))
            return;

        if (message.Length < entity.Comp1.MinEntryLength || message.Length > entity.Comp1.MaxEntryLength)
            return;

        entity.Comp1.NextLearnInterval = _gameTiming.CurTime + entity.Comp1.LearnCooldown;

        if (!_random.Prob(entity.Comp1.LearnChance))
            return;

        var language = _proto.Index(languageEnt.Comp.Language);

        LearnLanguage((entity, entity.Comp1), message, language, source);
    }

    private void LearnLanguage(Entity<ParrotMemoryComponent> entity,
        string message,
        LanguagePrototype language,
        EntityUid source)
    {
        var languageName = Loc.GetString(language.Name);

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Parroting entity {ToPrettyString(entity):entity} learned the phrase \"{message}\" in {languageName} from {ToPrettyString(source):speaker}");

        NetUserId? sourceNetUserId = null;
        if (_mind.TryGetMind(source, out _, out var mind))
        {
            sourceNetUserId = mind.UserId;
        }

        EntityUid spokenLanguage;
        if (_language.SpeaksLanguage(entity, language.ID, out var spokenEnt))
        {
            spokenLanguage = spokenEnt.Value;
        }
        else
        {
            if (!_language.TryAddLanguage(entity, language.ID, out var addedLangs))
            {
                Log.Warning("Failed to teach " + Name(entity) + " language: " + language.Name);
                return;
            }
            // If TryAddLanguage returned true this will have at least one language.
            spokenLanguage = addedLangs.First();
        }


        var newMemory = new SpeechMemory(sourceNetUserId, message, GetNetEntity(spokenLanguage));

        if (entity.Comp.SpeechMemories.Count < entity.Comp.MaxSpeechMemory)
        {
            entity.Comp.SpeechMemories.Add(newMemory);
            return;
        }

        var replaceIdx = _random.Next(entity.Comp.SpeechMemories.Count);
        entity.Comp.SpeechMemories[replaceIdx] = newMemory;
    }
}
