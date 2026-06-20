// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega
// лицензированы под GNU GPL v3:
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Shared._Wega.Lavaland.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Lavaland.EntityEffects;

public sealed partial class StabilizeLegionCore : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent<LegionCoreComponent>(args.TargetEntity, out var core) ||
            !core.Active)
        {
            return;
        }

        core.Stabilized = true;
    }

    protected override string ReagentEffectGuidebookText(
        IPrototypeManager prototype,
        IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-stabilize-legion-core");
    }
}
