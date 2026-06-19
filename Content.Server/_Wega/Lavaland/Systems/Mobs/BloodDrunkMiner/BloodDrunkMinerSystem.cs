// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Server._Wega.Lavaland.Mobs.Components;
using Content.Shared._Wega.Lavaland.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Wega.Lavaland.Mobs;

public sealed partial class BloodDrunkMinerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodDrunkMinerComponent, BloodDrunkMinerDashAction>(OnDash);
    }

    private void OnDash(Entity<BloodDrunkMinerComponent> ent, ref BloodDrunkMinerDashAction args)
    {
        args.Handled = true;
        _transform.SetCoordinates(ent, args.Target);
        _audio.PlayPvs(args.DashSound, ent);
    }
}
