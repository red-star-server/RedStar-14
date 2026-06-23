// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Mech.Equipment.Components;
using Content.Server.Mech.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Mech.Components;
using Content.Shared.Whitelist;

namespace Content.Server.Mech.Equipment.EntitySystems;

/// <summary>
/// Charges installed mech equipment batteries from the mech battery.
/// </summary>
public sealed class MechChargerSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MechComponent, MechEquipmentChargerComponent>();
        while (query.MoveNext(out var mechUid, out var mech, out var charger))
        {
            if (mech.Broken || mech.Energy <= 0 || charger.ChargeRate <= 0f)
                continue;

            var container = charger.SlotId == mech.EquipmentContainerId
                ? mech.EquipmentContainer
                : null;

            if (container == null)
                continue;

            foreach (var equipment in container.ContainedEntities)
            {
                if (_whitelist.IsWhitelistFail(charger.Whitelist, equipment))
                    continue;

                if (!TryComp<BatteryComponent>(equipment, out var equipmentBattery))
                    continue;

                var chargeNeeded = equipmentBattery.MaxCharge - equipmentBattery.CurrentCharge;
                if (chargeNeeded <= 0f)
                    continue;

                var transfer = MathF.Min(charger.ChargeRate * frameTime, chargeNeeded);
                transfer = MathF.Min(transfer, mech.Energy.Float());
                if (transfer <= 0f)
                    continue;

                var previousEnergy = mech.Energy;
                if (!_mech.TryChangeEnergy(mechUid, -FixedPoint2.New(transfer), mech))
                    continue;

                var transferred = (previousEnergy - mech.Energy).Float();
                if (transferred <= 0f)
                    continue;

                _battery.SetCharge(equipment, equipmentBattery.CurrentCharge + transferred, equipmentBattery);
            }
        }
    }
}
