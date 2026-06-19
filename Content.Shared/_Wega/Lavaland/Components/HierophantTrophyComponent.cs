// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Shared._Wega.Lavaland.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Lavaland.Components;

[RegisterComponent, Access(typeof(HierophantTrophySystem))]
public sealed partial class HierophantTrophyComponent : Component
{
    [DataField]
    public EntProtoId WallPrototype = "WallHierophantTrophy";
}
