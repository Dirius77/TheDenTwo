using System.Text;
using System.Text.RegularExpressions;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._DEN.Language;

[ImplicitDataDefinitionForInheritors]
public partial interface ILanguageScrambler
{
    /// <summary>
    ///     Scrambles a message in this language.
    /// </summary>
    /// <param name="message">The word to be scrambled.</param>
    /// <param name="languageSystem">The language system, used for accessing word caches.</param>
    /// <param name="language">The language that this translation is occurring for.</param>
    /// <param name="understanding">The amount of understanding applicable to the scrambling.</param>
    /// <returns>Scrambled version of the message.</returns>
    string ScrambleMessage(string message, EntitySystems.SharedLanguageSystem languageSystem, ProtoId<LanguagePrototype> language, int understanding = 0);
}

/// <summary>
///     Uses a set of syllables to scramble a language based on particular words.
/// </summary>
public sealed partial class LanguageSyllableScrambler : ILanguageScrambler
{
    [DataField]
    public int MinSyllables { get; private set; } = 1;

    [DataField]
    public int MaxSyllables { get; private set; } = 3;

    [DataField(required: true)]
    public List<string> Syllables { get; private set; } = new();


    private static readonly Regex Lowercase = new("[a-z]", RegexOptions.Compiled);
    private static readonly Regex Sentence = new(@"(.+?(?:[\.!\?]|$))", RegexOptions.Compiled);
    private static readonly Regex Punctuation = new(@"[\.\!\?]", RegexOptions.Compiled);

    public string ScrambleMessage(string message, EntitySystems.SharedLanguageSystem languageSystem, ProtoId<LanguagePrototype> language, int understanding = 0)
    {
        // This should never happen but...
        if (understanding >= 100)
            return message;

        // Check if we have this cached. This is useful so we don't have to re-scramble the message for multiple listeners.
        if (languageSystem.TryGetMessageCachedValue(language.Id + "-" + understanding, message, out var value))
            return value;


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
                int commonality = languageSystem.GetCommonWords().GetValueOrDefault(trimmedWord, 1500);

                var prob = 10 * (1 - (commonality / 500));
                if (understanding > 0 && random.Next(100) <= understanding + prob)
                {
                    builder.Append(trimmedWord);
                    builder.Append(' ');
                    firstWord = false;
                    continue;
                }

                if (languageSystem.TryGetWordCachedValue(language, trimmedWord, out var cachedWord))
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
                var count = random.Next(MinSyllables, MaxSyllables + 1);
                for (var i = 0; i < count; i++)
                {
                    var syllable = random.Pick(Syllables);
                    if (firstWord)
                    {
                        syllable = string.Concat(syllable[0].ToString().ToUpper(), syllable.AsSpan(1));
                        firstWord = false;
                    }

                    wordBuilder.Append(syllable);
                }
                var scrambledWord = wordBuilder.ToString();
                languageSystem.AddWordToCache(language, trimmedWord, scrambledWord.ToLower());
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
        languageSystem.AddMessageToCache(language.Id + "-" + understanding, message, result);
        return result;
    }
}
