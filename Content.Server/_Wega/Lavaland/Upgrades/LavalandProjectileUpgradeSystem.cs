// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using System.Linq;
using Content.Server.NPC.Components;
using Content.Shared._Wega.Lavaland.Upgrades;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Lavaland.Upgrades;

public sealed class LavalandProjectileUpgradeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileTimerResetUpgradeComponent, ProjectileHitEvent>(OnTimerResetHit);
        SubscribeLocalEvent<ProjectileAreaDamageComponent, ProjectileHitEvent>(OnAreaDamageHit);
    }

    private void OnTimerResetHit(Entity<ProjectileTimerResetUpgradeComponent> ent, ref ProjectileHitEvent args)
    {
        if (TryComp<NPCUseActionOnTargetComponent>(args.Target, out var single)
            && single.ActionEnt is { } singleAction)
            IncreaseCooldown(singleAction, ent.Comp.CooldownIncrease);

        if (!TryComp<NPCUseActionsOnTargetComponent>(args.Target, out var multiple))
            return;

        foreach (var (_, action) in multiple.ActionEnts)
        {
            if (action is { } actionUid)
                IncreaseCooldown(actionUid, ent.Comp.CooldownIncrease);
        }
    }

    private void IncreaseCooldown(EntityUid actionUid, float seconds)
    {
        if (!TryComp<ActionComponent>(actionUid, out var action))
            return;

        var remaining = action.Cooldown?.End - _timing.CurTime ?? TimeSpan.Zero;
        _actions.SetCooldown(actionUid, remaining + TimeSpan.FromSeconds(seconds));
    }

    private void OnAreaDamageHit(Entity<ProjectileAreaDamageComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter || shooter == args.Target)
            return;

        var hitTarget = args.Target;
        var targets = _lookup.GetEntitiesInRange<DamageableComponent>(
                Transform(hitTarget).Coordinates,
                ent.Comp.DamageRadius)
            .Where(target => target.Owner != shooter && target.Owner != hitTarget);

        foreach (var target in targets)
            _damage.TryChangeDamage(target.Owner, args.Damage * ent.Comp.DamageMultiplier, origin: shooter);
    }
}
