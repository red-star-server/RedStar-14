// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega
// лицензированы под GNU GPL v3:
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Shared.Damage;

namespace Content.Shared._Wega.Lavaland.Components;

[RegisterComponent]
public sealed partial class LegionCoreComponent : Component
{
    [DataField]
    public bool Active = true;

    [DataField]
    public bool Stabilized;

    [DataField]
    public DamageSpecifier HealAmount = new();

    [DataField]
    public TimeSpan ActiveDuration = TimeSpan.FromSeconds(150);

    [ViewVariables]
    public TimeSpan ActiveEndTime;
}
