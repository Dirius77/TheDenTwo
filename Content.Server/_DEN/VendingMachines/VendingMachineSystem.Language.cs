using Content.Server.Vocalization.Systems;
using Content.Shared.VendingMachines;

namespace Content.Server.VendingMachines;

public sealed partial class VendingMachineSystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<VendingMachineComponent, TryVocalizeLanguageEvent>(OnTryVocalizeLanguage);
    }

    private void OnTryVocalizeLanguage(Entity<VendingMachineComponent> ent, ref TryVocalizeLanguageEvent args)
    {
        args.Cancelled |= ent.Comp.Broken;
    }
}
