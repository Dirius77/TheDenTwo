using Content.Client._DEN.Language.EntitySystems;
using Content.Shared._DEN.Language.Components;
using Content.Shared.Examine;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._DEN.UserInterface.Systems.Language.Controls;

public sealed class LanguageContainer : Control
{
    private IEntityManager _entities;
    private IPlayerManager _player;
    private IPrototypeManager _proto;
    private LanguageSystem _language;

    public event Action? SpeakPressed;

    public Entity<LanguageComponent>? LanguageEnt { get; private set; }

    private Label _languageName;
    private Button _languageButton;
    private RichTextLabel _description;
    private BoxContainer _header;

    private bool CurrentlySpoken;

    public LanguageContainer(IEntityManager entities, IPlayerManager player, IPrototypeManager proto, LanguageSystem language)
    {
        _entities = entities;
        _player = player;
        _proto = proto;
        _language = language;

        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };

        _header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };

        _languageName = new Label
        {
            Name = "LanguageName",
            HorizontalExpand = true,
        };

        _languageButton = new Button { Text = Loc.GetString("language-ui-speak-language") };
        _languageButton.OnPressed += _ => SpeakPressed?.Invoke();

        _header.AddChild(_languageName);
        _header.AddChild(_languageButton);

        container.AddChild(_header);

        var cbody = new CollapsibleBody
        {
            HorizontalExpand = true,
            Margin = new Thickness(4f, 4f),
        };

        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };

        _description = new RichTextLabel { HorizontalExpand = true };
        body.AddChild(_description);
        cbody.AddChild(body);

        var collapsible = new Collapsible(Loc.GetString("language-ui-language-description"), cbody)
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };
        container.AddChild(collapsible);

        var wrapper = new PanelContainer
        {
            Margin = new Thickness(4f),
        };
        wrapper.StyleClasses.Add("PdaBorderRect");
        wrapper.AddChild(container);

        AddChild(wrapper);

        SpeakPressed += OnLanguageChosen;
    }

    public void UpdateLanguage(Entity<LanguageComponent> language)
    {
        LanguageEnt = language;
        UpdateData();
    }

    public void SetCurrentSpoken(bool current)
    {
        if (current)
        {
            if (_header.Children.Contains(_languageButton))
                _header.RemoveChild(_languageButton);
        }
        else
        {
            if (!_header.Children.Contains(_languageButton))
                _header.AddChild(_languageButton);
        }
    }

    private void UpdateData()
    {
        if (LanguageEnt == null || _player.LocalEntity == null)
            return;

        var langProto = _proto.Index(LanguageEnt.Value.Comp.Language);
        var fluencyProto = _proto.Index(LanguageEnt.Value.Comp.Fluency);

        _languageName.Text = langProto.LocalizedName;

        _languageButton.Disabled = !LanguageEnt.Value.Comp.Speaks;

        var desc = FormattedMessage.FromMarkupPermissive(
            Loc.GetString("language-ui-language-fluency",
                ("fluency", Loc.GetString(fluencyProto.Name)),
                ("color", Color.InterpolateBetween(Color.Red, Color.Green, (float)(fluencyProto.Understanding / 100.0)))));
        desc.PushNewline();
        desc.AddMarkupPermissive(langProto.LocalizedDescription);

        var ev = new ExaminedEvent(desc, LanguageEnt.Value, _player.LocalEntity.Value, true, !desc.IsEmpty);
        _entities.EventBus.RaiseLocalEvent(LanguageEnt.Value, ev);

        _description.SetMessage(ev.GetTotalMessage());
    }

    private void OnLanguageChosen()
    {
        if (LanguageEnt != null)
            _language.TrySetSpokenLanguage(LanguageEnt.Value);
    }
}
