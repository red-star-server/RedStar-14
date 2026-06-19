// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Server._Wega.Lavaland.Mobs.Components;
using Content.Shared._Wega.Lavaland.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Lavaland.Mobs;

public sealed partial class LegionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LegionBossComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LegionBossComponent, MegaLegionAction>(OnAction);
        SubscribeLocalEvent<LegionBossComponent, MobStateChangedEvent>(OnBossKilled);
        SubscribeLocalEvent<LegionSplitComponent, MobStateChangedEvent>(OnSplitKilled);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<LegionBossComponent>();
        while (query.MoveNext(out _, out var component))
        {
            if (_timing.CurTime < component.NextStateSwitchTime)
                continue;

            component.CurrentState = component.CurrentState == LegionState.Summoning
                ? LegionState.Charging
                : LegionState.Summoning;
            component.NextStateSwitchTime = _timing.CurTime + TimeSpan.FromSeconds(component.StateSwitchInterval);
        }
    }

    private void OnMapInit(Entity<LegionBossComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextStateSwitchTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.StateSwitchInterval);
        ent.Comp.NextSummonTime = _timing.CurTime;
        ent.Comp.NextChargeTime = _timing.CurTime;
    }

    private void OnAction(Entity<LegionBossComponent> ent, ref MegaLegionAction args)
    {
        if (_mobState.IsIncapacitated(ent) || _mobState.IsIncapacitated(args.Target))
            return;

        args.Handled = true;
        if (ent.Comp.CurrentState == LegionState.Summoning)
        {
            if (_timing.CurTime < ent.Comp.NextSummonTime)
                return;

            for (var i = 0; i < ent.Comp.SummonCount; i++)
                Spawn(ent.Comp.MinionPrototype, Transform(ent).Coordinates);

            ent.Comp.NextSummonTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.SummonInterval);
            return;
        }

        if (_timing.CurTime < ent.Comp.NextChargeTime)
            return;

        var direction = (Transform(args.Target).Coordinates.Position - Transform(ent).Coordinates.Position).Normalized();
        _throwing.TryThrow(ent, Transform(ent).Coordinates.Offset(direction * 6f), 15f);
        ent.Comp.NextChargeTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.ChargeInterval);
    }

    private void OnBossKilled(Entity<LegionBossComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var coords = Transform(ent).Coordinates;
        foreach (var (prototype, chance) in ent.Comp.LootPrototypes)
        {
            if (_random.Prob(chance))
                Spawn(prototype, coords);
        }

        if (!HasComp<LegionSplitComponent>(ent))
        {
            foreach (var prototype in ent.Comp.SplitPrototypes)
                Spawn(prototype, coords);
        }

        QueueDel(ent);
    }

    private void OnSplitKilled(Entity<LegionSplitComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var coords = Transform(ent).Coordinates;
        if (ent.Comp.NextSplitPrototype is { } nextSplit)
        {
            Spawn(nextSplit, coords);
            Spawn(nextSplit, coords);
        }
        else if (TryComp<LegionBossComponent>(ent, out var boss))
        {
            foreach (var reward in boss.RewardsProto)
                Spawn(reward, coords);
        }

        QueueDel(ent);
    }
}
