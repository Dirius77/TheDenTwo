using Content.Client._DEN.Denu.Core;
using Content.Client._DEN.Denu.UI.Denu;
using Content.Shared._DEN.Denu.Chat;
using UiControl = Robust.Client.UserInterface.Control;

namespace Content.Client._DEN.Denu.Modules.Chat;

public sealed class ChatFormatterDenuModule : DenuExclusiveModule<FormatterSettings>
{
    public override string Id => "formatter";

    public override string DisplayName => "Chat Formatter";

    public override int TabOrder => 0;

    private FormatterTab? _tab;

    private readonly MessageFormatter.FormatterConfig _effectiveFormatterConfig = CreateFormatterConfig();
    private readonly MessageFormatter.FormatterConfig _previewFormatterConfig = CreateFormatterConfig();

    private bool _effectiveEnabled;

    public bool Enabled { get; private set; }

    public bool RemoveAsterisks { get; private set; }

    public string DialogueColor { get; private set; } = "#FFFFFF";

    public string EmoteColor { get; private set; } = "#FF13FF";

    public MessageFormatter.FormatterConfig FormatterConfig => _effectiveFormatterConfig;

    protected override List<FormatterSettings> Items => Context.SettingsRoot.Modules.Chat.Formatter.Items;

    public event Action? OnFormatterChanged;

    protected override FormatterSettings CreateDefaultSettings()
    {
        return new FormatterSettings();
    }

    protected override void CopySettings(FormatterSettings source, FormatterSettings target)
    {
        target.Enabled = source.Enabled;
        target.RemoveAsterisks = source.RemoveAsterisks;
        target.DialogueColor = source.DialogueColor;
        target.EmoteColor = source.EmoteColor;
    }

    protected override void ApplyEffectiveSettings(FormatterSettings settings)
    {
        _effectiveEnabled = settings.Enabled;
        ApplyConfig(_effectiveFormatterConfig, settings);
        OnFormatterChanged?.Invoke();
    }

    protected override void OnEditSettingsUpdated(FormatterSettings settings)
    {
        Enabled = settings.Enabled;
        RemoveAsterisks = settings.RemoveAsterisks;
        DialogueColor = settings.DialogueColor;
        EmoteColor = settings.EmoteColor;

        ApplyConfig(_previewFormatterConfig, settings);
        _tab?.RefreshFromSettings();
    }

    public override UiControl CreateTabContent()
    {
        _tab = new FormatterTab(this);
        return _tab;
    }

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;

        if (!TryGetOrCreateEditSettings(out FormatterSettings settings))
            return;

        settings.Enabled = enabled;
        ApplyConfig(_previewFormatterConfig, settings);

        if (IsEditingEffectiveItem())
            ApplyEffectiveSettings(settings);

        SaveNow();
    }

    public void SetRemoveAsterisks(bool removeAsterisks)
    {
        RemoveAsterisks = removeAsterisks;

        if (!TryGetOrCreateEditSettings(out FormatterSettings settings))
            return;

        settings.RemoveAsterisks = removeAsterisks;
        ApplyConfig(_previewFormatterConfig, settings);

        if (IsEditingEffectiveItem())
            ApplyEffectiveSettings(settings);

        SaveNow();
    }

    public void SetDialogueColor(Color color)
    {
        DialogueColor = color.ToHex();

        if (!TryGetOrCreateEditSettings(out FormatterSettings settings))
            return;

        settings.DialogueColor = DialogueColor;
        ApplyConfig(_previewFormatterConfig, settings);

        if (IsEditingEffectiveItem())
            ApplyEffectiveSettings(settings);

        SaveDebounced("formatter.dialogue", TimeSpan.FromMilliseconds(150));
    }

    public void SetEmoteColor(Color color)
    {
        EmoteColor = color.ToHex();

        if (!TryGetOrCreateEditSettings(out FormatterSettings settings))
            return;

        settings.EmoteColor = EmoteColor;
        ApplyConfig(_previewFormatterConfig, settings);

        if (IsEditingEffectiveItem())
            ApplyEffectiveSettings(settings);

        SaveDebounced("formatter.emote", TimeSpan.FromMilliseconds(150));
    }

    public Color GetDialogueColor()
    {
        return !Color.TryFromHex(DialogueColor, out var color) ? Color.White : color;
    }

    public Color GetEmoteColor()
    {
        return !Color.TryFromHex(EmoteColor, out var color) ? Color.Magenta : color;
    }

    public string FormatMessage(string message)
    {
        if (!_effectiveEnabled)
            return message;

        return MessageFormatter.Format(message, _effectiveFormatterConfig);
    }

    public string FormatPreviewMessage(string message)
    {
        return MessageFormatter.Format(message, _previewFormatterConfig);
    }

    private static MessageFormatter.FormatterConfig CreateFormatterConfig()
    {
        return new MessageFormatter.FormatterConfig
        {
            Rules = new()
            {
                new("***", "[bolditalic]", "[/bolditalic]", false, false),
                new("**", "[bold]", "[/bold]", false, false),
                new("\"", "\"[color={DialogueColor}]", "[/color]\"", false, true),
                new("*", "[italic]", "[/italic]", true, false),
                new("*", "[italic][color={EmoteColor}]*", "*[/color][/italic]", false, false),
            },
            Replacements = new()
            {
                { "DialogueColor", "#FFFFFF" },
                { "EmoteColor", "#FF13FF" }
            },
            AllowEscaping = true,
            EscapableTokens = new() { '*', '"', '\\' },
            RemoveAsterisks = false
        };
    }

    private static void ApplyConfig(MessageFormatter.FormatterConfig config, FormatterSettings settings)
    {
        config.RemoveAsterisks = settings.RemoveAsterisks;
        config.Replacements["DialogueColor"] = settings.DialogueColor;
        config.Replacements["EmoteColor"] = settings.EmoteColor;
    }
}
