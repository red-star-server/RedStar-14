// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Mech.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;

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

        SubscribeLocalEvent<MechComponent, VehicleOperatorSetEvent>(OnOperatorSet);
        SubscribeLocalEvent<MechComponent, MechMovementDrainToggleEvent>(OnDrainToggle);
        SubscribeLocalEvent<MechComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnOperatorSet(Entity<MechComponent> ent, ref VehicleOperatorSetEvent args)
    {
        SetDrainEnabled(ent.Owner, args.NewOperator != null);
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

            var toDrain = mech.MovementEnergyPerSecond * frameTime;
            if (!_mech.TryChangeEnergy(uid, -FixedPoint2.New(toDrain), mech))
                _vehicle.RefreshCanRun(uid);
        }
    }
}
