// SPDX-FileCopyrightText: 2025 Fenriz <kastonbag552@gmail.com>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Vehicle.Components;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Vehicle;

/// <summary>
/// Handles logic relating to vehicles.
/// </summary>
public sealed partial class VehicleSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

    private EntityQuery<VehicleComponent> _vehicleQuery;
    private EntityQuery<VehicleOperatorComponent> _operatorQuery;
    private EntityQuery<ContainerVehicleComponent> _containerVehicleQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;
    private EntityQuery<InputMoverComponent> _inputMoverQuery;
    private EntityQuery<HandsComponent> _handsQuery;
    private EntityQuery<InteractionRelayComponent> _interactionRelayQuery;
    private EntityQuery<MovementRelayTargetComponent> _relayTargetQuery;
    private EntityQuery<RelayInputMoverComponent> _relayQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _vehicleQuery = GetEntityQuery<VehicleComponent>();
        _operatorQuery = GetEntityQuery<VehicleOperatorComponent>();
        _containerVehicleQuery = GetEntityQuery<ContainerVehicleComponent>();
        _appearanceQuery = GetEntityQuery<AppearanceComponent>();
        _inputMoverQuery = GetEntityQuery<InputMoverComponent>();
        _handsQuery = GetEntityQuery<HandsComponent>();
        _interactionRelayQuery = GetEntityQuery<InteractionRelayComponent>();
        _relayTargetQuery = GetEntityQuery<MovementRelayTargetComponent>();
        _relayQuery = GetEntityQuery<RelayInputMoverComponent>();

        InitializeOperator();
        InitializeKey();

        SubscribeLocalEvent<VehicleComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<VehicleComponent, UpdateCanMoveEvent>(OnVehicleUpdateCanMove);
        SubscribeLocalEvent<VehicleComponent, ComponentShutdown>(OnVehicleShutdown);
        SubscribeLocalEvent<VehicleComponent, GetAdditionalAccessEvent>(OnVehicleGetAdditionalAccess);

        SubscribeLocalEvent<VehicleOperatorComponent, ComponentShutdown>(OnOperatorShutdown);
    }

    /// <remarks>
    /// We subscribe to BeforeDamageChangedEvent so that we can access the damage value before the container is applied.
    /// </remarks>
    private void OnBeforeDamageChanged(Entity<VehicleComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!ent.Comp.TransferDamage || !args.Damage.AnyPositive() || ent.Comp.Operator is not { } operatorUid)
            return;

        var damage = DamageSpecifier.GetPositive(args.Damage);

        if (ent.Comp.TransferDamageModifier is { } modifierSet)
        {
            // Reduce damage to the operator via the specified modifier, if provided.
            damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);
        }

        _damageable.TryChangeDamage(operatorUid, damage, origin: args.Origin);
    }

    private void OnVehicleUpdateCanMove(Entity<VehicleComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (!CanVehicleRun(ent))
            args.Cancel();
    }

    private void OnVehicleShutdown(Entity<VehicleComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        ClearOperator(ent, removeOperatorComponent: true, raiseSetEvent: true);
    }

    private void OnVehicleGetAdditionalAccess(Entity<VehicleComponent> ent, ref GetAdditionalAccessEvent args)
    {
        // Vehicles inherit access from whoever is driving them
        if (ent.Comp.Operator is { } operatorUid && Exists(operatorUid))
            args.Entities.Add(operatorUid);
    }

    private void OnOperatorShutdown(Entity<VehicleOperatorComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Vehicle is { } vehicleUid &&
            _vehicleQuery.TryComp(vehicleUid, out var vehicle))
        {
            ClearOperator((vehicleUid, vehicle), removeOperatorComponent: false, raiseSetEvent: true);
            return;
        }

        CleanupOperatorRelays(ent, ent.Comp.Vehicle);
    }

    /// <summary>
    /// Set the operator for a given vehicle
    /// </summary>
    /// <param name="entity">The vehicle</param>
    /// <param name="uid">The new operator. If null, it will only remove the operator.</param>
    /// <param name="removeExisting">If true, will remove the current operator when setting the new one.</param>
    /// <returns>If the new operator was successfully able to be set</returns>
    public bool TrySetOperator(Entity<VehicleComponent> entity, EntityUid? uid, bool removeExisting = true)
    {
        var oldOperator = entity.Comp.Operator;

        if (oldOperator == null && uid is null)
            return false;

        if (uid is not null && _operatorQuery.TryComp(uid, out var eOperator))
        {
            if (eOperator.Vehicle == entity.Owner)
            {
                if (!CanUseOperatorRelays(uid.Value))
                    return false;

                entity.Comp.Operator = uid;
                SetOperatorComponent(uid.Value, entity.Owner, eOperator);
                EnsureOperatorRelays(uid.Value, entity.Owner);
                RefreshCanRun((entity.Owner, entity.Comp));
                Dirty(entity);
                return true;
            }

            if (!removeExisting)
                return false;

            if (eOperator.Vehicle is { } oldVehicleUid && _vehicleQuery.TryComp(oldVehicleUid, out var oldVehicle))
                ClearOperator((oldVehicleUid, oldVehicle), removeOperatorComponent: true, raiseSetEvent: true);
            else
                CleanupOperatorRelays(uid.Value, eOperator.Vehicle);
        }

        if (!removeExisting && oldOperator is not null && oldOperator != uid)
            return false;

        if (uid is { } newOperator &&
            (!CanOperate(entity.AsNullable(), newOperator) || !CanUseOperatorRelays(newOperator)))
            return false;

        if (oldOperator is { } currentOperator)
            ClearOperator(entity, removeOperatorComponent: true, raiseSetEvent: false);

        entity.Comp.Operator = uid;

        if (uid is { } operatorUid)
        {
            SetOperatorComponent(operatorUid, entity.Owner);
            EnsureOperatorRelays(operatorUid, entity.Owner);

            var enterEvent = new OnVehicleEnteredEvent(entity, operatorUid);
            RaiseLocalEvent(operatorUid, ref enterEvent);
        }

        RefreshCanRun((entity.Owner, entity.Comp));

        var setEvent = new VehicleOperatorSetEvent(uid, oldOperator);
        RaiseLocalEvent(entity, ref setEvent);

        Dirty(entity);
        return true;
    }

    private void ClearOperator(Entity<VehicleComponent> entity, bool removeOperatorComponent, bool raiseSetEvent)
    {
        var oldOperator = entity.Comp.Operator;
        entity.Comp.Operator = null;

        if (oldOperator is { } oldOperatorUid)
        {
            var exitEvent = new OnVehicleExitedEvent(entity, oldOperatorUid);
            RaiseLocalEvent(oldOperatorUid, ref exitEvent);
            CleanupOperatorRelays(oldOperatorUid, entity.Owner);

            if (removeOperatorComponent)
                RemCompDeferred<VehicleOperatorComponent>(oldOperatorUid);
            else if (_operatorQuery.TryComp(oldOperatorUid, out var operatorComponent))
            {
                operatorComponent.Vehicle = null;
                Dirty(oldOperatorUid, operatorComponent);
            }
        }

        if (_relayTargetQuery.TryComp(entity.Owner, out var relayTarget))
            RemCompDeferred(entity.Owner, relayTarget);

        RefreshCanRun((entity.Owner, entity.Comp));

        if (raiseSetEvent)
        {
            var setEvent = new VehicleOperatorSetEvent(null, oldOperator);
            RaiseLocalEvent(entity, ref setEvent);
        }

        Dirty(entity);
    }

    private void SetOperatorComponent(EntityUid operatorUid, EntityUid vehicleUid, VehicleOperatorComponent? component = null)
    {
        component ??= EnsureComp<VehicleOperatorComponent>(operatorUid);
        component.Vehicle = vehicleUid;
        Dirty(operatorUid, component);
    }

    private bool CanUseOperatorRelays(EntityUid operatorUid)
    {
        if (_relayQuery.TryComp(operatorUid, out var relay) && !Exists(relay.RelayEntity))
            return false;

        if (_interactionRelayQuery.TryComp(operatorUid, out var interactionRelay) && !Exists(interactionRelay.RelayEntity))
            return false;

        return true;
    }

    private void EnsureOperatorRelays(EntityUid operatorUid, EntityUid vehicleUid)
    {
        _mover.SetRelay(operatorUid, vehicleUid);

        if (!_containerVehicleQuery.HasComp(vehicleUid))
        {
            CleanupOperatorInteractionRelay(operatorUid, vehicleUid);
            return;
        }

        var interactionRelay = EnsureComp<InteractionRelayComponent>(operatorUid);
        _interaction.SetRelay(operatorUid, vehicleUid, interactionRelay);
    }

    private void CleanupOperatorRelays(EntityUid operatorUid, EntityUid? vehicleUid)
    {
        if (_relayQuery.TryComp(operatorUid, out var relay) &&
            (vehicleUid == null || relay.RelayEntity == vehicleUid))
        {
            RemCompDeferred(operatorUid, relay);
        }

        CleanupOperatorInteractionRelay(operatorUid, vehicleUid);
    }

    private void CleanupOperatorInteractionRelay(EntityUid operatorUid, EntityUid? vehicleUid)
    {
        if (_interactionRelayQuery.TryComp(operatorUid, out var interactionRelay) &&
            (vehicleUid == null || interactionRelay.RelayEntity == vehicleUid))
        {
            RemCompDeferred(operatorUid, interactionRelay);
        }
    }

    /// <summary>
    /// Attempts to remove the current operator from a vehicle
    /// </summary>
    /// <param name="entity">The vehicle whose operator is being removed.</param>
    /// <returns>If the operator was removed successfully</returns>
    [PublicAPI]
    public bool TryRemoveOperator(Entity<VehicleComponent> entity)
    {
        return TrySetOperator(entity, null, removeExisting: true);
    }

    /// <summary>
    /// From an operator, removes it from the vehicle
    /// </summary>
    /// <param name="operatorEntity">The operator who is riding a vehicle</param>
    /// <returns>If the operator was removed successfully, or if the entity was not operating a vehicle.</returns>
    [PublicAPI]
    public bool TryRemoveOperator(Entity<VehicleOperatorComponent?> operatorEntity)
    {
        if (!Resolve(operatorEntity, ref operatorEntity.Comp, false))
            return true;

        if (!_vehicleQuery.TryComp(operatorEntity.Comp.Vehicle, out var vehicle))
            return true;

        return TrySetOperator((operatorEntity.Comp.Vehicle.Value, vehicle), null, removeExisting: true);
    }

    /// <summary>
    /// Attempts to get the current operator of a vehicle
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="operatorEnt"></param>
    [PublicAPI]
    public bool TryGetOperator(Entity<VehicleComponent?> entity, [NotNullWhen(true)] out Entity<VehicleOperatorComponent>? operatorEnt)
    {
        operatorEnt = null;
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (entity.Comp.Operator is not { } operatorUid)
            return false;

        if (!_operatorQuery.TryComp(operatorUid, out var operatorComponent))
            return false;

        operatorEnt = (operatorUid, operatorComponent);
        return true;
    }

    /// <summary>
    /// Returns the operator of the vehicle or none if there isn't one present
    /// </summary>
    public EntityUid? GetOperatorOrNull(Entity<VehicleComponent?> entity)
    {
        TryGetOperator(entity, out var operatorEnt);
        return operatorEnt;
    }

    /// <summary>
    /// Checks if the current vehicle has an operator.
    /// </summary>
    [PublicAPI]
    public bool HasOperator(Entity<VehicleComponent?> entity)
    {
        return TryGetOperator(entity, out _);
    }

    /// <summary>
    /// Checks if a given entity is capable of operating a vehicle.
    /// Note that the general ability for a vehicle to run (keys, fuel, etc.) is not checked here.
    /// This is *only* for checks on the user.
    /// </summary>
    public bool CanOperate(Entity<VehicleComponent?> entity, EntityUid uid)
    {
        if (!Exists(uid))
            return false;

        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (_entityWhitelist.IsWhitelistFail(entity.Comp.OperatorWhitelist, uid))
            return false;

        if (entity.Comp.RequiresHands && (!_handsQuery.HasComp(uid) || !_actionBlocker.CanInteract(uid, entity)))
            return false;

        return _actionBlocker.CanConsciouslyPerformAction(uid);
    }

    /// <summary>
    /// Checks if the vehicle is capable of running (has keys, fuel, etc.) and caches the value.
    /// Updates the appearance data.
    /// </summary>
    public void RefreshCanRun(Entity<VehicleComponent> entity)
    {
        if (TerminatingOrDeleted(entity))
            return;

        _actionBlocker.UpdateCanMove(entity.Owner);
        UpdateAppearance(entity);
    }

    private bool CanVehicleRun(Entity<VehicleComponent> entity)
    {
        var ev = new VehicleCanRunEvent(entity);
        RaiseLocalEvent(entity, ref ev);
        return ev.CanRun;
    }

    private void UpdateAppearance(Entity<VehicleComponent> entity)
    {
        if (!_appearanceQuery.TryComp(entity, out var appearance))
            return;

        if (_inputMoverQuery.TryComp(entity, out var inputMover))
        {
            _appearance.SetData(entity, VehicleVisuals.CanRun, inputMover.CanMove, appearance);
        }

        _appearance.SetData(entity, VehicleVisuals.HasOperator, entity.Comp.Operator is not null, appearance);
    }
}
