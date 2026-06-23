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

using System.Linq;
using Content.Server.Construction; // RS14
using Content.Server.Construction.Components; // RS14
using Content.Shared.Construction.Components; // RS14
using Content.Shared.Construction.Prototypes; // RS14
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared._RedStar.Skills; // RS14
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech.Events; // RS14
using Content.Shared.Mech.Module.Components;
using Content.Shared.Popups;
using Content.Shared.Repairable; // RS14
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Vehicle.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Content.Shared.Wires;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Server.Emp; // Goobstation

namespace Content.Server.Mech.Systems;

/// <inheritdoc/>
public sealed partial class MechSystem : SharedMechSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedSkillsSystem _skills = default!; // RS14
    [Dependency] private readonly MechLockSystem _mechLock = default!; // RS14
    [Dependency] private readonly ConstructionSystem _construction = default!; // RS14
    [Dependency] private readonly SharedAudioSystem _audio = default!; // RS14

    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";
    // RS14-start
    private const float ExosuitDelayModifierWithoutSkill = 1.8f;
    private const float MinimumGasDisplayPressure = 0.0001f;
    private static readonly ProtoId<ConstructionGraphPrototype> MechRepairGraph = "MechRepair";
    private static readonly ProtoId<ConstructionGraphPrototype> MechDisassembleGraph = "MechDisassemble";
    private static readonly ProtoId<SkillPrototype> ExosuitsSkill = "Exosuits";
    // RS14-end

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MechComponent, EntInsertedIntoContainerMessage>(OnInsertBattery);
        SubscribeLocalEvent<MechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MechComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<MechComponent, MechOpenUiEvent>(OnOpenUi);
        SubscribeLocalEvent<MechComponent, RemoveBatteryEvent>(OnRemoveBattery);
        SubscribeLocalEvent<MechComponent, MechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<MechComponent, MechExitEvent>(OnMechExit);
        SubscribeLocalEvent<MechComponent, EmpAttemptEvent>(OnEmpAttempt); // RS14
        SubscribeLocalEvent<MechComponent, EmpPulseEvent>(OnEmpPulse); // Goobstation


        SubscribeLocalEvent<MechComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MechComponent, RepairAttemptEvent>(OnRepairAttempt); // RS14
        SubscribeLocalEvent<MechComponent, RepairMechEvent>(OnRepairMechEvent); // RS14
        SubscribeLocalEvent<MechComponent, MechEquipmentRemoveMessage>(OnRemoveEquipmentMessage);
        SubscribeLocalEvent<MechComponent, MechModuleRemoveMessage>(OnRemoveModuleMessage); // RS14
        SubscribeLocalEvent<MechComponent, MechBrokenSoundEvent>(OnMechBrokenSound); // RS14
        SubscribeLocalEvent<MechComponent, MechEntrySuccessSoundEvent>(OnMechEntrySuccessSound); // RS14

        SubscribeLocalEvent<MechComponent, VehicleCanRunEvent>(OnMechCanMoveEvent); // RS14
        SubscribeLocalEvent<MechComponent, AttemptChangePanelEvent>(OnAttemptChangePanel); // RS14


        #region Equipment UI message relays
        SubscribeLocalEvent<MechComponent, MechGrabberEjectMessage>(ReceiveEquipmentUiMesssages);
        SubscribeLocalEvent<MechComponent, MechSoundboardPlayMessage>(ReceiveEquipmentUiMesssages);
        SubscribeLocalEvent<MechComponent, MechGeneratorEjectFuelMessage>(ReceiveEquipmentUiMesssages); // RS14
        #endregion
    }

    private void OnMechCanMoveEvent(EntityUid uid, MechComponent component, ref VehicleCanRunEvent args) // RS14
    {
        if (component.Broken || component.Integrity <= 0 || component.Energy <= 0)
            args.CanRun = false; // RS14

        if (Vehicle.GetOperatorOrNull(uid) is { } operatorUid &&
            !_mechLock.CheckAccess(uid, operatorUid))
        {
            args.CanRun = false;
        }
    }

    // RS14-start
    private void OnMechBrokenSound(EntityUid uid, MechComponent component, ref MechBrokenSoundEvent args)
    {
        _audio.PlayPvs(args.Sound, uid);
    }

    private void OnMechEntrySuccessSound(EntityUid uid, MechComponent component, ref MechEntrySuccessSoundEvent args)
    {
        _audio.PlayPvs(args.Sound, uid);
    }

    private static void OnEmpAttempt(EntityUid uid, MechComponent component, ref EmpAttemptEvent args)
    {
        // Mech batteries handle EMP through the mech pulse path.
        args.Cancel();
    }
    // RS14-end

    private void OnInteractUsing(EntityUid uid, MechComponent component, InteractUsingEvent args)
    {
        if (!_mechLock.CheckAccessWithFeedback(uid, args.User))
            return;

        if (TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
            return;

        if (component.BatterySlot.ContainedEntity == null && TryComp<BatteryComponent>(args.Used, out var battery))
        {
            InsertBattery(uid, args.Used, component, battery);
            Vehicle.RefreshCanRun(uid); // RS14
            return;
        }

        if (_toolSystem.HasQuality(args.Used, PryingQuality) && component.BatterySlot.ContainedEntity != null)
        {
            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.BatteryRemovalDelay,
                new RemoveBatteryEvent(), uid, target: uid, used: args.Target)
            {
                BreakOnMove = true
            };

            _doAfter.TryStartDoAfter(doAfterEventArgs);
        }
    }

    private void OnInsertBattery(EntityUid uid, MechComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container == component.PilotSlot)
        {
            SetBatterySlotLocked(uid, component, true);
            return;
        }

        if (args.Container != component.BatterySlot || !TryComp<BatteryComponent>(args.Entity, out var battery))
            return;

        component.Energy = battery.CurrentCharge;
        component.MaxEnergy = battery.MaxCharge;

        Dirty(uid, component);
        UpdateBatteryAlert(uid, component); // RS14
        Vehicle.RefreshCanRun(uid); // RS14
    }

    private void OnRemoveBattery(EntityUid uid, MechComponent component, RemoveBatteryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!_mechLock.CheckAccessWithFeedback(uid, args.User))
            return;

        RemoveBattery(uid, component);
        Vehicle.RefreshCanRun(uid); // RS14

        args.Handled = true;
    }

    // RS14-start
    private void OnRepairAttempt(EntityUid uid, MechComponent component, ref RepairAttemptEvent args)
    {
        if (!component.Broken)
            return;

        args.Cancelled = true;

        SetMechConstructionGraph(uid, component, MechRepairGraph, "repaired", args.User);
    }

    private void SetMechConstructionGraph(EntityUid uid, MechComponent component, ProtoId<ConstructionGraphPrototype> graph, string? target, EntityUid? user = null)
    {
        var construction = EnsureComp<ConstructionComponent>(uid);
        if (_construction.ChangeGraph(uid, user, graph, "start", performActions: false, construction) && target != null)
            _construction.SetPathfindingTarget(uid, target, construction);
    }

    private void OnRepairMechEvent(EntityUid uid, MechComponent component, RepairMechEvent args)
    {
        SetIntegrity(uid, component.MaxIntegrity, component);
        if (HasComp<PartDisassemblyComponent>(uid))
            SetMechConstructionGraph(uid, component, MechDisassembleGraph, "disassembled");
        else
            RemComp<ConstructionComponent>(uid);
        Vehicle.RefreshCanRun(uid);
    }
    // RS14-end

    private void OnMapInit(EntityUid uid, MechComponent component, MapInitEvent args)
    {
        var xform = Transform(uid);
        // TODO: this should use containerfill?
        foreach (var equipment in component.StartingEquipment)
        {
            var ent = Spawn(equipment, xform.Coordinates);
            InsertEquipment(uid, ent, component);
        }

        // RS14-start
        foreach (var module in component.StartingModules)
        {
            var ent = Spawn(module, xform.Coordinates);
            InsertEquipment(uid, ent, component);
        }
        // RS14-end

        // TODO: this should just be damage and battery
        component.Integrity = component.MaxIntegrity;
        component.Energy = component.MaxEnergy;

        Vehicle.RefreshCanRun(uid); // RS14
        Dirty(uid, component);
    }

    private void OnRemoveEquipmentMessage(EntityUid uid, MechComponent component, MechEquipmentRemoveMessage args)
    {
        if (!_mechLock.CheckAccessWithFeedback(uid, args.Actor))
            return;

        var equip = GetEntity(args.Equipment);

        if (!Exists(equip) || Deleted(equip))
            return;

        if (!component.EquipmentContainer.ContainedEntities.Contains(equip))
            return;

        RemoveEquipment(uid, equip, component);
    }

    // RS14-start
    private void OnRemoveModuleMessage(EntityUid uid, MechComponent component, MechModuleRemoveMessage args)
    {
        if (!_mechLock.CheckAccessWithFeedback(uid, args.Actor))
            return;

        var module = GetEntity(args.Module);

        if (!Exists(module) || Deleted(module))
            return;

        if (!component.ModuleContainer.ContainedEntities.Contains(module))
            return;

        RemoveEquipment(uid, module, component);
    }
    // RS14-end

    private void OnOpenUi(EntityUid uid, MechComponent component, MechOpenUiEvent args)
    {
        args.Handled = true;
        if (!_mechLock.CheckAccessWithFeedback(uid, args.Performer))
            return;

        ToggleMechUi(uid, component);
    }

    private void OnAttemptChangePanel(EntityUid uid, MechComponent component, ref AttemptChangePanelEvent args)
    {
        if (args.User == null || _mechLock.CheckAccessWithFeedback(uid, args.User.Value))
            return;

        args.Cancelled = true;
    }

    private void OnAlternativeVerb(EntityUid uid, MechComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.Broken)
            return;

        if (CanInsert(uid, args.User, component))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-enter"),
                Act = () =>
                {
                    if (!_mechLock.CheckAccessWithFeedback(uid, args.User))
                        return;

                    // RS14-start
                    var delay = component.EntryDelay;
                    if (!_skills.HasSkill(args.User, ExosuitsSkill))
                        delay *= ExosuitDelayModifierWithoutSkill;
                    // RS14-end

                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, delay, new MechEntryEvent(), uid, target: uid) // RS14
                    {
                        BreakOnMove = true,
                        MultiplyDelay = false, // Goobstation
                    };

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            var openUiVerb = new AlternativeVerb //can't hijack someone else's mech
            {
                Act = () =>
                {
                    if (!_mechLock.CheckAccessWithFeedback(uid, args.User))
                        return;

                    ToggleMechUi(uid, component, args.User);
                },
                Text = Loc.GetString("mech-ui-open-verb")
            };
            args.Verbs.Add(enterVerb);
            args.Verbs.Add(openUiVerb);
        }
        else if (Vehicle.HasOperator(uid)) // RS14
        {
            var operatorUid = Vehicle.GetOperatorOrNull(uid); // RS14
            var ejectVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-exit"),
                Priority = 1, // Promote to top to make ejecting the ALT-click action
                Act = () =>
                {
                    if (args.User == uid || args.User == operatorUid) // RS14
                    {
                        TryEject(uid, component);
                        return;
                    }

                    if (!_mechLock.CheckAccessWithFeedback(uid, args.User))
                        return;

                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.ExitDelay, new MechExitEvent(), uid, target: uid)
                    {
                        BreakOnMove = true,
                    };
                    _popup.PopupEntity(Loc.GetString("mech-eject-pilot-alert", ("item", uid), ("user", args.User)), uid, PopupType.Large);

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    private void OnMechEntry(EntityUid uid, MechComponent component, MechEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        // RS14-start
        if (!Vehicle.CanOperate(uid, args.User)
            || _whitelistSystem.IsWhitelistFail(component.PilotWhitelist, args.User)
            || _whitelistSystem.IsBlacklistPass(component.PilotBlacklist, args.User)) // Goobstation Change
        // RS14-end
        {
            _popup.PopupEntity(Loc.GetString("mech-no-enter", ("item", uid)), args.User);
            return;
        }

        if (!_mechLock.CheckAccessWithFeedback(uid, args.User))
            return;

        // RS14-start
        if (!TryInsert(uid, args.User, component))
        {
            _popup.PopupEntity(Loc.GetString("mech-no-enter", ("item", uid)), args.User);
            return;
        }
        // RS14-end

        args.Handled = true;
    }

    private void OnMechExit(EntityUid uid, MechComponent component, MechExitEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (Vehicle.GetOperatorOrNull(uid) is { } operatorUid &&
            args.User != operatorUid &&
            !_mechLock.CheckAccessWithFeedback(uid, args.User))
        {
            return;
        }

        if (!TryEject(uid, component)) // RS14
            return;

        args.Handled = true;
    }
    //goobstation
    private void OnEmpPulse(EntityUid uid, MechComponent component, EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;
        component.Energy -= args.EnergyConsumption;
        if (component.Energy < 0)
            component.Energy = 0;
        Dirty(uid, component);
        UpdateUserInterface(uid, component);
        UpdateBatteryAlert(uid, component); // RS14
        Vehicle.RefreshCanRun(uid); // RS14
    }

    private void OnDamageChanged(EntityUid uid, MechComponent component, DamageChangedEvent args)
    {
        var integrity = component.MaxIntegrity - args.Damageable.TotalDamage;
        SetIntegrity(uid, integrity, component);

        // RS14-start
        if (component.Broken)
            SetMechConstructionGraph(uid, component, MechRepairGraph, "repaired");
        else if (HasComp<PartDisassemblyComponent>(uid))
            SetMechConstructionGraph(uid, component, MechDisassembleGraph, "disassembled");
        // RS14-end

        if (args.DamageIncreased &&
            args.DamageDelta != null &&
            Vehicle.GetOperatorOrNull(uid) is { } operatorUid) // RS14
        {
            var damage = args.DamageDelta * component.MechToPilotDamageMultiplier;
            _damageable.TryChangeDamage(operatorUid, damage); // RS14
        }
    }

    private void ToggleMechUi(EntityUid uid, MechComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return;
        user ??= Vehicle.GetOperatorOrNull(uid); // RS14
        if (user == null)
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _ui.TryToggleUi(uid, MechUiKey.Key, actor.PlayerSession);
        UpdateUserInterface(uid, component);
    }

    private void ReceiveEquipmentUiMesssages<T>(EntityUid uid, MechComponent component, T args) where T : MechEquipmentUiMessage
    {
        // RS14-start
        if (!_mechLock.CheckAccessWithFeedback(uid, args.Actor))
            return;
        // RS14-end

        var ev = new MechEquipmentUiMessageRelayEvent(args);
        var allEquipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
        // RS14-start
        allEquipment.AddRange(component.ModuleContainer.ContainedEntities);
        // RS14-end
        var argEquip = GetEntity(args.Equipment);

        foreach (var equipment in allEquipment)
        {
            if (argEquip == equipment)
                RaiseLocalEvent(equipment, ev);
        }
    }

    public override void UpdateUserInterface(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        base.UpdateUserInterface(uid, component);

        var ev = new MechEquipmentUiStateReadyEvent();
        foreach (var ent in component.EquipmentContainer.ContainedEntities)
        {
            RaiseLocalEvent(ent, ev);
        }
        // RS14-start
        foreach (var ent in component.ModuleContainer.ContainedEntities)
        {
            RaiseLocalEvent(ent, ev);
        }
        // RS14-end

        var state = new MechBoundUiState
        {
            EquipmentStates = ev.States
        };

        if (TryComp<MechLockComponent>(uid, out var lockComp))
        {
            state.HasLock = true;
            state.IsLocked = lockComp.IsLocked;
            state.DnaLockRegistered = lockComp.DnaLockRegistered;
            state.DnaLockActive = lockComp.DnaLockActive;
            state.DnaLockOwner = lockComp.OwnerDna;
            state.CardLockRegistered = lockComp.CardLockRegistered;
            state.CardLockActive = lockComp.CardLockActive;
            state.CardLockOwner = lockComp.OwnerJobTitle;
        }

        // RS14-start
        foreach (var equipment in component.EquipmentContainer.ContainedEntities)
        {
            state.Equipment.Add(GetNetEntity(equipment));
        }

        foreach (var module in component.ModuleContainer.ContainedEntities)
        {
            state.Modules.Add(GetNetEntity(module));
        }

        state.PilotPresent = component.PilotSlot.ContainedEntity != null;
        state.Integrity = component.Integrity.Float();
        state.MaxIntegrity = component.MaxIntegrity.Float();
        state.Energy = component.Energy.Float();
        state.MaxEnergy = component.MaxEnergy.Float();
        state.EquipmentUsed = component.EquipmentContainer.ContainedEntities.Count;
        state.MaxEquipmentAmount = component.MaxEquipmentAmount;
        state.ModuleSpaceMax = component.MaxModuleAmount;
        state.IsBroken = component.Broken;
        state.CanAirtight = component.CanAirtight;
        state.IsAirtight = component.Airtight;
        state.CabinPurgeAvailable = true;

        foreach (var module in component.ModuleContainer.ContainedEntities)
        {
            if (TryComp<MechModuleComponent>(module, out var moduleComp))
                state.ModuleSpaceUsed += moduleComp.Size;
        }

        if (TryComp<MechCabinAirComponent>(uid, out var cabin))
        {
            state.CabinPressureLevel = cabin.Air.Pressure;
            state.CabinTemperature = cabin.Air.Temperature;
        }

        foreach (var module in component.ModuleContainer.ContainedEntities)
        {
            if (TryComp<MechFanModuleComponent>(module, out var fan))
            {
                state.HasFanModule = true;
                state.FanActive = fan.IsActive;
                state.FanState = fan.State;
                state.FilterEnabled = fan.FilterEnabled;
            }

            if (HasComp<MechAirTankModuleComponent>(module) &&
                TryComp<GasTankComponent>(module, out var tank))
            {
                state.HasGasModule = true;
                state.TankPressure = tank.Air.Pressure;

                state.GasAmountLiters = tank.Air.Pressure > MinimumGasDisplayPressure
                    ? tank.Air.TotalMoles * Atmospherics.R * tank.Air.Temperature / tank.Air.Pressure
                    : 0f;
            }
        }

        if (TryComp<MechCabinPurgeComponent>(uid, out var purge))
            state.CabinPurgeAvailable = purge.CooldownRemaining <= 0;
        // RS14-end

        _ui.SetUiState(uid, MechUiKey.Key, state);
    }

    public override void BreakMech(EntityUid uid, MechComponent? component = null)
    {
        base.BreakMech(uid, component);

        _ui.CloseUi(uid, MechUiKey.Key);
        Vehicle.RefreshCanRun(uid); // RS14
    }

    public override bool TryChangeEnergy(EntityUid uid, FixedPoint2 delta, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!base.TryChangeEnergy(uid, delta, component))
            return false;

        var battery = component.BatterySlot.ContainedEntity;
        if (battery == null)
            return false;

        if (!TryComp<BatteryComponent>(battery, out var batteryComp))
            return false;

        _battery.SetCharge(battery.Value, batteryComp.CurrentCharge + delta.Float(), batteryComp);
        if (Math.Abs(batteryComp.CurrentCharge - component.Energy.Float()) > 0.01f)
        {
            component.Energy = batteryComp.CurrentCharge;
            Dirty(uid, component);
        }
        UpdateBatteryAlert(uid, component); // RS14
        Vehicle.RefreshCanRun(uid); // RS14
        return true;
    }

    public void InsertBattery(EntityUid uid, EntityUid toInsert, MechComponent? component = null, BatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!Resolve(toInsert, ref battery, false))
            return;

        _container.Insert(toInsert, component.BatterySlot);
        component.Energy = battery.CurrentCharge;
        component.MaxEnergy = battery.MaxCharge;

        Vehicle.RefreshCanRun(uid); // RS14

        Dirty(uid, component);
        UpdateUserInterface(uid, component);
        UpdateBatteryAlert(uid, component); // RS14
    }

    public void RemoveBattery(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _container.EmptyContainer(component.BatterySlot);
        component.Energy = 0;
        component.MaxEnergy = 0;

        Vehicle.RefreshCanRun(uid); // RS14

        Dirty(uid, component);
        UpdateUserInterface(uid, component);
        UpdateBatteryAlert(uid, component); // RS14
    }

}
