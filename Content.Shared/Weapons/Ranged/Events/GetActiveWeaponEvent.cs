namespace Content.Shared.Weapons.Ranged.Events;

// RS14-start
[ByRefEvent]
public struct GetActiveWeaponEvent
{
    public EntityUid? Weapon;

    public bool Handled;
}
// RS14-end
