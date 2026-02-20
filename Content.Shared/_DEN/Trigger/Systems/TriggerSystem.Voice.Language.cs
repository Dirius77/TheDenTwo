using System.Linq;
using Content.Shared._DEN.Speech;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private void InitializeLanguage()
    {
        SubscribeLocalEvent<TriggerOnVoiceComponent, ListenLanguageEvent>(OnListenLanguage);
    }

    private void OnListenLanguage(Entity<TriggerOnVoiceComponent> ent, ref ListenLanguageEvent args)
    {
        var languageEnt = args.LanguageEnt;
        if (!Resolve(languageEnt, ref languageEnt.Comp))
            return;

        var language = _proto.Index(languageEnt.Comp.Language);
        var component = ent.Comp;
        var message = string.Join(' ', args.Message.Parts.Where(part => part.Item1 == ChatPart.Dialog).Select(part => part.Item2)).Trim();

        if (component.IsRecording)
        {
            var ev = new ListenLanguageAttemptEvent(args.Source, languageEnt);
            RaiseLocalEvent(ent, ev);

            if (ev.Cancelled)
                return;

            if (message.Length >= component.MinLength && message.Length <= component.MaxLength)
                FinishRecording(ent, args.Source, message, language.ID);
            else if (message.Length > component.MaxLength)
                _popup.PopupEntity(Loc.GetString("trigger-on-voice-record-failed-too-long"), ent);
            else if (message.Length < component.MinLength)
                _popup.PopupEntity(Loc.GetString("trigger-on-voice-record-failed-too-short"), ent);

            return;
        }

        if (!string.IsNullOrWhiteSpace(component.KeyPhrase) &&
            message.IndexOf(component.KeyPhrase, StringComparison.InvariantCultureIgnoreCase) is var index and >= 0 &&
            component.KeyLanguage is not null &&
            component.KeyLanguage.Value == language.ID)
        {
            _adminLogger.Add(LogType.Trigger, LogImpact.Medium,
                $"A voice-trigger on {ToPrettyString(ent):entity} was triggered by {ToPrettyString(args.Source):speaker} speaking the key-phrase {component.KeyPhrase}.");
            Trigger(ent, args.Source, ent.Comp.KeyOut);

            var messageWithoutPhrase = message.Remove(index, component.KeyPhrase.Length).Trim();
            var voice = new VoiceTriggeredEvent(args.Source, message, messageWithoutPhrase);
            RaiseLocalEvent(ent, ref voice);
        }
    }
}
