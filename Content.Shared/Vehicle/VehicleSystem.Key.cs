// SPDX-FileCopyrightText: 2025 Fenriz <kastonbag552@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Vehicle.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Vehicle;

public sealed partial class VehicleSystem
{
    private void InitializeKey()
    {
        SubscribeLocalEvent<GenericKeyedVehicleComponent, ContainerIsInsertingAttemptEvent>(OnGenericKeyedInsertAttempt);
        SubscribeLocalEvent<GenericKeyedVehicleComponent, EntInsertedIntoContainerMessage>(OnGenericKeyedEntInserted);
        SubscribeLocalEvent<GenericKeyedVehicleComponent, EntRemovedFromContainerMessage>(OnGenericKeyedEntRemoved);
        SubscribeLocalEvent<GenericKeyedVehicleComponent, ComponentShutdown>(OnGenericKeyedShutdown);
        SubscribeLocalEvent<VehicleKeyComponent, ComponentShutdown>(OnVehicleKeyShutdown);
        SubscribeLocalEvent<GenericKeyedVehicleComponent, VehicleCanRunEvent>(OnGenericKeyedCanRun);
    }

    private void OnGenericKeyedInsertAttempt(Entity<GenericKeyedVehicleComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || _timing.ApplyingState || args.Container.ID != ent.Comp.ContainerId)
            return;

        ClearDeletedBoundKey(ent);

        if (TryComp<VehicleKeyComponent>(args.EntityUid, out var key) &&
            key.BoundVehicle is { } boundVehicle &&
            boundVehicle != ent.Owner &&
            !Deleted(boundVehicle))
        {
            PopupWrongKey(ent, args.EntityUid);
            args.Cancel();
            return;
        }

        if (ent.Comp.BoundKey is { } boundKey && boundKey != args.EntityUid)
        {
            PopupWrongKey(ent, args.EntityUid);
            args.Cancel();
            return;
        }

        if (!ent.Comp.PreventInvalidInsertion)
            return;

        if (_entityWhitelist.IsWhitelistFail(ent.Comp.KeyWhitelist, args.EntityUid))
            args.Cancel();
    }

    private void OnGenericKeyedEntInserted(Entity<GenericKeyedVehicleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != ent.Comp.ContainerId)
            return;

        ClearDeletedBoundKey(ent);

        if (ent.Comp.BoundKey == null || ent.Comp.BoundKey == args.Entity)
            TryBindKey(ent, args.Entity);

        RefreshKeyedVehicle(ent);
    }

    private void OnGenericKeyedEntRemoved(Entity<GenericKeyedVehicleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != ent.Comp.ContainerId)
            return;

        RefreshKeyedVehicle(ent);
    }

    private void OnGenericKeyedShutdown(Entity<GenericKeyedVehicleComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.BoundKey is not { } boundKey ||
            !TryComp<VehicleKeyComponent>(boundKey, out var key) ||
            key.BoundVehicle != ent.Owner)
            return;

        key.BoundVehicle = null;
        Dirty(boundKey, key);
    }

    private void OnVehicleKeyShutdown(Entity<VehicleKeyComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState ||
            ent.Comp.BoundVehicle is not { } vehicleUid ||
            !_vehicleQuery.TryComp(vehicleUid, out var vehicle) ||
            !TryComp<GenericKeyedVehicleComponent>(vehicleUid, out var keyed) ||
            keyed.BoundKey != ent.Owner)
            return;

        keyed.BoundKey = null;
        Dirty(vehicleUid, keyed);

        RefreshCanRun((vehicleUid, vehicle));
    }

    private void OnGenericKeyedCanRun(Entity<GenericKeyedVehicleComponent> ent, ref VehicleCanRunEvent args)
    {
        if (!args.CanRun)
            return;

        if (IsMissingRequiredKey(ent))
            args = args with { CanRun = false };
    }

    private bool TryBindKey(Entity<GenericKeyedVehicleComponent> ent, EntityUid inserted)
    {
        if (_entityWhitelist.IsWhitelistFail(ent.Comp.KeyWhitelist, inserted))
            return false;

        var key = EnsureComp<VehicleKeyComponent>(inserted);
        if (key.BoundVehicle is { } boundVehicle &&
            boundVehicle != ent.Owner &&
            !Deleted(boundVehicle))
            return false;

        ent.Comp.BoundKey = inserted;
        key.BoundVehicle = ent.Owner;
        Dirty(ent);
        Dirty(inserted, key);

        return true;
    }

    private void ClearDeletedBoundKey(Entity<GenericKeyedVehicleComponent> ent)
    {
        if (ent.Comp.BoundKey is not { } boundKey || !Deleted(boundKey))
            return;

        ent.Comp.BoundKey = null;
        Dirty(ent);
    }

    private void RefreshKeyedVehicle(Entity<GenericKeyedVehicleComponent> ent)
    {
        if (!_vehicleQuery.TryComp(ent, out var vehicle))
            return;

        RefreshCanRun((ent.Owner, vehicle));

        if (vehicle.Operator is { } operatorUid)
            _actionBlocker.UpdateCanMove(operatorUid);
    }

    private bool IsMissingRequiredKey(Entity<GenericKeyedVehicleComponent> ent)
    {
        ClearDeletedBoundKey(ent);

        if (ent.Comp.BoundKey is not { } boundKey)
            return true;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            return true;

        return !container.Contains(boundKey);
    }

    private void PopupWrongKey(Entity<GenericKeyedVehicleComponent> ent, EntityUid key)
    {
        if (_timing.CurTime < ent.Comp.NextWrongKeyPopup)
            return;

        ent.Comp.NextWrongKeyPopup = _timing.CurTime + TimeSpan.FromSeconds(1);
        _popup.PopupPredicted(Loc.GetString("vehicle-key-wrong"), ent.Owner, key);
    }
}
