namespace Content.Shared.Telephone;

public sealed partial class TelephoneComponent
{
    // Controls whether visual languages can be spoken over the 'telephone'
    // All the instances of this currently in the game do this, but if it ever changes, it's here.
    [DataField]
    public bool TransmitsVisuals = true;
}
