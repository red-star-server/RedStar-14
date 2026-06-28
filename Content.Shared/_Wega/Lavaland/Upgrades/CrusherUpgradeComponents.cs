// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Lavaland.Upgrades;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherLegionSkullUpgradeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float FireRateCoefficient = 1.3f;

    [DataField, AutoNetworkedField]
    public float MeleeAttackRateCoefficient = 1.15f;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherGoliathTentacleUpgradeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MaxCoefficient = 1f;

    [DataField, AutoNetworkedField]
    public MobState TargetState = MobState.Critical;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherAncientGoliathTentacleUpgradeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Coefficient = 0.5f;

    [DataField, AutoNetworkedField]
    public float HealthThreshold = 0.9f;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherWatcherWingUpgradeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float CooldownIncrease = 1f;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherMagmaWingUpgradeComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool Active;

    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherDemonClawsUpgradeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DamageMultiplier = 0.15f;

    [DataField, AutoNetworkedField]
    public DamageSpecifier MeleeHeal = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherBlasterTubesUpgradeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ProjectileSpeedCoefficient = 1.25f;
}

[RegisterComponent]
public sealed partial class ProjectileTimerResetUpgradeComponent : Component
{
    [DataField]
    public float CooldownIncrease = 1f;
}

[RegisterComponent]
public sealed partial class ProjectileAreaDamageComponent : Component
{
    [DataField]
    public float DamageRadius = 3f;

    [DataField]
    public float DamageMultiplier = 0.5f;
}
