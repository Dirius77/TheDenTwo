using System.Linq;
using System.Text;

namespace Content.Shared._DEN.Language;

[ImplicitDataDefinitionForInheritors]
public abstract partial class LanguageEffect
{
    // Linear Congruential Generator go brrrrrrrr
    // https://en.wikipedia.org/wiki/Linear_congruential_generator
    // These numbers are from glibc
    internal int SeededPseudoRandom(int seed, int min, int max)
    {
        uint a = 1103515245;
        uint m = 2147483648; // 2^31;
        uint c = 12345;
        long result = (a * seed + c) % m;
        return (int)(result % (max - min)) + min;
    }

    internal int StringToInt(string message)
    {
        ulong hash = 0;
        foreach (var c in message)
            hash += (c + 31 * hash);
        return (int)hash;
    }

    public abstract string Apply(string message, int roundId);
}

/// <summary>
/// Replaces the entire message with one of the available replacement phrases.
/// </summary>
public partial class ReplacementEffect : LanguageEffect
{
    /// <summary>
    /// The list of replacement phrases to use with this ReplacementEffect
    /// </summary>
    [DataField(required: true)]
    public List<string> Replacements { get; private set; }

    public override string Apply(string message, int roundId)
    {
        return Replacements[SeededPseudoRandom(StringToInt(message) + roundId, 0, Replacements.Count)];
    }
}

public abstract partial class PunctuationBasedEffect : ReplacementEffect
{
    /// <summary>
    /// Which characters are considered punctuation for the sake of preservation.
    /// These only do anything if the effect actually cares about punctuation.
    /// </summary>
    [DataField]
    public char[] PunctuationChars { get; private set; } = ['.', '!', '?', ',', ':'];

    /// <summary>
    /// Whether to reproduce punctuation characters. Spaces are always reproduced.
    /// </summary>
    [DataField]
    public bool PreservePunctuation { get; private set; } = true;
}

/// <summary>
/// Replaces individual words in the message with a sequence of syllables between min and max in length.
/// Specific words will always be translated into the same sequence of syllables within a round, meaning that
/// it is possible to 'learn' particular words.
/// </summary>
public sealed partial class SyllableReplacementEffect : PunctuationBasedEffect
{
    [DataField]
    public int MinSyllables { get; private set; } = 1;

    [DataField]
    public int MaxSyllables { get; private set; } = 4;

    public override string Apply(string message, int roundId)
    {
        var builder = new StringBuilder();
        var i = 0;
        // Start looping over the message
        while (i < message.Length)
        {
            var startIndex = i;
            // Loop till we hit punctuation.
            while (i < message.Length && (!PunctuationChars.Contains(message[i]) && message[i] != ' '))
                i++;

            if (i != startIndex)
            {
                var endIndex = i;
                var word = message.Substring(startIndex, endIndex-startIndex);
                var hash = StringToInt(word);
                // The max is exclusive.
                var count = SeededPseudoRandom(hash + roundId, MinSyllables, MaxSyllables + 1);

                // Add the syllables to the builder.
                for (var j = 0; j < count; j++)
                    builder.Append(Replacements[SeededPseudoRandom(hash + j + roundId, 0, Replacements.Count)]);
            }

            // Loop until we're NOT on punctuation anymore, and add it to the builder if needed.
            while (i < message.Length  && (PunctuationChars.Contains(message[i]) || message[i] == ' '))
            {
                if(message[i] == ' ')
                    builder.Append(' ');
                else if(PreservePunctuation)
                    builder.Append(message[i]);
                i++;
            }
        }

        return builder.ToString();
    }
}

/// <summary>
/// Replaces entire sentences in the message with a sequence of phrases. The number of phrases used is based
/// upon the length of the initial sentence.
/// </summary>
public sealed partial class PhraseObfuscationEffect : PunctuationBasedEffect
{
    [DataField]
    public int MinPhrases { get; private set; } = 1;

    [DataField]
    public int MaxPhrases { get; private set; } = 4;

    /// <summary>
    /// String used to separate added phrases.
    /// </summary>
    [DataField]
    public string Separator = " ";

    /// <summary>
    /// The power to which the length of each sentence is raised in order to determine the number of phrases used.
    /// </summary>
    [DataField]
    public float Proportion = 1f / 3;

    /// <summary>
    /// Which characters are considered punctuation for the sake of preservation.
    /// </summary>
    [DataField]
    public new char[] PunctuationChars { get; private set; } = ['.', '!', '?'];

    public override string Apply(string message, int roundId)
    {
        var builder = new StringBuilder();

        var i = 0;
        var sentenceLength = 0;
        var sentenceHash = 0;
        // Start looping over the message
        while (i < message.Length)
        {
            var startIndex = i;
            // Loop till we hit punctuation.
            while (i < message.Length && (!PunctuationChars.Contains(message[i]) && message[i] != ' '))
                i++;

            if (i != startIndex)
            {
                // Found a word.
                var endIndex = i;

                sentenceLength += endIndex - startIndex;
                sentenceHash += StringToInt(message.Substring(startIndex, endIndex - startIndex));

                // Found the end of a sentence.
                if (PunctuationChars.Contains(message[i]))
                {
                    var phraseCount = (int) Math.Round(Math.Clamp(Math.Pow(sentenceLength, Proportion), MinPhrases, MaxPhrases));

                    for (int j = 0; j < phraseCount; j++)
                    {
                        builder.Append(Replacements[SeededPseudoRandom(sentenceHash + j + roundId, 0, Replacements.Count)]);
                        if(j != phraseCount - 1)
                            builder.Append(Separator);
                    }
                }
            }

            // Loop until we're NOT on punctuation anymore, and add it to the builder if needed.
            while (i < message.Length && (PunctuationChars.Contains(message[i]) || message[i] == ' '))
            {
                if(message[i] == ' ')
                    builder.Append(' ');
                else if(PreservePunctuation)
                    builder.Append(message[i]);
                i++;
            }
        }
        return builder.ToString();
    }
}
