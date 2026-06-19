// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Shared._Wega.Trigger.Systems;
using Content.Shared.Whitelist;

namespace Content.Shared._Wega.Trigger.Components;

[RegisterComponent, Access(typeof(DamageOnTriggerBlacklistSystem))]
public sealed partial class DamageOnTriggerBlacklistComponent : Component
{
    [DataField(required: true)]
    public EntityWhitelist Blacklist = new();
}
