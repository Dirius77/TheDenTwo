using Content.Server._MACRO.Speech.Components;
using Content.Server.Speech.Components;
using Content.Shared._DEN.Speech;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.Whitelist;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class SpeechRequiresEquipmentSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeechRequiresEquipmentComponent, SpeakLanguageAttemptEvent>(OnSpeechAttempt); // DEN: Languages
    }

    public void OnSpeechAttempt(Entity<SpeechRequiresEquipmentComponent> ent, ref SpeakLanguageAttemptEvent args) // DEN: Languages
    {
        if (_inventory.TryGetContainerSlotEnumerator(ent.Owner, out var enumerator, SlotFlags.WITHOUT_POCKET))
        {
            while (enumerator.NextItem(out var item, out _))
            {
                if (TryComp<SpeechSoundComponent>(item, out var comp)
                    && _whitelist.CheckBoth(item, ent.Comp.Blacklist, ent.Comp.Whitelist))
                    return;
            }
        }

        args.Cancel();
        if (ent.Comp.FailMessage != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.FailMessage), ent, ent);
    }
}
