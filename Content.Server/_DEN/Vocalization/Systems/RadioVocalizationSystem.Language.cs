using Content.Server.Chat.Systems;
using Content.Server.Vocalization.Components;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Chat;
using Robust.Shared.Random;

namespace Content.Server.Vocalization.Systems;

public sealed partial class RadioVocalizationSystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<RadioVocalizerComponent, VocalizeLanguageEvent>(OnVocalizeLanguage);
    }

    private void OnVocalizeLanguage(Entity<RadioVocalizerComponent> entity, ref VocalizeLanguageEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TrySpeakLanguageRadio(entity.Owner, args.Language, args.Message);
    }

    private bool TrySpeakLanguageRadio(Entity<RadioVocalizerComponent?> entity,
        Entity<LanguageComponent> language,
        string message)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!_random.Prob(entity.Comp.RadioAttemptChance))
            return false;

        if (!TryPickRandomRadioChannel(entity, out var channel))
            return false;

        var radioChannel = _proto.Index(channel);
        var cmplxMessage = new ComplexChatMessage(message, "\"", false, false, false);

        _chat.SendEntityComplexSpeech(
            entity,
            cmplxMessage,
            ChatSystem.WhisperWrapper,
            ChatTransmitRange.Normal,
            ChatChannel.Whisper,
            radioChannel
            );

        return true;
    }
}
