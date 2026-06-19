// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(AshDrakeSystem))]
public sealed partial class AshDrakeBossComponent : Component
{
    [DataField] public EntProtoId MeteorCircle = "EffectAshDrakeCircle";
    [DataField] public EntProtoId Shadow = "EffectAshDrakeShadow";
    [DataField] public EntProtoId FireTrail = "EffectAshDrakeFire";

    [DataField] public SoundSpecifier AttackSound = new SoundPathSpecifier("/Audio/Magic/fireball.ogg");

    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    public string TargetKey = "Target";
}
