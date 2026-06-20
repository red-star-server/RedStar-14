// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(LegionSystem))]
public sealed partial class LegionBossComponent : Component
{
    [ViewVariables]
    public LegionState CurrentState = LegionState.Summoning;
    [ViewVariables] public TimeSpan NextStateSwitchTime;
    [ViewVariables] public TimeSpan NextSummonTime;
    [ViewVariables] public TimeSpan NextChargeTime;

    [DataField]
    public float StateSwitchInterval = 30f;

    [DataField]
    public float SummonInterval = 6f;

    [DataField]
    public float ChargeInterval = 1.5f;

    [DataField]
    public int SummonCount = 2;

    [DataField]
    public EntProtoId MinionPrototype = "MobLegionSkull";

    [DataField]
    public List<EntProtoId> SplitPrototypes = new()
    {
        "MobMegaLegionSplitLeft",
        "MobMegaLegionSplitRight",
        "MobMegaLegionSplitEye"
    };

    [DataField]
    public Dictionary<EntProtoId, float> LootPrototypes = new();

    [DataField("rewards")]
    public List<EntProtoId> RewardsProto = new();
}

[RegisterComponent]
public sealed partial class LegionSplitComponent : Component
{
    [DataField("nextSplit")]
    public EntProtoId? NextSplitPrototype;

    [ViewVariables]
    public Guid SplitGroup;
}
