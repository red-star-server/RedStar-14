// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Server.Power.Generator;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech.Module.Components;
using Content.Shared.Power.Generator;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Consumes stored solid fuel from mech generator modules and recharges their owner mech.
/// </summary>
public sealed class MechFuelGeneratorSystem : EntitySystem
{
    [Dependency] private readonly GeneratorSystem _generator = default!;
    [Dependency] private readonly SharedMechSystem _mech = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MechComponent>();
        while (query.MoveNext(out var mechUid, out var mech))
        {
            var uiDirty = false;
            var canCharge = !mech.Broken &&
                            mech.BatterySlot.ContainedEntity != null &&
                            mech.Energy < mech.MaxEnergy;

            foreach (var module in mech.ModuleContainer.ContainedEntities)
            {
                if (!TryComp<MechGeneratorModuleComponent>(module, out var generator) ||
                    generator.GenerationType != MechGenerationType.FuelGenerator)
                    continue;

                var telemetry = EnsureComp<MechEnergyAccumulatorComponent>(module);
                var previousCurrent = telemetry.Current;
                var previousMax = telemetry.Max;
                telemetry.Current = 0f;

                if (!TryComp<FuelGeneratorComponent>(module, out var fuelGenerator))
                {
                    telemetry.Max = 0f;
                    uiDirty |= Math.Abs(previousCurrent - telemetry.Current) > 0.01f ||
                               Math.Abs(previousMax - telemetry.Max) > 0.01f;
                    continue;
                }

                telemetry.Max = fuelGenerator.TargetPower;

                if (!canCharge || _generator.GetFuel(module) <= 0 || _generator.GetIsClogged(module))
                {
                    uiDirty |= Math.Abs(previousCurrent - telemetry.Current) > 0.01f ||
                               Math.Abs(previousMax - telemetry.Max) > 0.01f;
                    continue;
                }

                var efficiency = 1 / SharedGeneratorSystem.CalcFuelEfficiency(
                    fuelGenerator.TargetPower,
                    fuelGenerator.OptimalPower,
                    fuelGenerator);

                var burn = fuelGenerator.OptimalBurnRate * frameTime * efficiency;
                RaiseLocalEvent(module, new GeneratorUseFuel(burn));

                telemetry.Current = fuelGenerator.TargetPower;
                var accumulator = EnsureComp<MechEnergyAccumulatorComponent>(mechUid);
                accumulator.PendingRechargeRate += fuelGenerator.TargetPower;

                uiDirty |= Math.Abs(previousCurrent - telemetry.Current) > 0.01f ||
                           Math.Abs(previousMax - telemetry.Max) > 0.01f;
            }

            if (uiDirty)
                _mech.UpdateMechUi(mechUid);
        }
    }
}
