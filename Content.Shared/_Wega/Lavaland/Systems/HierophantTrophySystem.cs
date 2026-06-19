// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using System.Numerics;
using Content.Shared._Lavaland.Weapons.Marker;
using Content.Shared._Wega.Lavaland.Components;
using Robust.Shared.Network;

namespace Content.Shared._Wega.Lavaland.Systems;

public sealed class HierophantTrophySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HierophantTrophyComponent, ApplyMarkerBonusEvent>(OnMarkerBonus);
    }

    private void OnMarkerBonus(Entity<HierophantTrophyComponent> ent, ref ApplyMarkerBonusEvent args)
    {
        if (!_net.IsServer || !Exists(args.User))
            return;

        var transform = Transform(args.User);
        var direction = transform.LocalRotation.ToWorldVec().Normalized();
        var perpendicular = new Vector2(-direction.Y, direction.X);

        for (var i = -1; i <= 1; i++)
            Spawn(ent.Comp.WallPrototype, transform.Coordinates.Offset(perpendicular * i));
    }
}
