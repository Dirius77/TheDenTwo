using Content.Server.Chat.Systems;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Content.Shared.Radio.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class HeadsetSystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private EntityQuery<RadioTransmittableComponent> _radioLang = default!;

    private void InitializeLanguage()
    {
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveLanguageEvent>(OnHeadsetReceiveLanguage);
        SubscribeLocalEvent<WearingHeadsetComponent, EntitySpokeLanguageEvent>(OnSpeakLanguage);
    }

    private void OnSpeakLanguage(EntityUid uid, WearingHeadsetComponent component, EntitySpokeLanguageEvent args)
    {
        if (args.RadioChannel != null
            && TryComp(component.Headset, out EncryptionKeyHolderComponent? keys)
            && keys.Channels.Contains(args.RadioChannel.ID)
            && _radioLang.HasComponent(args.LanguageEnt))
        {
            _radio.SendLanguageRadioMessage(uid, args.LanguageEnt, args.Message, args.RadioChannel, component.Headset);
        }
    }

    private void OnHeadsetReceiveLanguage(EntityUid uid, HeadsetComponent component, ref RadioReceiveLanguageEvent args)
    {
        var parent = Transform(uid).ParentUid;

        if (parent.IsValid())
        {
            var relayEvent = new HeadsetRadioReceiveLanguageRelayEvent(args);
            RaiseLocalEvent(parent, ref relayEvent);
        }

        if (TryComp(parent, out ActorComponent? _))
        {
            _chat.SendComplexMessageToEntity(
                args.RadioSource,
                parent,
                args.LanguageEnt,
                args.Message,
                _prototype.Index(RadioSystem.RadioWrapper),
                ChatChannel.Radio,
                args.Name,
                args.Verb,
                args.Speech.Bold,
                false,
                args.Channel.LocalizedName,
                args.Channel.Color);
        }
    }
}
