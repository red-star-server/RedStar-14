// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech.Module.Components;
using Content.Shared.Vehicle.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Handles atmospheric systems for mechs, including cabin air, fans, and tank modules.
/// </summary>
public sealed class MechAtmosphereSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly MechLockSystem _mechLock = default!;
    [Dependency] private readonly SharedMechSystem _mech = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private const float MinExternalPressure = 0.05f;
    private const float PressureTolerance = 0.1f;

    public override void Initialize()
    {
        SubscribeLocalEvent<MechComponent, MechAirtightMessage>(OnAirtightMessage);
        SubscribeLocalEvent<MechComponent, MechCabinAirMessage>(OnCabinPurgeMessage);
        SubscribeLocalEvent<MechComponent, MechFanToggleMessage>(OnFanToggleMessage);
        SubscribeLocalEvent<MechComponent, MechFilterToggleMessage>(OnFilterToggleMessage);

        SubscribeLocalEvent<MechPilotComponent, InhaleLocationEvent>(OnPilotInhale); // RS14
        SubscribeLocalEvent<MechPilotComponent, ExhaleLocationEvent>(OnPilotExhale); // RS14
        SubscribeLocalEvent<MechPilotComponent, AtmosExposedGetAirEvent>(OnPilotExpose); // RS14
        SubscribeLocalEvent<VehicleOperatorComponent, InhaleLocationEvent>(OnInhale);
        SubscribeLocalEvent<VehicleOperatorComponent, ExhaleLocationEvent>(OnExhale);
        SubscribeLocalEvent<VehicleOperatorComponent, AtmosExposedGetAirEvent>(OnExpose);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MechComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var uiDirty = false;

            uiDirty |= UpdatePurgeCooldown(uid, frameTime);
            uiDirty |= UpdateFanModule((uid, component), frameTime);
            uiDirty |= UpdateCabinPressure((uid, component));

            if (uiDirty && _ui.IsUiOpen(uid, MechUiKey.Key))
                _mech.UpdateMechUi(uid);
        }
    }

    public bool TryGetGasModuleAir(Entity<MechComponent> ent, out GasMixture? air)
    {
        air = null;
        foreach (var moduleEnt in ent.Comp.ModuleContainer.ContainedEntities)
        {
            if (!HasComp<MechAirTankModuleComponent>(moduleEnt))
                continue;

            if (!TryComp<GasTankComponent>(moduleEnt, out var tank))
                continue;

            air = tank.Air;
            return true;
        }

        return false;
    }

    private bool UpdatePurgeCooldown(EntityUid uid, float frameTime)
    {
        if (!TryComp<MechCabinPurgeComponent>(uid, out var purge))
            return false;

        if (purge.CooldownRemaining <= 0)
            return false;

        purge.CooldownRemaining -= frameTime;
        Dirty(uid, purge);

        if (purge.CooldownRemaining > 0)
            return false;

        RemCompDeferred<MechCabinPurgeComponent>(uid);
        return true;
    }

    private bool UpdateCabinPressure(Entity<MechComponent> ent)
    {
        if (!TryComp<MechCabinAirComponent>(ent.Owner, out var cabin))
            return false;

        var purgingActive = TryComp<MechCabinPurgeComponent>(ent.Owner, out var purgeComp) &&
                            purgeComp.CooldownRemaining > 0;
        if (purgingActive || !TryGetGasModuleAir(ent, out var tankAir) || tankAir == null)
            return false;

        return _atmosphere.PumpGasTo(tankAir, cabin.Air, cabin.TargetPressure);
    }

    private void OnAirtightMessage(Entity<MechComponent> ent, ref MechAirtightMessage args)
    {
        if (!_mechLock.CheckAccessWithFeedback(ent.Owner, args.Actor))
            return;

        ent.Comp.Airtight = ent.Comp.CanAirtight && args.IsAirtight;
        Dirty(ent);
        _mech.UpdateMechUi(ent.Owner);
    }

    private void OnCabinPurgeMessage(Entity<MechComponent> ent, ref MechCabinAirMessage args)
    {
        if (!_mechLock.CheckAccessWithFeedback(ent.Owner, args.Actor))
            return;

        if (!TryComp<MechCabinAirComponent>(ent.Owner, out var cabin))
            return;

        if (TryComp<MechCabinPurgeComponent>(ent.Owner, out var existingPurge) &&
            existingPurge.CooldownRemaining > 0)
        {
            return;
        }

        var environment = _atmosphere.GetContainingMixture(ent.Owner, false, true);
        if (environment != null)
        {
            var removed = cabin.Air.RemoveRatio(1f);
            _atmosphere.Merge(environment, removed);
        }
        else
        {
            cabin.Air.Clear();
        }

        Dirty(ent.Owner, cabin);

        var purge = EnsureComp<MechCabinPurgeComponent>(ent.Owner);
        purge.CooldownRemaining = purge.CooldownDuration;
        Dirty(ent.Owner, purge);

        _mech.UpdateMechUi(ent.Owner);
    }

    private void OnInhale(EntityUid uid, VehicleOperatorComponent component, ref InhaleLocationEvent args)
    {
        if (!TryGetOperatedMech(component, out var ent))
            return;

        SetInhaleGas(ent, ref args);
    }

    private void OnExhale(EntityUid uid, VehicleOperatorComponent component, ref ExhaleLocationEvent args)
    {
        if (!TryGetOperatedMech(component, out var ent))
            return;

        args.Gas = GetBreathMixture(ent);
    }

    private void OnExpose(EntityUid uid, VehicleOperatorComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled || !TryGetOperatedMech(component, out var ent))
            return;

        args.Gas = GetBreathMixture(ent, args.Excite);
        args.Handled = true;
    }

    private void OnPilotInhale(Entity<MechPilotComponent> ent, ref InhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mechComp))
            return;

        SetInhaleGas((ent.Comp.Mech, mechComp), ref args);
    }

    private void OnPilotExhale(Entity<MechPilotComponent> ent, ref ExhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mechComp))
            return;

        args.Gas = GetBreathMixture((ent.Comp.Mech, mechComp));
    }

    private void OnPilotExpose(Entity<MechPilotComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mechComp))
            return;

        args.Gas = GetBreathMixture((ent.Comp.Mech, mechComp), args.Excite);
        args.Handled = true;
    }

    private void SetInhaleGas(Entity<MechComponent> ent, ref InhaleLocationEvent args)
    {
        if (ent.Comp.Airtight && TryComp<MechCabinAirComponent>(ent.Owner, out var cabin))
        {
            var breath = new GasMixture(args.Respirator.BreathVolume)
            {
                Temperature = cabin.Air.Temperature
            };

            _atmosphere.PumpGasTo(cabin.Air, breath, cabin.RegulatorPressure);
            args.Gas = breath;
            return;
        }

        args.Gas = _atmosphere.GetContainingMixture(ent.Owner, excite: true);
    }

    private GasMixture? GetBreathMixture(Entity<MechComponent> ent, bool excite = true)
    {
        if (ent.Comp.Airtight && TryComp<MechCabinAirComponent>(ent.Owner, out var cabin))
            return cabin.Air;

        return _atmosphere.GetContainingMixture(ent.Owner, excite: excite);
    }

    private bool TryGetOperatedMech(VehicleOperatorComponent component, out Entity<MechComponent> ent)
    {
        ent = default;
        if (component.Vehicle is not { } mech ||
            !TryComp<MechComponent>(mech, out var mechComp))
        {
            return false;
        }

        ent = (mech, mechComp);
        return true;
    }

    private bool UpdateFanModule(Entity<MechComponent> ent, float frameTime)
    {
        var fanModule = GetFanModule(ent);
        if (fanModule == null || !fanModule.Value.Comp.IsActive)
        {
            if (fanModule != null)
                SetFanState(fanModule.Value, MechFanState.Off);

            return false;
        }

        var (tankComp, tankAir) = GetGasTank(ent.Comp);
        if (tankAir == null || tankComp == null)
        {
            SetFanState(fanModule.Value, MechFanState.Off);
            return false;
        }

        return ProcessFanOperation(ent, fanModule.Value, tankComp, tankAir, frameTime);
    }

    private (GasTankComponent? Tank, GasMixture? Air) GetGasTank(MechComponent mechComp)
    {
        foreach (var moduleEnt in mechComp.ModuleContainer.ContainedEntities)
        {
            if (HasComp<MechAirTankModuleComponent>(moduleEnt) &&
                TryComp<GasTankComponent>(moduleEnt, out var tank))
            {
                return (tank, tank.Air);
            }
        }

        return (null, null);
    }

    private bool ProcessFanOperation(
        Entity<MechComponent> ent,
        Entity<MechFanModuleComponent> fanModule,
        GasTankComponent tankComp,
        GasMixture tankAir,
        float frameTime)
    {
        var external = _atmosphere.GetContainingMixture(ent.Owner);
        if (external == null ||
            external.Pressure <= MinExternalPressure ||
            tankAir.Pressure >= tankComp.MaxOutputPressure - PressureTolerance)
        {
            SetFanState(fanModule, MechFanState.Idle);
            return false;
        }

        if (!_mech.TryChangeEnergy(ent.AsNullable(), -fanModule.Comp.EnergyConsumption * frameTime))
        {
            SetFanState(fanModule, MechFanState.Off);
            return false;
        }

        var success = ProcessFilteredTransfer(external, tankAir, fanModule.Comp, frameTime);

        SetFanState(fanModule, success ? MechFanState.On : MechFanState.Idle);
        return success;
    }

    private bool ProcessFilteredTransfer(
        GasMixture external,
        GasMixture tankAir,
        MechFanModuleComponent fanModule,
        float frameTime)
    {
        var transferVolume = fanModule.GasProcessingRate * frameTime;
        if (transferVolume <= 0)
            return false;

        var removed = external.RemoveVolume(transferVolume);
        if (removed.TotalMoles <= 0)
            return false;

        if (fanModule is { FilterEnabled: true, FilterGases.Count: > 0 })
        {
            var filtered = new GasMixture(removed.Volume) { Temperature = removed.Temperature };
            _atmosphere.ScrubInto(removed, filtered, fanModule.FilterGases);
            _atmosphere.Merge(external, filtered);
        }

        _atmosphere.Merge(tankAir, removed);
        return true;
    }

    private void SetFanState(Entity<MechFanModuleComponent> fanModule, MechFanState state)
    {
        if (fanModule.Comp.State == state)
            return;

        fanModule.Comp.State = state;
        Dirty(fanModule);
    }

    private void OnFanToggleMessage(Entity<MechComponent> ent, ref MechFanToggleMessage args)
    {
        if (!_mechLock.CheckAccessWithFeedback(ent.Owner, args.Actor))
            return;

        var fanModule = GetFanModule(ent);
        if (fanModule == null)
            return;

        fanModule.Value.Comp.IsActive = args.IsActive;
        fanModule.Value.Comp.State = args.IsActive ? MechFanState.On : MechFanState.Off;
        Dirty(fanModule.Value);
        _mech.UpdateMechUi(ent.Owner);
    }

    private void OnFilterToggleMessage(Entity<MechComponent> ent, ref MechFilterToggleMessage args)
    {
        if (!_mechLock.CheckAccessWithFeedback(ent.Owner, args.Actor))
            return;

        var fanModule = GetFanModule(ent);
        if (fanModule == null)
            return;

        fanModule.Value.Comp.FilterEnabled = args.Enabled;
        Dirty(fanModule.Value);
        _mech.UpdateMechUi(ent.Owner);
    }

    private Entity<MechFanModuleComponent>? GetFanModule(Entity<MechComponent> ent)
    {
        foreach (var moduleEnt in ent.Comp.ModuleContainer.ContainedEntities)
        {
            if (TryComp<MechFanModuleComponent>(moduleEnt, out var fanModule))
                return (moduleEnt, fanModule);
        }

        return null;
    }
}
