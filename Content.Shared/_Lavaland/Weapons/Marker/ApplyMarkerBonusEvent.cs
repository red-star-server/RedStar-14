using Content.Shared.Damage;

namespace Content.Shared._Lavaland.Weapons.Marker;

/// <summary>
/// Lavaland marker effect retained for existing charge and trophy integrations.
/// </summary>
public record struct ApplyMarkerBonusEvent(EntityUid Weapon, EntityUid User);

/// <summary>
/// Raised on the crusher before marker bonus damage and healing are calculated.
/// </summary>
[ByRefEvent]
public record struct MarkerAttackAttemptEvent(
    EntityUid Weapon,
    EntityUid User,
    EntityUid Target,
    float DamageModifier = 1f,
    float HealModifier = 1f);

/// <summary>
/// Raised on the crusher after a marker has been consumed.
/// </summary>
[ByRefEvent]
public record struct AfterMarkerAttackedEvent(
    EntityUid Weapon,
    EntityUid User,
    EntityUid Target,
    DamageSpecifier Damage);
