using System.Diagnostics.CodeAnalysis;
using Content.Shared._DEN.CCVars;
using Content.Server.Chat.Systems;
using Content.Shared._DEN.Language;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server._DEN.Language.EntitySystems;

public sealed partial class LanguageSystem : Shared._DEN.Language.EntitySystems.SharedLanguageSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    // 1000 most common words and their order. This is a dictionary to make looking up specific words faster.
    public Dictionary<string, int> CommonWordFrequency = new();

    // Cache for individual words
    private readonly Dictionary<ProtoId<LanguagePrototype>, OrderedDictionary<string, string>> _wordCache = new();
    // Cache for the 1000 most common words, gets added to but never excluded from. Still gets built as needed.
    private readonly Dictionary<ProtoId<LanguagePrototype>, Dictionary<string, string>> _commonWordCache = new();
    // Cache for messages, cares about the understanding of the language.
    private readonly Dictionary<string, OrderedDictionary<string, string>> _messageCache = new();

    private static readonly ProtoId<LocalizedDatasetPrototype> CommonWords = "CommonWords";

    private int _messageCacheMaxSize = 0;
    private int _wordCacheMaxSize = 0;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(DenCCVars.LanguageMessageCacheSize, cacheSize => _messageCacheMaxSize = cacheSize, true);
        _cfg.OnValueChanged(DenCCVars.LanguageWordCacheSize, cacheSize => _wordCacheMaxSize = cacheSize, true);

        BuildCommonWordSet();
    }

    private void BuildCommonWordSet()
    {
        var commonWords = _proto.Index(CommonWords);
        CommonWordFrequency = new Dictionary<string, int>(commonWords.Values.Count, StringComparer.OrdinalIgnoreCase);
        var i = 0;
        foreach (var word in commonWords.Values)
        {
            CommonWordFrequency.Add(Loc.GetString(word), i++);
        }
    }

    public string ObfuscateMessageWithLanguage(string message,
        LanguagePrototype language,
        LanguageFluencyPrototype understanding)
    {
        return language.Scrambler.ScrambleMessage(message, this, language, understanding.Understanding);
    }

    public override bool TryGetMessageCachedValue(string key, string msg, [MaybeNullWhen(false)] out string value)
    {
        _messageCache.TryAdd(key, new OrderedDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        var messageCache = _messageCache[key];
        if (messageCache.Remove(msg, out value))
        {
            // Put the entry back at the end of the ordered cache.
            messageCache.Add(msg, value);
            return true;
        }
        return false;
    }

    public override void AddMessageToCache(string key, string msg, string value)
    {
        _messageCache.TryAdd(key, new OrderedDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        var messageCache = _messageCache[key];
        messageCache.Remove(msg);
        messageCache.Add(msg, value);
        if (messageCache.Count > _messageCacheMaxSize)
            messageCache.RemoveAt(0);
    }

    public override bool TryGetWordCachedValue(ProtoId<LanguagePrototype> language, string word, [MaybeNullWhen(false)] out string value)
    {
        if (CommonWordFrequency.ContainsKey(word))
        {
            _commonWordCache.TryAdd(language, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            var commonCache = _commonWordCache[language];
            return commonCache.TryGetValue(word, out value);
        }

        _wordCache.TryAdd(language, new OrderedDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        var wordCache = _wordCache[language];
        if (wordCache.Remove(word, out value))
        {
            wordCache.Add(word, value);
            return true;
        }

        return false;
    }

    public override void AddWordToCache(ProtoId<LanguagePrototype> language, string word, string value)
    {
        if (CommonWordFrequency.ContainsKey(word))
        {
            _commonWordCache.TryAdd(language, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            var commonCache = _commonWordCache[language];
            commonCache.TryAdd(word, value);
            return;
        }

        _wordCache.TryAdd(language, new OrderedDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        var wordCache = _wordCache[language];
        wordCache.Remove(word);
        wordCache.Add(word, value);
        if (wordCache.Count > _wordCacheMaxSize)
            wordCache.RemoveAt(0);
    }

    public override Dictionary<string, int> GetCommonWords()
    {
        return CommonWordFrequency;
    }
}
