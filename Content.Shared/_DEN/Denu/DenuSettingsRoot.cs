using Content.Shared._DEN.Denu.Chat;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared._DEN.Denu;

[Serializable, NetSerializable]
public sealed class DenuSettingsRoot
{
    private const int MaxExclusiveItems = 32;
    private const int MaxProfileIdsPerItem = 32;

    public DenuModules Modules { get; set; } = new();

    public void EnsureValid(HashSet<int> validProfileIds)
    {
        Modules ??= new DenuModules();
        Modules.Chat ??= new ChatModule();
        Modules.Chat.Formatter ??= new FormatterModule();
        Modules.Chat.Formatter.Items ??= new List<FormatterSettings>();
        Modules.Chat.EarmuffRange ??= new EarmuffRangeModule();
        Modules.Chat.EarmuffRange.Items ??= new List<EarmuffRangeSettings>();

        EnsureFormatterSettings(validProfileIds);
        EnsureEarmuffRangeSettings(validProfileIds);
    }

    private void EnsureFormatterSettings(HashSet<int> validProfileIds)
    {
        var items = Modules.Chat.Formatter.Items;
        TrimItems(items, MaxExclusiveItems);

        foreach (var item in items)
        {
            item.ProfileIds ??= new List<int>();
        }

        EnsureScopes(items, validProfileIds);

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                item.Id = Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(item.DialogueColor) || !Color.TryFromHex(item.DialogueColor, out _))
                item.DialogueColor = "#FFFFFF";

            if (string.IsNullOrWhiteSpace(item.EmoteColor) || !Color.TryFromHex(item.EmoteColor, out _))
                item.EmoteColor = "#FF13FF";
        }
    }

    private void EnsureEarmuffRangeSettings(HashSet<int> validProfileIds)
    {
        List<EarmuffRangeSettings> items = Modules.Chat.EarmuffRange.Items;
        TrimItems(items, MaxExclusiveItems);

        foreach (EarmuffRangeSettings item in items)
        {
            item.ProfileIds ??= new List<int>();
        }

        EnsureScopes(items, validProfileIds);

        foreach (EarmuffRangeSettings item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                item.Id = Guid.NewGuid().ToString();

            if (float.IsNaN(item.Value) || float.IsInfinity(item.Value))
                item.Value = 10.0f;

            item.Value = Math.Clamp(item.Value, 1.0f, 20.0f);
        }
    }

    private static void TrimItems<T>(List<T> items, int maxItems)
    {
        if (items.Count <= maxItems)
            return;

        items.RemoveRange(0, items.Count - maxItems);
    }

    private static void EnsureScopes<T>(List<T> items, HashSet<int> validProfileIds)
        where T : IScopedItem
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            List<int> profileIds = items[i].ProfileIds;
            bool wasScoped = profileIds.Count != 0;
            ScopeResolver.CanonicaliseProfileIds(profileIds, validProfileIds);

            if (profileIds.Count > MaxProfileIdsPerItem)
                profileIds.RemoveRange(MaxProfileIdsPerItem, profileIds.Count - MaxProfileIdsPerItem);

            if (wasScoped && profileIds.Count == 0)
                items.RemoveAt(i);
        }
    }
}
