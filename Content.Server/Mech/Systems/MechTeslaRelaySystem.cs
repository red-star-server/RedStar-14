// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Server.Mech.Components;
using Content.Server.Power.Components;
using Content.Shared.APC;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech.Module.Components;
using Content.Goobstation.Maths.FixedPoint;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Recharges installed mech batteries while a Tesla relay module is near a powered APC.
/// </summary>
public sealed class MechTeslaRelaySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMechSystem _mech = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MechComponent>();
        while (query.MoveNext(out var mechUid, out var mech))
        {
            var uiDirty = false;

            foreach (var module in mech.ModuleContainer.ContainedEntities)
            {
                if (!TryComp<MechGeneratorModuleComponent>(module, out var generator) ||
                    generator.GenerationType != MechGenerationType.TeslaRelay)
                    continue;

                var telemetry = EnsureComp<MechEnergyAccumulatorComponent>(module);
                var radius = generator.Tesla?.Radius ?? 0f;
                var rate = generator.Tesla?.ChargeRate ?? 0f;
                var previousCurrent = telemetry.Current;
                var previousMax = telemetry.Max;

                telemetry.Max = rate;
                telemetry.Current = 0f;

                if (radius > 0f && rate > 0f && IsNearPoweredApc(mechUid, radius))
                {
                    telemetry.Current = rate;
                    _mech.TryChangeEnergy(mechUid, FixedPoint2.New(rate * frameTime), mech);
                }

                uiDirty |= Math.Abs(previousCurrent - telemetry.Current) > 0.01f ||
                           Math.Abs(previousMax - telemetry.Max) > 0.01f;
            }

            if (uiDirty)
                _mech.UpdateMechUi(mechUid);
        }
    }

    private bool IsNearPoweredApc(EntityUid mech, float radius)
    {
        var apcs = new HashSet<Entity<ApcComponent>>();
        _lookup.GetEntitiesInRange(Transform(mech).Coordinates, radius, apcs);

        foreach (var apc in apcs)
        {
            if (apc.Comp.MainBreakerEnabled && apc.Comp.LastExternalState != ApcExternalPowerState.None)
                return true;
        }

        return false;
    }
}
