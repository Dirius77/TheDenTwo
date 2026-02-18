using Content.Server.Telephone;
using Content.Shared.Holopad;

namespace Content.Server.Holopad;

public sealed partial class HolopadSystem
{
    private void InitializeLanguage()
    {
        SubscribeLocalEvent<HolopadComponent, TelephoneMessageLanguageSentEvent>(OnTelephoneMessageLanguageSent);
    }

    private void OnTelephoneMessageLanguageSent(Entity<HolopadComponent> holopad,
        ref TelephoneMessageLanguageSentEvent args)
    {
        LinkHolopadToUser(holopad, args.MessageSource);
    }
}
