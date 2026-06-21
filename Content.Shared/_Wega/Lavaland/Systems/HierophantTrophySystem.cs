// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using System.Numerics;
using Content.Shared._Lavaland.Weapons.Marker;
using Content.Shared._Wega.Lavaland.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Wega.Lavaland.Systems;

public sealed class HierophantTrophySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HierophantTrophyComponent, ApplyMarkerBonusEvent>(OnMarkerBonus);
        SubscribeLocalEvent<HierophantTrophyComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMarkerBonus(Entity<HierophantTrophyComponent> ent, ref ApplyMarkerBonusEvent args)
    {
        if (!_net.IsServer || !Exists(args.User))
            return;

        SpawnWalls(ent.Comp, args.User);
    }

    private void OnMeleeHit(Entity<HierophantTrophyComponent> ent, ref MeleeHitEvent args)
    {
        if (!_net.IsServer || args.HitEntities.Count == 0 || _timing.CurTime < ent.Comp.NextActivation)
            return;

        ent.Comp.NextActivation = _timing.CurTime + ent.Comp.Cooldown;
        SpawnWalls(ent.Comp, args.User);
    }

    private void SpawnWalls(HierophantTrophyComponent component, EntityUid user)
    {
        if (!Exists(user))
            return;

        var transform = Transform(user);
        var direction = transform.LocalRotation.ToWorldVec().Normalized();
        var perpendicular = new Vector2(-direction.Y, direction.X);

        for (var i = -1; i <= 1; i++)
            Spawn(component.WallPrototype, transform.Coordinates.Offset(perpendicular * i));
    }
}
