// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Mech.Components;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Applies recharge accumulated by passive mech power modules to the mech battery.
/// </summary>
public sealed class MechBatteryRechargeApplySystem : EntitySystem
{
    [Dependency] private readonly MechSystem _mech = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MechComponent, MechEnergyAccumulatorComponent>();
        while (query.MoveNext(out var uid, out var mech, out var accumulator))
        {
            var rechargeRate = accumulator.PendingRechargeRate;
            accumulator.PendingRechargeRate = 0f;

            if (rechargeRate <= 0f)
                continue;

            _mech.TryChangeEnergy(uid, FixedPoint2.New(rechargeRate * frameTime), mech);
        }
    }
}
