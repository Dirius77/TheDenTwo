using Robust.Shared.Configuration;

namespace Content.Shared._DEN.CCVars;

[CVarDefs]
public sealed class DenCCVars
{
    /// <summary>
    ///     Allows the Language system to be 'disabled'. This does not actually prevent language related events from
    ///     occurring, because of how much of the chat infrastructure is replaced with language based systems. Instead
    ///     this setting hides the language UI on clients, prevents language from being changed, and forces every entity
    ///     to use a 'Default' language that behaves the same way as language-less chat.
    /// </summary>
    public static readonly CVarDef<bool> LanguageEnabled =
        CVarDef.Create("languages.language_enabled", true, CVar.ARCHIVE | CVar.SERVER | CVar.NOTIFY | CVar.REPLICATED);

    /// <summary>
    ///     Whether or not to allow detailed speech, that is, prefixing a message with an ! in order to allow special
    ///     formatting related to mixed emotes and dialogs in a message, or emoting over the radio.
    /// </summary>
    public static readonly CVarDef<bool> DetailedSpeechEnabled =
        CVarDef.Create("languages.detailed_speech_enabled", true, CVar.ARCHIVE | CVar.SERVER);

    /// <summary>
    ///     The maximum number of message translations to cache at a time.
    ///     The total size will cap out at this times the number of languages times the number of
    ///     different 'understanding' variants in use.
    /// </summary>
    public static readonly CVarDef<int> LanguageMessageCacheSize =
        CVarDef.Create("languages.message_cache_size", 20, CVar.ARCHIVE | CVar.SERVER);

    /// <summary>
    ///     The number of words to keep in the word cache at a time.
    /// </summary>
    public static readonly CVarDef<int> LanguageWordCacheSize =
        CVarDef.Create("languages.word_cache_size", 50, CVar.ARCHIVE | CVar.SERVER);

    /// <summary>
    ///     Whether or not to give an entity that tries speaking without LanguageCommunicatorComponent a language.
    /// </summary>
    public static readonly CVarDef<bool> FallbackDefaultLanguage =
        CVarDef.Create("languages.fallback_default_language", false, CVar.ARCHIVE | CVar.SERVER);

    /// <summary>
    ///     The default spoken language. If fallback_default_language is set, entities without LanguageCommunicatorComponent
    ///     will use this. Systems that directly send messages will also use this language.
    /// </summary>
    public static readonly CVarDef<string> DefaultLanguage =
        CVarDef.Create("languages.default_language", "Basic", CVar.ARCHIVE | CVar.SERVER);

    /// <summary>
    ///     Client's preference for how to display language fonts.
    /// </summary>
    public static readonly CVarDef<HideLanguageFontSetting> HideLanguageFonts =
        CVarDef.Create("languages.hide_fonts", HideLanguageFontSetting.None, CVar.CLIENTONLY | CVar.ARCHIVE);
}

public enum HideLanguageFontSetting
{
    None,
    Understood,
    All,
}
