// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Mech.Module.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechSystem
{
    // RS14-start
    public void InsertEquipment(Entity<MechComponent?> ent,
        EntityUid toInsert,
        MechEquipmentComponent? equipmentComponent = null,
        MechModuleComponent? moduleComponent = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        InsertEquipment(ent.Owner, toInsert, ent.Comp, equipmentComponent, moduleComponent);
    }

    public void RemoveEquipment(Entity<MechComponent?> ent,
        EntityUid toRemove,
        MechEquipmentComponent? equipmentComponent = null,
        bool forced = false,
        MechModuleComponent? moduleComponent = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        RemoveEquipment(ent.Owner, toRemove, ent.Comp, equipmentComponent, forced, moduleComponent);
    }

    public bool TryChangeEnergy(Entity<MechComponent?> ent, FixedPoint2 delta)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return TryChangeEnergy(ent.Owner, delta, ent.Comp);
    }

    public void SetIntegrity(Entity<MechComponent?> ent, FixedPoint2 value)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        SetIntegrity(ent.Owner, value, ent.Comp);
    }

    public bool CanInsert(Entity<MechComponent?> ent, EntityUid toInsert)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return CanInsert(ent.Owner, toInsert, ent.Comp);
    }

    public bool TryInsert(Entity<MechComponent?> ent, EntityUid toInsert)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return TryInsert(ent.Owner, toInsert, ent.Comp);
    }

    public bool TryEject(Entity<MechComponent?> ent, EntityUid? pilot = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return TryEject(ent.Owner, ent.Comp, pilot);
    }

    public void UpdateMechUi(EntityUid uid)
    {
        var ev = new UpdateMechUiEvent();
        RaiseLocalEvent(uid, ev);
        UpdateUserInterface(uid);
    }

    public void RefreshPilotHandVirtualItems(Entity<MechComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var pilot = Vehicle.GetOperatorOrNull(ent.Owner);
        if (pilot == null)
            return;

        var blocking = ent.Comp.CurrentSelectedEquipment ?? ent.Owner;
        foreach (var held in _hands.EnumerateHeld(pilot.Value))
        {
            if (!TryComp<VirtualItemComponent>(held, out var virtualItem))
                continue;

            if (virtualItem.BlockingEntity == blocking)
                continue;

            virtualItem.BlockingEntity = blocking;
            Dirty(held, virtualItem);
        }
    }
    // RS14-end
}

// RS14-start
[Serializable, NetSerializable]
public sealed class UpdateMechUiEvent : EntityEventArgs;
// RS14-end
