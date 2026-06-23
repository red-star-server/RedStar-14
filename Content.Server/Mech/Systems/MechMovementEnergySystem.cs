// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Vehicle;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Drains mech battery charge while an occupied mech is actively moving.
/// </summary>
public sealed class MechMovementEnergySystem : EntitySystem
{
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly VehicleSystem _vehicle = default!;

    private readonly HashSet<EntityUid> _activeMechs = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, MechMovementDrainToggleEvent>(OnDrainToggle);
        SubscribeLocalEvent<MechComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnDrainToggle(Entity<MechComponent> ent, ref MechMovementDrainToggleEvent args)
    {
        SetDrainEnabled(ent.Owner, args.Enabled);
    }

    private void OnShutdown(Entity<MechComponent> ent, ref ComponentShutdown args)
    {
        _activeMechs.Remove(ent.Owner);
    }

    private void SetDrainEnabled(EntityUid uid, bool enabled)
    {
        if (enabled)
            _activeMechs.Add(uid);
        else
            _activeMechs.Remove(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_activeMechs.Count == 0)
            return;

        foreach (var uid in _activeMechs.ToArray())
        {
            if (!TryComp<MechComponent>(uid, out var mech) ||
                !TryComp<InputMoverComponent>(uid, out var mover))
            {
                _activeMechs.Remove(uid);
                continue;
            }

            if (mech.MovementEnergyPerSecond <= 0f)
                continue;

            if (!mover.CanMove || mover.WishDir == Vector2.Zero)
                continue;

            var requestedDrain = FixedPoint2.New(mech.MovementEnergyPerSecond * frameTime);
            if (requestedDrain <= 0)
                continue;

            var actualDrain = requestedDrain > mech.Energy
                ? mech.Energy
                : requestedDrain;

            if (actualDrain <= 0 || !_mech.TryChangeEnergy(uid, -actualDrain, mech))
            {
                _vehicle.RefreshCanRun(uid);
                continue;
            }

            if (mech.Energy <= 0)
                _vehicle.RefreshCanRun(uid);
        }
    }
}
