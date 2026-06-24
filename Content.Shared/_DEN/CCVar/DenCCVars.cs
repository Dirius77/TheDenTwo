using Robust.Shared.Configuration;

namespace Content.Shared._DEN.CCVar;

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
  
    /// <summary>
    /// Stops the server from sending the station broadcast about people cryoing to this client.
    /// </summary>
    public static readonly CVarDef<bool> IgnoreCryoMessage =
        CVarDef.Create("den.ignore_cryo_message", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Discord role IDs that are considered "game admins".
    /// </summary>
    public static readonly CVarDef<string> DiscordAdminRoleIds =
        CVarDef.Create("den.discord_admin_role_ids",
            "1302235169591394305,1302235145889124383,1302235089677320245,1302235039651598386,1302235013986910219,1392313569390886942",
            CVar.SERVERONLY | CVar.ARCHIVE);
}

public enum HideLanguageFontSetting
{
    None,
    Understood,
    All,
}
