using System.Linq;
using System.Text;
using Robust.Shared.Utility;

namespace Content.Shared._DEN.Utility;

public sealed class StringUtil
{
    public static string FilterStringTags(string original, string[] allowedTags)
    {
        var parsed = FormattedMessage.FromMarkupPermissive(original);
        var filtered = new StringBuilder();
        foreach (var node in parsed)
        {
            if (node.Name is { } nodeName && !allowedTags.Contains(nodeName, StringComparer.OrdinalIgnoreCase))
                continue;

            filtered.Append(node);
        }

        return filtered.ToString();
    }
}