// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega
// лицензированы под GNU GPL v3:
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Lavaland.Events.Artefacts;

public sealed partial class BecomeToDrakeActionEvent : InstantActionEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> LowerDrake = "LowerAshDrakePolymorph";

    [DataField]
    public EntProtoId ReturnBackAction = "DrakeReturnBackAction";
}

public sealed partial class DrakeReturnBackActionEvent : InstantActionEvent;
