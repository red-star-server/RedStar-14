// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega
// лицензированы под GNU GPL v3:
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Shared.Maps;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Lavaland.Components.Artefacts;

[RegisterComponent]
public sealed partial class LavaStaffComponent : Component
{
    [DataField]
    public EntProtoId LavaEntity = "FloorLavaEntity";

    [DataField]
    public ProtoId<ContentTileDefinition> BasaltTile = "FloorBasaltLavaland";

    [DataField]
    public SoundSpecifier? UseSound;
}

[RegisterComponent]
public sealed partial class DragonBloodComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> Skeleton = "WizardForcedSkeleton";

    [DataField]
    public EntProtoId LowerDrakeAction = "BecomeToDrakeAction";

    [DataField]
    public TimeSpan UseTime = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");
}

[RegisterComponent]
public sealed partial class SoulStorageComponent : Component
{
    [DataField]
    public float BonusDamagePerSoul = 4f;

    [DataField]
    public float MaxBonusDamage = 76f;

    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> StolenSouls = [];
}

[RegisterComponent]
public sealed partial class DivineVocalCordsImplantComponent : Component
{
    [DataField]
    public float Radius = 5f;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [ViewVariables]
    public TimeSpan NextUse;
}

[RegisterComponent]
public sealed partial class DivineVoiceCarrierComponent : Component
{
    [DataField]
    public EntityUid Implant;
}

[RegisterComponent]
public sealed partial class RodOfAsclepiusComponent : Component
{
    [DataField]
    public EntityUid? BoundTo;

    [DataField]
    public float HealRadius = 3f;

    [DataField]
    public TimeSpan HealInterval = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public TimeSpan NextHealTime;

    [DataField]
    public float HealAmount = 2f;
}

[RegisterComponent]
public sealed partial class LinkedCubeComponent : Component
{
    [DataField(required: true)]
    public EntProtoId PairPrototype;

    [DataField]
    public float MinTeleportDistance = 4f;

    [DataField]
    public bool IsPrimary;

    [ViewVariables]
    public EntityUid? LinkedCube;
}

[Serializable, NetSerializable]
public sealed partial class DragonBloodDoAfterEvent : SimpleDoAfterEvent;

public sealed partial class BecomeToDrakeActionEvent : InstantActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> LowerDrake = "LowerAshDrakePolymorph";

    [DataField]
    public EntProtoId ReturnBack = "DrakeReturnBackAction";
}

public sealed partial class DrakeReturnBackActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class RodOathDoAfterEvent : SimpleDoAfterEvent;
