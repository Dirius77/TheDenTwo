using Content.Server.Chat.Systems;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Radio.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class HeadsetSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private void InitializeLanguage()
    {
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveLanguageEvent>(OnHeadsetReceiveLanguage);

        SubscribeLocalEvent<WearingHeadsetComponent, EntitySpokeLanguageEvent>(OnSpeakLanguage);
    }

    private void OnSpeakLanguage(EntityUid uid, WearingHeadsetComponent component, EntitySpokeLanguageEvent args)
    {
        if (args.Channel != null
            && TryComp(component.Headset, out EncryptionKeyHolderComponent? keys)
            && keys.Channels.Contains(args.Channel.ID))
        {
            _radio.SendLanguageRadioMessage(uid, args.Message, args.Language, args.Channel, component.Headset);
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

        if (TryComp(parent, out ActorComponent? actor))
        {
            _chat.SendComplexMessageToEntity(
                args.RadioSource,
                parent,
                args.Message,
                args.Language,
                RadioSystem.RadioWrapper,
                ChatChannel.Radio,
                args.Name,
                args.Verb,
                args.Speech.Bold,
                false,
                false,
                args.Channel.LocalizedName,
                args.Channel.Color);
        }
    }
}
