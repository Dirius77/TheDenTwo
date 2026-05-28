using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Content.Shared._DEN.CCVar;
using Content.Shared._DEN.Language;
using Content.Shared._DEN.Language.Components;
using Content.Shared._DEN.Language.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._DEN.Language.EntitySystems;

public sealed class SyllableScramblingSystem : SharedSyllableScramblingSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

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

    private static readonly Regex Lowercase = new("[a-z]|I$|[0-9]", RegexOptions.Compiled);
    private static readonly Regex Sentence = new(@"(.+?(?:[\.!\?]|$))", RegexOptions.Compiled);
    private static readonly Regex Punctuation = new(@"[\,\.\!\?]", RegexOptions.Compiled);

    public override void Initialize()
    {
        SubscribeLocalEvent<SyllableScramblingComponent, LanguageModifyMessageEvent>(OnLanguageModifyMessage);

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

    private void OnLanguageModifyMessage(Entity<SyllableScramblingComponent> entity, ref LanguageModifyMessageEvent args)
    {
        var newMessageParts = new List<(ChatPart, string)>();
        foreach (var (kind, part) in args.Message.Parts)
        {
            if (kind == ChatPart.Dialog)
            {
                var modifiedMsg = ScrambleMessage(part, args.Language, entity.Comp, args.Understanding.Understanding);
                newMessageParts.Add((kind, modifiedMsg));
            }
            else
            {
                newMessageParts.Add((kind, part));
            }
        }
        args.Message = new ComplexChatMessage(args.Message, newMessageParts);
    }

    private string ScrambleMessage(string message, ProtoId<LanguagePrototype> language, SyllableScramblingComponent comp, int understanding = 0)
    {
        if (understanding >= 100)
            return message;

        // Check if we have this cached. This is useful so we don't have to re-scramble the message for multiple listeners.
        if (TryGetMessageCachedValue(language.Id + "-" + understanding, message, out var value))
        {
            var allCaps = !Lowercase.IsMatch(message);
            return allCaps ? value.ToUpper() : value;
        }

        var builder = new StringBuilder();
        var wordBuilder = new StringBuilder();
        var random = IoCManager.Resolve<IRobustRandom>();

        foreach (Match sentence in Sentence.Matches(message))
        {
            var firstWord = true;
            foreach (var word in sentence.Value.Split(' '))
            {
                var allCaps = !Lowercase.IsMatch(word);
                var trimmedWord = Punctuation.Replace(word, string.Empty);
                var commonality = CommonWordFrequency.GetValueOrDefault(trimmedWord, 1500);

                var prob = 10 * (1 - (commonality / 500));
                if (understanding > 0 && random.Next(100) <= understanding + prob)
                {
                    builder.Append(trimmedWord);
                    builder.Append(' ');
                    firstWord = false;
                    continue;
                }

                if (TryGetWordCachedValue(language, trimmedWord, out var cachedWord))
                {
                    if (firstWord)
                    {
                        cachedWord = string.Concat(cachedWord[0].ToString().ToUpper(), cachedWord.AsSpan(1));
                        firstWord = false;
                    }
                    builder.Append(allCaps ? cachedWord.ToUpper() : cachedWord);
                    builder.Append(' ');
                    continue;
                }

                wordBuilder.Clear();
                var count = random.Next(comp.MinSyllables, comp.MaxSyllables + 1);
                for (var i = 0; i < count; i++)
                {
                    var syllable = random.Pick(comp.Syllables);
                    if (firstWord)
                    {
                        syllable = string.Concat(syllable[0].ToString().ToUpper(), syllable.AsSpan(1));
                        firstWord = false;
                    }

                    wordBuilder.Append(syllable);
                }
                var scrambledWord = wordBuilder.ToString();
                AddWordToCache(language, trimmedWord, scrambledWord.ToLower());
                builder.Append(allCaps ? scrambledWord.ToUpper() : scrambledWord);
                builder.Append(' ');
            }

            if (Punctuation.IsMatch(sentence.Value[^1].ToString()))
            {
                builder.Remove(builder.Length - 1, 1);
                builder.Append(sentence.Value[^1]);
            }

            builder.Append(' ');
        }

        var result = builder.ToString().Trim();
        AddMessageToCache(language.Id + "-" + understanding, message, result);
        return result;
    }

    private bool TryGetMessageCachedValue(string key, string msg, [MaybeNullWhen(false)] out string value)
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

    private void AddMessageToCache(string key, string msg, string value)
    {
        _messageCache.TryAdd(key, new OrderedDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        var messageCache = _messageCache[key];
        messageCache.Remove(msg);
        messageCache.Add(msg, value);
        if (messageCache.Count > _messageCacheMaxSize)
            messageCache.RemoveAt(0);
    }

    private bool TryGetWordCachedValue(ProtoId<LanguagePrototype> language, string word, [MaybeNullWhen(false)] out string value)
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

    private void AddWordToCache(ProtoId<LanguagePrototype> language, string word, string value)
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
}
