// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega
// лицензированы под GNU GPL v3:
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Lavaland.Components;

[RegisterComponent]
public sealed partial class FloraComponent : Component
{
    [DataField]
    public TimeSpan MinGrowthTime = TimeSpan.FromMinutes(20);

    [DataField]
    public TimeSpan MaxGrowthTime = TimeSpan.FromMinutes(30);

    [DataField]
    public TimeSpan NextGrowthTime;

    [DataField]
    public bool IsGrown = true;

    [DataField(required: true)]
    public EntProtoId HarvestPrototype;

    [DataField]
    public int MinYield = 1;

    [DataField]
    public int MaxYield = 3;

    [DataField]
    public ProtoId<ToolQualityPrototype>? SpecialTool;

    [DataField]
    public TimeSpan HarvestDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier? HarvestSound;
}

[Serializable, NetSerializable]
public enum FloraVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum FloraState : byte
{
    Grown,
    Harvested,
}

[Serializable, NetSerializable]
public sealed partial class FloraHarvestDoAfterEvent : SimpleDoAfterEvent;
