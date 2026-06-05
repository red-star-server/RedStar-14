using Content.Shared.Mech.Components;
using Content.Server.PowerCell;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Applies the sum of recharge rates accumulated on a mech during the current tick to the mech's battery
/// by enabling <see cref="BatterySelfRechargerComponent"/> at the computed rate, then clears the accumulator.
/// </summary>
public sealed class MechBatteryRechargeApplySystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MechComponent, MechEnergyAccumulatorComponent>();
        while (query.MoveNext(out var mechUid, out var _, out var acc))
        {
            if (!_powerCell.TryGetBatteryFromSlot(mechUid, out var mechBatteryUid, out _))
            {
                acc.PendingRechargeRate = 0f;
                continue;
            }

            var total = acc.PendingRechargeRate;
            acc.PendingRechargeRate = 0f;

            var self = EnsureComp<BatterySelfRechargerComponent>(mechBatteryUid.Value);
            if (!MathHelper.CloseTo(self.AutoRechargeRate, total))
            {
                self.AutoRechargeRate = total;
                Dirty(mechBatteryUid.Value, self);
            }
        }
    }
}
