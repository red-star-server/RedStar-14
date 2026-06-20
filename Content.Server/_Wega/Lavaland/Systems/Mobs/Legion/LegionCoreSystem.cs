// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega
// лицензированы под GNU GPL v3:
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Server.Popups;
using Content.Shared._Wega.Lavaland.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Lavaland.Systems.Mobs.Legion;

public sealed class LegionCoreSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LegionCoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LegionCoreComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<LegionCoreComponent, AfterInteractEvent>(OnInteract);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<LegionCoreComponent>();
        while (query.MoveNext(out var uid, out var core))
        {
            if (!core.Active || core.Stabilized || core.ActiveEndTime > _timing.CurTime)
                continue;

            core.Active = false;
            _appearance.SetData(uid, VisualLayers.Enabled, false);
        }
    }

    private void OnMapInit(Entity<LegionCoreComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ActiveEndTime = _timing.CurTime + ent.Comp.ActiveDuration;
        _appearance.SetData(ent, VisualLayers.Enabled, true);
    }

    private void OnUse(Entity<LegionCoreComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryHeal(args.User, args.User, ent);
    }

    private void OnInteract(Entity<LegionCoreComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        args.Handled = TryHeal(target, args.User, ent);
    }

    private bool TryHeal(EntityUid target, EntityUid user, Entity<LegionCoreComponent> core)
    {
        if (!core.Comp.Stabilized && core.Comp.ActiveEndTime <= _timing.CurTime)
        {
            core.Comp.Active = false;
            _appearance.SetData(core, VisualLayers.Enabled, false);
        }

        if (!core.Comp.Active)
        {
            _popup.PopupEntity(
                Loc.GetString("legion-core-inert"),
                core,
                user,
                PopupType.SmallCaution);
            return false;
        }

        if (!HasComp<DamageableComponent>(target))
            return false;

        _damage.TryChangeDamage(target, core.Comp.HealAmount, true, false);
        QueueDel(core);
        return true;
    }
}
