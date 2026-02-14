using Content.Server.Animals.Components;
using Content.Shared._DEN.Speech;

namespace Content.Server.Animals.Systems;

public sealed partial class ParrotMemorySystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<ParrotListenerComponent, ListenLanguageEvent>(OnLanguageListen);
    }

    private void OnLanguageListen(Entity<ParrotListenerComponent> entity, ref ListenLanguageEvent args)
    {

    }
}
