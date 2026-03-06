namespace Content.Shared._DEN.Language.Components;

[RegisterComponent]
public sealed partial class UniversalLanguageSpeakerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Entity<LanguageComponent> UniversalLanguage;
}
