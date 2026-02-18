using System.Linq;
using Content.Server.Animals.Components;
using Content.Server.Radio;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Speech;
using Content.Shared.Animals.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Server.Animals.Systems;

public sealed partial class ParrotMemorySystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<ParrotListenerComponent, ListenLanguageEvent>(OnLanguageListen);
        SubscribeLocalEvent<ParrotListenerComponent, HeadsetRadioReceiveLanguageRelayEvent>(OnHeadsetReceiveLanguage);
    }

    private void OnLanguageListen(Entity<ParrotListenerComponent> entity, ref ListenLanguageEvent args)
    {
        if (args.Whisper)
            return;
        TryLearnLanguage(entity.Owner, args.Message, args.Language, args.Source);
    }

    private void OnHeadsetReceiveLanguage(Entity<ParrotListenerComponent> entity,
        ref HeadsetRadioReceiveLanguageRelayEvent args)
    {
        var message = args.RelayedEvent.Message;
        var language = args.RelayedEvent.Language;
        var source = args.RelayedEvent.MessageSource;

        TryLearnLanguage(entity.Owner, message, language, source);
    }

    private void TryLearnLanguage(Entity<ParrotMemoryComponent?, ParrotListenerComponent?> entity,
        ComplexChatMessage incomingMessage,
        LanguagePrototype language,
        EntityUid source)
    {
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2))
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

        var newMemory = new SpeechMemory(sourceNetUserId, message, language);

        if (entity.Comp.SpeechMemories.Count < entity.Comp.MaxSpeechMemory)
        {
            entity.Comp.SpeechMemories.Add(newMemory);
            return;
        }

        var replaceIdx = _random.Next(entity.Comp.SpeechMemories.Count);
        entity.Comp.SpeechMemories[replaceIdx] = newMemory;
    }
}
