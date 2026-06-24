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

public sealed partial class SyllableScramblingSystem : SharedSyllableScramblingSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IRobustRandom _random = default!;

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

    private static readonly Regex Lowercase = new("[a-z]|^I$|[0-9]", RegexOptions.Compiled);
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
            // Check if the original message was in all caps, excluding the word "I". If it was, the scrambled text is
            // also converted to be in all caps.
            var allCaps = !Lowercase.IsMatch(message);
            return allCaps ? value.ToUpper() : value;
        }

        var builder = new StringBuilder();
        var wordBuilder = new StringBuilder();

        foreach (Match sentence in Sentence.Matches(message))
        {
            var firstWord = true;
            // Break the sentence into words because they each get unique 'translations'.
            foreach (var word in sentence.Value.Split(' '))
            {
                // Check if the word contains any lowercase, or is "I".
                var allCaps = !Lowercase.IsMatch(word);
                var trimmedWord = Punctuation.Replace(word, string.Empty);
                var commonality = CommonWordFrequency.GetValueOrDefault(trimmedWord, 1500);

                // The probability that a word is understood and left untranslated scales with how common it is.
                var prob = 10 * (1 - (commonality / 500));
                if (understanding > 0 && _random.Next(100) <= understanding + prob)
                {
                    builder.Append(trimmedWord);
                    builder.Append(' ');
                    firstWord = false;
                    continue;
                }

                // Check if we've already translated the word and have it cached, use it if we do.
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
                // Build the syllables for the scrambled word based on the language's rules.
                var count = _random.Next(comp.MinSyllables, comp.MaxSyllables + 1);
                for (var i = 0; i < count; i++)
                {
                    var syllable = _random.Pick(comp.Syllables);
                    if (firstWord)
                    {
                        syllable = string.Concat(syllable[0].ToString().ToUpper(), syllable.AsSpan(1));
                        firstWord = false;
                    }

                    wordBuilder.Append(syllable);
                }
                var scrambledWord = wordBuilder.ToString();
                // Cache the word for later, possibly evicting something.
                AddWordToCache(language, trimmedWord, scrambledWord.ToLower());
                builder.Append(allCaps ? scrambledWord.ToUpper() : scrambledWord);
                builder.Append(' ');
            }

            // Punctuation is preserved if it's at the end of sentences.
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

    // Checks if we have a cached scrambling of the passed message.
    // Messaged are cached by understanding as well, which is why the key is a string instead of just a language ID.
    // So the keys look like "Basic-50" for 50% understanding.
    private bool TryGetMessageCachedValue(string key, string msg, [MaybeNullWhen(false)] out string value)
    {
        _messageCache.TryAdd(key, new OrderedDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        var messageCache = _messageCache[key];
        if (messageCache.Remove(msg, out value))
        {
            // Put the entry back at the end of the ordered cache to indicate it has been used again.
            messageCache.Add(msg, value);
            return true;
        }
        return false;
    }

    // Handles adding a message to the cache or updating its position if it was already present.
    private void AddMessageToCache(string key, string msg, string value)
    {
        _messageCache.TryAdd(key, new OrderedDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        var messageCache = _messageCache[key];
        messageCache.Remove(msg);
        messageCache.Add(msg, value);
        if (messageCache.Count > _messageCacheMaxSize)
            messageCache.RemoveAt(0);
    }

    // Tries to get a word from the cache. Words in the common list live in their own cache and are never evicted after
    // being populated.
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

    // Adds a word to the cache, resetting its position if it was already in the cache.
    // If the word is a common word it is added to that cache instead, which is never purged or updated.
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
