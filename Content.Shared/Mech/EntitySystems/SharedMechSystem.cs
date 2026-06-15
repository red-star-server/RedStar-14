// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Rane <60792108+Elijahrane@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 brainfood1183 <113240905+brainfood1183@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2023 keronshb <keronshb@live.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 themias <89101928+themias@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Arendian <137322659+Arendian@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 NULL882 <gost6865@yandex.ru>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Plykiya <58439124+Plykiya@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 ScyronX <166930367+ScyronX@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Tayrtahn <tayrtahn@gmail.com>
// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 nikthechampiongr <32041239+nikthechampiongr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Ilya246 <57039557+Ilya246@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Ilya246 <ilyukarno@gmail.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.CCVar; // Goob Edit
using Content.Goobstation.Common.Mech; // Goobstation
using Content.Shared._vg.TileMovement; // Goobstation
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Mech.Module.Components;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Tools.Components;
using Content.Shared.UserInterface;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

using Content.Shared.Emag.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Configuration;

namespace Content.Shared.Mech.EntitySystems;

/// <summary>
/// Handles all of the interactions, UI handling, and items shennanigans for <see cref="MechComponent"/>
/// </summary>
public abstract partial class SharedMechSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!; // RS14
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!; // RS14
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly VehicleSystem Vehicle = default!; // RS14
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly EmagSystem _emag = default!; // Goobstation change
    [Dependency] private readonly SharedHandsSystem _hands = default!; // Goobstation Change
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!; // Goobstation Change
    [Dependency] private readonly IConfigurationManager _config = default!; // Goobstation Change

    // Goobstation: Local variable for checking if mech guns can be used out of them.
    private bool _canUseMechGunOutside;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechComponent, MechToggleEquipmentEvent>(OnToggleEquipmentAction);
        SubscribeLocalEvent<MechComponent, MechEjectPilotEvent>(OnEjectPilotEvent);
        SubscribeLocalEvent<MechComponent, UserActivateInWorldEvent>(RelayInteractionEvent);
        SubscribeLocalEvent<MechComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MechComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<MechComponent, EntityStorageIntoContainerAttemptEvent>(OnEntityStorageDump); // RS14
        SubscribeLocalEvent<MechComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<MechComponent, CanDropTargetEvent>(OnCanDragDrop);
        SubscribeLocalEvent<MechComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<MechComponent, VehicleOperatorSetEvent>(OnOperatorSet); // RS14
        SubscribeLocalEvent<MechComponent, EntRemovedFromContainerMessage>(OnContainerChanged); // RS14

        // RS14-start
        SubscribeLocalEvent<VehicleOperatorComponent, GetMeleeWeaponEvent>(OnGetMeleeWeapon);
        SubscribeLocalEvent<VehicleOperatorComponent, CanAttackFromContainerEvent>(OnCanAttackFromContainer);
        SubscribeLocalEvent<VehicleOperatorComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<MechPilotComponent, CanAttackFromContainerEvent>(OnPilotCanAttackFromContainer);
        SubscribeLocalEvent<MechPilotComponent, GetMeleeAttackEntityEvent>(OnPilotGetMeleeAttackEntity);
        SubscribeLocalEvent<MechPilotComponent, GetMeleeWeaponEvent>(OnPilotGetMeleeWeapon);
        SubscribeLocalEvent<MechPilotComponent, GetActiveWeaponEvent>(OnPilotGetActiveWeapon);
        SubscribeLocalEvent<MechPilotComponent, GetUsedEntityEvent>(OnPilotGetUsedEntity);
        SubscribeLocalEvent<MechPilotComponent, AccessibleOverrideEvent>(OnPilotAccessible);
        SubscribeLocalEvent<MechPilotComponent, GetShootingEntityEvent>(OnPilotGetShootingEntity);
        SubscribeLocalEvent<MechPilotComponent, ToolUserAttemptUseEvent>(OnPilotToolUseAttempt);
        // RS14-end
        SubscribeLocalEvent<MechEquipmentComponent, ShotAttemptedEvent>(OnShotAttempted); // Goobstation
        // RS14-start
        SubscribeLocalEvent<MechEquipmentComponent, AttemptMeleeEvent>(OnMechEquipmentMeleeAttempt);
        SubscribeLocalEvent<MechEquipmentComponent, GettingUsedAttemptEvent>(OnMechEquipmentGettingUsedAttempt);
        SubscribeLocalEvent<MechEquipmentComponent, ActivatableUIOpenAttemptEvent>(OnMechEquipmentUiOpenAttempt);
        // RS14-end
        Subs.CVar(_config, GoobCVars.MechGunOutsideMech, value => _canUseMechGunOutside = value, true); // Goobstation
        SubscribeAllEvent<RequestMechEquipmentSelectEvent>(OnEquipmentSelectRequest); // RS14

        InitializeRelay();
    }

    private void OnToggleEquipmentAction(EntityUid uid, MechComponent component, MechToggleEquipmentEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        // RS14-start
        var ev = new MechOpenEquipmentRadialEvent();
        RaiseLocalEvent(uid, ref ev);
        // RS14-end
    }

    private void OnEjectPilotEvent(EntityUid uid, MechComponent component, MechEjectPilotEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        TryEject(uid, component);
    }

    private void RelayInteractionEvent(EntityUid uid, MechComponent component, UserActivateInWorldEvent args)
    {
        if (!Vehicle.HasOperator(uid)) // RS14
            return;

        // TODO why is this being blocked?
        if (!_timing.IsFirstTimePredicted)
            return;

        if (component.CurrentSelectedEquipment != null)
        {
            RaiseLocalEvent(component.CurrentSelectedEquipment.Value, args);
        }
    }

    private void OnStartup(EntityUid uid, MechComponent component, ComponentStartup args)
    {
        component.PilotSlot = _container.EnsureContainer<ContainerSlot>(uid, component.PilotSlotId);
        component.EquipmentContainer = _container.EnsureContainer<Container>(uid, component.EquipmentContainerId);
        // RS14-start
        component.ModuleContainer = _container.EnsureContainer<Container>(uid, component.ModuleContainerId);
        // RS14-end
        component.BatterySlot = _container.EnsureContainer<ContainerSlot>(uid, component.BatterySlotId);
        UpdateAppearance(uid, component);
    }

    private void OnDestruction(EntityUid uid, MechComponent component, DestructionEventArgs args)
    {
        BreakMech(uid, component);
    }

    // RS14-start
    private void OnEntityStorageDump(Entity<MechComponent> ent, ref EntityStorageIntoContainerAttemptEvent args)
    {
        args.Cancelled = true;
    }
    // RS14-end

    private void SetupUser(EntityUid mech, EntityUid pilot, MechComponent? component = null)
    {
        if (!Resolve(mech, ref component))
            return;

        // RS14-start
        var mechPilot = EnsureComp<MechPilotComponent>(pilot);
        mechPilot.Mech = mech;
        Dirty(pilot, mechPilot);
        // RS14-end

        if (HasComp<TileMovementComponent>(pilot)) // Goob change - Prevent mech jank.
            EnsureComp<TileMovementComponent>(mech);

        // Warning: this bypasses most normal interaction blocking components on the user, like drone laws and the like.
        var irelay = EnsureComp<InteractionRelayComponent>(pilot);

        _interaction.SetRelay(pilot, mech, irelay);

        if (_net.IsClient)
        {
            UpdateHands(pilot, mech, true); // Goobstation
            return;
        }

        // RS14-start
        var alertRelay = EnsureComp<AlertsDisplayRelayComponent>(pilot);
        alertRelay.Source = mech;
        Dirty(pilot, alertRelay);
        // RS14-end

        _actions.AddAction(pilot, ref component.MechCycleActionEntity, component.MechCycleAction, mech);
        _actions.AddAction(pilot, ref component.MechUiActionEntity, component.MechUiAction, mech);
        _actions.AddAction(pilot, ref component.MechEjectActionEntity, component.MechEjectAction, mech);
        _actions.AddAction(pilot, ref component.ToggleActionEntity, component.ToggleAction, mech); //Goobstation Mech Lights toggle action
        // RS14-start
        if (component.EntrySuccessSound != null)
        {
            var ev = new MechEntrySuccessSoundEvent(mech, component.EntrySuccessSound);
            RaiseLocalEvent(mech, ref ev);
        }

        UpdateBatteryAlert(mech, component);
        UpdateHealthAlert(mech, component);
        // RS14-end
        UpdateHands(pilot, mech, true); // Goobstation
    }

    private void RemoveUser(EntityUid mech, EntityUid pilot)
    {
        if (HasComp<TileMovementComponent>(mech)) // Goob change - Prevent mech jank.
            RemComp<TileMovementComponent>(mech);

        // RS14-start
        RemComp<MechPilotComponent>(pilot);
        // RS14-end
        RemComp<InteractionRelayComponent>(pilot);
        // RS14-start
        if (TryComp<AlertsDisplayRelayComponent>(pilot, out var alertRelay) &&
            alertRelay.Source == mech)
        {
            RemComp<AlertsDisplayRelayComponent>(pilot);
        }
        // RS14-end

        _actions.RemoveProvidedActions(pilot, mech);
        UpdateHands(pilot, mech, false); // Goobstation
    }

    /// <summary>
    /// Destroys the mech, removing the user and ejecting all installed equipment.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public virtual void BreakMech(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        TryEject(uid, component);
        var equipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
        foreach (var ent in equipment)
        {
            RemoveEquipment(uid, ent, component, forced: true);
        }

        // RS14-start
        var modules = new List<EntityUid>(component.ModuleContainer.ContainedEntities);
        foreach (var ent in modules)
        {
            RemoveEquipment(uid, ent, component, forced: true);
        }

        if (component.BatterySlot.ContainedEntity is { } battery)
            _container.Remove(battery, component.BatterySlot);

        component.Energy = 0;
        component.MaxEnergy = 0;
        // RS14-end

        component.Broken = true;
        UpdateAppearance(uid, component);
        Dirty(uid, component);
        UpdateUserInterface(uid, component);
        UpdateBatteryAlert(uid, component);
        UpdateHealthAlert(uid, component);

        if (component.BrokenSound != null)
        {
            var ev = new MechBrokenSoundEvent(uid, component.BrokenSound);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    /// <summary>
    /// Cycles through the currently selected equipment.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public void CycleEquipment(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var allEquipment = component.EquipmentContainer.ContainedEntities.ToList();

        var equipmentIndex = -1;
        if (component.CurrentSelectedEquipment != null)
        {
            bool StartIndex(EntityUid u) => u == component.CurrentSelectedEquipment;
            equipmentIndex = allEquipment.FindIndex(StartIndex);
        }

        equipmentIndex++;
        component.CurrentSelectedEquipment = equipmentIndex >= allEquipment.Count
            ? null
            : allEquipment[equipmentIndex];

        var popupString = component.CurrentSelectedEquipment != null
            ? Loc.GetString("mech-equipment-select-popup", ("item", component.CurrentSelectedEquipment))
            : Loc.GetString("mech-equipment-select-none-popup");

        if (_net.IsServer)
            _popup.PopupEntity(popupString, uid);

        RefreshPilotHandVirtualItems((uid, component)); // RS14
        Dirty(uid, component);
    }

    // RS14-start
    private void OnEquipmentSelectRequest(RequestMechEquipmentSelectEvent args, EntitySessionEventArgs session)
    {
        var user = session.SenderSession.AttachedEntity;
        if (user == null)
            return;

        EntityUid? mech = null;
        if (TryComp<MechPilotComponent>(user.Value, out var pilot))
            mech = pilot.Mech;
        else if (TryComp<VehicleOperatorComponent>(user.Value, out var vehicleOperator) &&
                 vehicleOperator.Vehicle != null)
            mech = vehicleOperator.Vehicle.Value;

        if (mech == null)
            return;

        if (!TryComp<MechComponent>(mech.Value, out var mechComp))
            return;

        if (args.Equipment == null)
        {
            mechComp.CurrentSelectedEquipment = null;
            _popup.PopupClient(Loc.GetString("mech-equipment-select-none-popup"), mech.Value, user.Value);
        }
        else
        {
            var equipment = GetEntity(args.Equipment.Value);
            if (!Exists(equipment) || !mechComp.EquipmentContainer.ContainedEntities.Contains(equipment))
                return;

            mechComp.CurrentSelectedEquipment = equipment;
            _popup.PopupClient(Loc.GetString("mech-equipment-select-popup", ("item", equipment)), mech.Value, user.Value);
        }

        RefreshPilotHandVirtualItems((mech.Value, mechComp));
        Dirty(mech.Value, mechComp);
    }
    // RS14-end

    /// <summary>
    /// Inserts an equipment item into the mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toInsert"></param>
    /// <param name="component"></param>
    /// <param name="equipmentComponent"></param>
    public void InsertEquipment(EntityUid uid, EntityUid toInsert, MechComponent? component = null,
        MechEquipmentComponent? equipmentComponent = null, MechModuleComponent? moduleComponent = null) // RS14
    {
        if (!Resolve(uid, ref component))
            return;

        // RS14-start
        if (Resolve(toInsert, ref equipmentComponent, false))
        {
            if (component.EquipmentContainer.ContainedEntities.Count >= component.MaxEquipmentAmount)
                return;

            if (_whitelistSystem.IsWhitelistFail(component.EquipmentWhitelist, toInsert))
                return;

            equipmentComponent.EquipmentOwner = uid;
            Dirty(toInsert, equipmentComponent);
            _container.Insert(toInsert, component.EquipmentContainer);
            var ev = new MechEquipmentInsertedEvent(uid);
            RaiseLocalEvent(toInsert, ref ev);
            UpdateUserInterface(uid, component);
            return;
        }

        if (Resolve(toInsert, ref moduleComponent, false))
        {
            var usedModuleSize = 0;
            foreach (var moduleUid in component.ModuleContainer.ContainedEntities)
            {
                if (TryComp<MechModuleComponent>(moduleUid, out var installedModule))
                    usedModuleSize += installedModule.Size;
            }

            if (usedModuleSize + moduleComponent.Size > component.MaxModuleAmount)
                return;

            if (_whitelistSystem.IsWhitelistFail(component.ModuleWhitelist, toInsert))
                return;

            moduleComponent.ModuleOwner = uid;
            _container.Insert(toInsert, component.ModuleContainer);
            var ev = new MechModuleInsertedEvent(uid);
            RaiseLocalEvent(toInsert, ref ev);
            UpdateUserInterface(uid, component);
        }
        // RS14-end
    }

    /// <summary>
    /// Removes an equipment item from a mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toRemove"></param>
    /// <param name="component"></param>
    /// <param name="equipmentComponent"></param>
    /// <param name="forced">Whether or not the removal can be cancelled</param>
    public void RemoveEquipment(EntityUid uid, EntityUid toRemove, MechComponent? component = null,
        MechEquipmentComponent? equipmentComponent = null, bool forced = false, MechModuleComponent? moduleComponent = null) // RS14
    {
        if (!Resolve(uid, ref component))
            return;

        // RS14-start
        var isEquipment = Resolve(toRemove, ref equipmentComponent, false);
        var isModule = Resolve(toRemove, ref moduleComponent, false);

        if (!isEquipment && !isModule && !forced)
            return;
        // RS14-end

        if (!forced && isEquipment)
        {
            var attemptev = new AttemptRemoveMechEquipmentEvent();
            RaiseLocalEvent(toRemove, ref attemptev);
            if (attemptev.Cancelled)
                return;
        }

        // RS14-start
        if (isEquipment)
        {
            var ev = new MechEquipmentRemovedEvent(uid);
            RaiseLocalEvent(toRemove, ref ev);

            if (component.CurrentSelectedEquipment == toRemove)
                CycleEquipment(uid, component);

            equipmentComponent!.EquipmentOwner = null;
            Dirty(toRemove, equipmentComponent);
            _container.Remove(toRemove, component.EquipmentContainer);
        }
        else if (isModule)
        {
            var ev = new MechModuleRemovedEvent(uid);
            RaiseLocalEvent(toRemove, ref ev);

            moduleComponent!.ModuleOwner = null;
            _container.Remove(toRemove, component.ModuleContainer);
        }
        else
        {
            _container.Remove(toRemove, component.EquipmentContainer);
            _container.Remove(toRemove, component.ModuleContainer);
        }
        // RS14-end

        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Attempts to change the amount of energy in the mech.
    /// </summary>
    /// <param name="uid">The mech itself</param>
    /// <param name="delta">The change in energy</param>
    /// <param name="component"></param>
    /// <returns>If the energy was successfully changed.</returns>
    public virtual bool TryChangeEnergy(EntityUid uid, FixedPoint2 delta, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Energy + delta < 0)
            return false;

        component.Energy = FixedPoint2.Clamp(component.Energy + delta, 0, component.MaxEnergy);
        Dirty(uid, component);
        UpdateUserInterface(uid, component);
        UpdateBatteryAlert(uid, component); // RS14
        return true;
    }

    /// <summary>
    /// Sets the integrity of the mech.
    /// </summary>
    /// <param name="uid">The mech itself</param>
    /// <param name="value">The value the integrity will be set at</param>
    /// <param name="component"></param>
    public void SetIntegrity(EntityUid uid, FixedPoint2 value, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Integrity = FixedPoint2.Clamp(value, 0, component.MaxIntegrity);

        if (component.Integrity <= component.BrokenThreshold && !component.Broken)
        {
            BreakMech(uid, component);
        }
        else if (component.Integrity > component.BrokenThreshold && component.Broken)
        {
            component.Broken = false;
            UpdateAppearance(uid, component);
        }

        Dirty(uid, component);
        UpdateUserInterface(uid, component);
        UpdateHealthAlert(uid, component); // RS14
    }

    // RS14-start
    public void UpdateBatteryAlert(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.BatterySlot.ContainedEntity == null || component.MaxEnergy <= 0)
        {
            _alerts.ClearAlert(uid, component.BatteryAlert);
            _alerts.ShowAlert(uid, component.NoBatteryAlert);
            return;
        }

        var chargePercent = (short) MathF.Round(component.Energy.Float() / component.MaxEnergy.Float() * 10f);
        if (chargePercent == 0 && component.Energy > 0)
            chargePercent = 1;

        _alerts.ClearAlert(uid, component.NoBatteryAlert);
        _alerts.ShowAlert(uid, component.BatteryAlert, chargePercent);
    }

    public void UpdateHealthAlert(EntityUid uid, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.Broken)
        {
            _alerts.ClearAlert(uid, component.HealthAlert);
            _alerts.ShowAlert(uid, component.BrokenAlert);
            return;
        }

        _alerts.ClearAlert(uid, component.BrokenAlert);
        var healthPercent = component.MaxIntegrity <= 0
            ? (short) 4
            : (short) MathF.Round((1f - component.Integrity.Float() / component.MaxIntegrity.Float()) * 4f);
        _alerts.ShowAlert(uid, component.HealthAlert, healthPercent);
    }
    // RS14-end

    /// <summary>
    /// Checks if an entity can be inserted into the mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toInsert"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public bool CanInsert(EntityUid uid, EntityUid toInsert, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // RS14-start
        if (!_actionBlocker.CanMove(toInsert))
            return false;

        if (Vehicle.GetOperatorOrNull(uid) == toInsert)
            return false;

        return _container.CanInsert(toInsert, component.PilotSlot);
        // RS14-end
    }

    /// <summary>
    /// Updates the user interface
    /// </summary>
    /// <remarks>
    /// This is defined here so that UI updates can be accessed from shared.
    /// </remarks>
    public virtual void UpdateUserInterface(EntityUid uid, MechComponent? component = null)
    {
    }

    /// <summary>
    /// Attempts to insert a pilot into the mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toInsert"></param>
    /// <param name="component"></param>
    /// <returns>Whether or not the entity was inserted</returns>
    public bool TryInsert(EntityUid uid, EntityUid? toInsert, MechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (toInsert == null)
            return false;

        if (!CanInsert(uid, toInsert.Value, component))
            return false;

        _container.Insert(toInsert.Value, component.PilotSlot);
        return true;
    }

    /// <summary>
    /// Attempts to eject the current pilot from the mech
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="pilot">The pilot to eject</param>
    /// <returns>Whether or not the pilot was ejected.</returns>
    public bool TryEject(EntityUid uid, MechComponent? component = null, EntityUid? pilot = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // RS14-start
        if (!Vehicle.TryGetOperator(uid, out var operatorEnt))
            return false;

        if (pilot != null && pilot != operatorEnt.Value.Owner)
            return false;

        _container.RemoveEntity(uid, operatorEnt.Value.Owner);
        return true;
        // RS14-end
    }

    // Goobstation Change Start
    private void UpdateHands(EntityUid uid, EntityUid mech, bool active)
    {
        if (!TryComp<HandsComponent>(uid, out var handsComponent))
            return;

        if (active)
            BlockHands(uid, mech, handsComponent);
        else
            FreeHands(uid, mech);
    }

    private void BlockHands(EntityUid uid, EntityUid mech, HandsComponent handsComponent)
    {
        var freeHands = 0;
        foreach (var hand in _hands.EnumerateHands((uid, handsComponent)))
        {
            if (!_hands.TryGetHeldItem((uid, handsComponent), hand, out var held))
            {
                freeHands++;
                continue;
            }

            // Is this entity removable? (they might have handcuffs on)
            if (HasComp<UnremoveableComponent>(held) && held != mech)
                continue;

            _hands.DoDrop((uid, handsComponent), hand);
            freeHands++;
            if (freeHands == 2)
                break;
        }
        if (_virtualItem.TrySpawnVirtualItemInHand(mech, uid, out var virtItem1))
            EnsureComp<UnremoveableComponent>(virtItem1.Value);

        if (_virtualItem.TrySpawnVirtualItemInHand(mech, uid, out var virtItem2))
            EnsureComp<UnremoveableComponent>(virtItem2.Value);
    }

    private void FreeHands(EntityUid uid, EntityUid mech)
    {
        _virtualItem.DeleteInHandsMatching(uid, mech);
    }

    // Goobstation Change End
    private void OnGetMeleeWeapon(EntityUid uid, VehicleOperatorComponent component, GetMeleeWeaponEvent args)
    {
        if (args.Handled)
            return;

        // RS14-start
        if (component.Vehicle is not { } vehicle || !TryComp<MechComponent>(vehicle, out var mech))
            return;

        var weapon = mech.CurrentSelectedEquipment ?? vehicle;
        // RS14-end
        args.Weapon = weapon;
        args.Handled = true;
    }

    private void OnCanAttackFromContainer(EntityUid uid, VehicleOperatorComponent component, CanAttackFromContainerEvent args)
    {
        // RS14-start
        if (component.Vehicle is { } vehicle && HasComp<MechComponent>(vehicle))
            args.CanAttack = true;
        // RS14-end
    }

    private void OnAttackAttempt(EntityUid uid, VehicleOperatorComponent component, AttackAttemptEvent args)
    {
        if (component.Vehicle is { } vehicle && args.Target == vehicle) // RS14
            args.Cancel();
    }

    private void OnPilotCanAttackFromContainer(Entity<MechPilotComponent> ent, ref CanAttackFromContainerEvent args)
    {
        args.CanAttack = true;
    }

    private void OnPilotGetMeleeAttackEntity(Entity<MechPilotComponent> ent, ref GetMeleeAttackEntityEvent args)
    {
        if (args.Handled)
            return;

        args.AttackEntity = ent.Comp.Mech;
        args.Handled = true;
    }

    private void OnPilotGetMeleeWeapon(Entity<MechPilotComponent> ent, ref GetMeleeWeaponEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<HandsComponent>(ent.Owner))
        {
            args.Handled = true;
            return;
        }

        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mech))
            return;

        args.Weapon = mech.CurrentSelectedEquipment ?? ent.Comp.Mech;
        args.Handled = true;
    }

    private void OnPilotGetActiveWeapon(Entity<MechPilotComponent> ent, ref GetActiveWeaponEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mech))
            return;

        args.Weapon = mech.CurrentSelectedEquipment ?? ent.Comp.Mech;
        args.Handled = true;
    }

    private void OnPilotGetUsedEntity(Entity<MechPilotComponent> ent, ref GetUsedEntityEvent args)
    {
        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mech))
            return;

        if (!Vehicle.HasOperator(ent.Comp.Mech))
            return;

        if (mech.CurrentSelectedEquipment != null)
            args.Used = mech.CurrentSelectedEquipment;
    }

    private void OnPilotAccessible(Entity<MechPilotComponent> ent, ref AccessibleOverrideEvent args)
    {
        args.Handled = true;
        args.Accessible = _interaction.IsAccessible(ent.Comp.Mech, args.Target);
    }

    private void OnPilotGetShootingEntity(Entity<MechPilotComponent> ent, ref GetShootingEntityEvent args)
    {
        if (args.Handled)
            return;

        args.ShootingEntity = ent.Comp.Mech;
        args.Handled = true;
    }

    private static void OnPilotToolUseAttempt(Entity<MechPilotComponent> ent, ref ToolUserAttemptUseEvent args)
    {
        if (args.Target == ent.Comp.Mech)
            args.Cancelled = true;
    }

    // RS14-start
    private bool IsMechEquipmentUsableFromHands(Entity<MechEquipmentComponent> ent)
    {
        if (!ent.Comp.BlockUseOutsideMech)
            return true;

        if (ent.Comp.EquipmentOwner.HasValue)
            return true;

        if (_container.TryGetContainingContainer(ent.Owner, out var container) &&
            HasComp<MechComponent>(container.Owner))
            return true;

        return false;
    }

    private void OnMechEquipmentMeleeAttempt(Entity<MechEquipmentComponent> ent, ref AttemptMeleeEvent args)
    {
        if (!IsMechEquipmentUsableFromHands(ent))
            args.Cancelled = true;
    }

    private void OnMechEquipmentGettingUsedAttempt(Entity<MechEquipmentComponent> ent, ref GettingUsedAttemptEvent args)
    {
        if (_net.IsClient)
            return;

        if (!IsMechEquipmentUsableFromHands(ent))
            args.Cancel();
    }

    private void OnMechEquipmentUiOpenAttempt(Entity<MechEquipmentComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!IsMechEquipmentUsableFromHands(ent))
            args.Cancel();
    }

    // Goobstation: Prevent guns being used out of mechs if CCVAR is set.
    private void OnShotAttempted(EntityUid uid, MechEquipmentComponent component, ref ShotAttemptedEvent args)
    {
        if (!component.EquipmentOwner.HasValue
            || !HasComp<MechComponent>(component.EquipmentOwner.Value))
        {
            if (component.BlockUseOutsideMech && !_canUseMechGunOutside)
                args.Cancel();
            return;
        }

        var ev = new HandleMechEquipmentBatteryEvent();
        RaiseLocalEvent(uid, ev);
    }
    // RS14-end

    private void UpdateAppearance(EntityUid uid, MechComponent? component = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;

        _appearance.SetData(uid, MechVisuals.Open, !Vehicle.HasOperator(uid), appearance); // RS14
        _appearance.SetData(uid, MechVisuals.Broken, component.Broken, appearance);
    }

    private void OnDragDrop(EntityUid uid, MechComponent component, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Dragged, component.EntryDelay, new MechEntryEvent(), uid, target: uid)
        {
            BreakOnMove = true,
            MultiplyDelay = false // Goobstation
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnCanDragDrop(EntityUid uid, MechComponent component, ref CanDropTargetEvent args)
    {
        args.Handled = true;

        args.CanDrop |= !component.Broken && CanInsert(uid, args.Dragged, component);
    }

    private void OnEmagged(EntityUid uid, MechComponent component, ref GotEmaggedEvent args) // Goobstation
    {
        if (!component.BreakOnEmag || !_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;
        args.Handled = true;
        component.EquipmentWhitelist = null;
        Dirty(uid, component);
    }

    // RS14-start
    private void OnContainerChanged(Entity<MechComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container == ent.Comp.PilotSlot)
            SetBatterySlotLocked(ent.Owner, ent.Comp, false);
    }

    private void OnOperatorSet(Entity<MechComponent> ent, ref VehicleOperatorSetEvent args)
    {
        if (args.OldOperator is { } oldOperator)
        {
            RemoveUser(ent, oldOperator);
            SetBatterySlotLocked(ent.Owner, ent.Comp, false);

            var ev = new MechEjectedEvent(ent);
            RaiseLocalEvent(oldOperator, ev);
        }

        if (args.NewOperator is { } newOperator)
        {
            SetupUser(ent, newOperator, ent);
            SetBatterySlotLocked(ent.Owner, ent.Comp, true);

            var ev = new MechInsertedEvent(ent);
            RaiseLocalEvent(newOperator, ev);
        }

        UpdateAppearance(ent, ent);
        UpdateUserInterface(ent, ent);

        var drainEv = new MechMovementDrainToggleEvent(args.NewOperator != null);
        RaiseLocalEvent(ent, ref drainEv);
    }

    protected void SetBatterySlotLocked(EntityUid uid, MechComponent component, bool locked)
    {
        if (TryComp<ItemSlotsComponent>(uid, out var slots))
            _itemSlots.SetLock(uid, component.BatterySlotId, locked, slots);
    }
    // RS14-end
}

/// <summary>
///     Event raised when the battery is successfully removed from the mech,
///     on both success and failure
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RemoveBatteryEvent : SimpleDoAfterEvent; // RS14

/// <summary>
///     Event raised when a person removes someone from a mech,
///     on both success and failure
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MechExitEvent : SimpleDoAfterEvent; // RS14

/// <summary>
///     Event raised when a person enters a mech, on both success and failure
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MechEntryEvent : SimpleDoAfterEvent; // RS14

// RS14-start
/// <summary>
/// Raised when a mech enters broken state and should play its configured sound.
/// </summary>
[ByRefEvent]
public readonly record struct MechBrokenSoundEvent(EntityUid Mech, SoundSpecifier Sound);

/// <summary>
/// Raised when a pilot successfully enters a mech and should play its configured sound.
/// </summary>
[ByRefEvent]
public readonly record struct MechEntrySuccessSoundEvent(EntityUid Mech, SoundSpecifier Sound);
// RS14-end

/// <summary>
///     Event raised when an user attempts to fire a mech weapon to check if its battery is drained
/// </summary>

[Serializable, NetSerializable]
public sealed partial class HandleMechEquipmentBatteryEvent : EntityEventArgs
{
}
