using Robust.Shared.Configuration;

namespace Content.Shared._DEN.CCVars;

[CVarDefs]
public sealed class DenCCVars
{
    /// <summary>
    ///     The maximum number of message translations to cache at a time.
    ///     The total size will cap out at this times the number of languages times the number of
    ///     different 'understanding' variants in use.
    /// </summary>
    public static readonly CVarDef<int> LanguageMessageCacheSize =
        CVarDef.Create("languages.message_cache_size", 20, CVar.ARCHIVE);

    /// <summary>
    ///     The number of words to keep in the word cache at a time.
    /// </summary>
    public static readonly CVarDef<int> LanguageWordCacheSize =
        CVarDef.Create("languages.word_cache_size", 50, CVar.ARCHIVE);

    /// <summary>
    ///     Whether or not to give an entity that tries speaking without LanguageCommunicatorComponent a language.
    /// </summary>
    public static readonly CVarDef<bool> FallbackDefaultLanguage =
        CVarDef.Create("languages.fallback_default_language", false, CVar.ARCHIVE);

    /// <summary>
    ///     The default spoken language. If fallback_default_language is set, entities without LanguageCommunicatorComponent
    ///     will use this. Systems that directly send messages will also use this language.
    /// </summary>
    public static readonly CVarDef<string> DefaultLanguage =
        CVarDef.Create("languages.default_language", "Basic", CVar.ARCHIVE);
}
