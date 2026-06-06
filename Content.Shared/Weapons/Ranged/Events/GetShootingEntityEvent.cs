namespace Content.Shared.Weapons.Ranged.Events;

// RS14-start
[ByRefEvent]
public struct GetShootingEntityEvent
{
    public EntityUid? ShootingEntity;

    public bool Handled;
}
// RS14-end
