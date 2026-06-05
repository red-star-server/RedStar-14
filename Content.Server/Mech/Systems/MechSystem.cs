// SPDX-FileCopyrightText: 2022 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <drsmugleaf@gmail.com>
// SPDX-FileCopyrightText: 2023 Slava0135 <40753025+Slava0135@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 Zoldorf <silvertorch5@gmail.com>
// SPDX-FileCopyrightText: 2023 brainfood1183 <113240905+brainfood1183@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2023 keronshb <54602815+keronshb@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 keronshb <keronshb@live.com>
// SPDX-FileCopyrightText: 2024 Armok <155400926+ARMOKS@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Errant <35878406+Errant-4@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Gorox221 <139872389+Gorox221@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Jake Huxell <JakeHuxell@pm.me>
// SPDX-FileCopyrightText: 2024 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 LordCarve <27449516+LordCarve@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Plykiya <58439124+Plykiya@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Tayrtahn <tayrtahn@gmail.com>
// SPDX-FileCopyrightText: 2024 Verm <32827189+Vermidia@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 nikthechampiongr <32041239+nikthechampiongr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Alert;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Events;
using Content.Shared.Mech.Systems;
using Content.Shared.PowerCell.Components;

namespace Content.Server.Mech.Systems;

public sealed class MechSystem : SharedMechSystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    private const string MechRepairGraph = "MechRepair";
    private const string MechDisassembleGraph = "MechDisassemble";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MechComponent, RepairMechEvent>(OnRepairMechEvent);
        SubscribeLocalEvent<MechComponent, PowerCellChangedEvent>(OnBatteryChanged);
    }

    public override bool TryChangeEnergy(Entity<MechComponent?> ent, FixedPoint2 delta)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (delta > 0)
            return false;

        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var batteryUid, out var battery))
            return false;

        var amount = MathF.Abs(delta.Float());
        if (!_battery.TryUseCharge(batteryUid.Value, amount, battery))
            return false;

        UpdateMechUi(ent.Owner);
        UpdateBatteryAlert(ent);

        return true;
    }

    public override void UpdateBatteryAlert(Entity<MechComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out _, out var cell))
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);
            return;
        }

        var charge = cell.CurrentCharge;
        var maxCharge = cell.MaxCharge;
        var chargePercent = (short) MathF.Round(charge / maxCharge * 10f);

        if (chargePercent == 0 && charge > 0)
            chargePercent = 1;

        _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
        _alerts.ShowAlert(ent.Owner, ent.Comp.BatteryAlert, chargePercent);
    }

    protected override bool HasEnergy(Entity<MechComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false)
            && _powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery)
            && battery.CurrentCharge > 0;
    }

    private void OnDamageChanged(Entity<MechComponent> ent, ref DamageChangedEvent args)
    {
        var integrity = ent.Comp.MaxIntegrity - args.Damageable.TotalDamage;
        SetIntegrity(ent.AsNullable(), integrity);

        // Sync construction graph with mech state.
        var cc = EnsureComp<ConstructionComponent>(ent.Owner);
        if (ent.Comp.Broken)
        {
            if (_construction.ChangeGraph(ent.Owner, null, MechRepairGraph, "start", performActions: false, cc))
                _construction.SetPathfindingTarget(ent.Owner, "repaired", cc);
        }

        UpdateMechUi(ent.Owner);
        UpdateHealthAlert(ent.AsNullable());
    }

    private void OnRepairMechEvent(Entity<MechComponent> ent, ref RepairMechEvent args)
    {
        RepairMech(ent.AsNullable());

        // Restore prototype-declared disassembly graph after successful repair.
        var cc = EnsureComp<ConstructionComponent>(ent.Owner);
        _construction.ChangeGraph(ent.Owner, null, MechDisassembleGraph, "start", performActions: false, cc);
    }

    private void OnBatteryChanged(Entity<MechComponent> ent, ref PowerCellChangedEvent args)
    {
        UpdateMechUi(ent.Owner);
        UpdateBatteryAlert(ent.AsNullable());
    }
}
