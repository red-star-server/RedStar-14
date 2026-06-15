// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Shared.Interaction;
using Content.Shared.Mech.Equipment.Components;

namespace Content.Shared.Mech.EntitySystems;

/// <summary>
/// Handles installation of active mech equipment.
/// </summary>
public sealed class MechEquipmentSystem : MechInstallSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechEquipmentComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<MechEquipmentComponent, InsertEquipmentEvent>(OnInsert);
    }

    private void OnUsed(Entity<MechEquipmentComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var mech = args.Target.Value;
        if (!TryPrepareInstall(args.User, mech, out var mechComp) || mechComp == null)
            return;

        if (mechComp.EquipmentContainer.ContainedEntities.Count >= mechComp.MaxEquipmentAmount)
        {
            Popup.PopupClient(Loc.GetString("mech-equipment-slot-full-popup"), args.User, args.User);
            return;
        }

        if (Whitelist.IsWhitelistFail(mechComp.EquipmentWhitelist, ent.Owner))
        {
            Popup.PopupClient(Loc.GetString("mech-equipment-whitelist-fail-popup"), args.User, args.User);
            return;
        }

        if (HasDuplicateInstalled(ent.Owner, mechComp.EquipmentContainer.ContainedEntities, args.User))
            return;

        StartInstallDoAfter(args.User, ent.Owner, mech, ent.Comp.InstallDuration, new InsertEquipmentEvent());
        args.Handled = true;
    }

    private void OnInsert(Entity<MechEquipmentComponent> ent, ref InsertEquipmentEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        if (!TryFinishInstall(ent.Owner,
                args.Args.User,
                args.Args.Target.Value,
                mech => mech.EquipmentContainer.ContainedEntities,
                out var mechComp) ||
            mechComp == null)
            return;

        if (mechComp.EquipmentContainer.ContainedEntities.Count >= mechComp.MaxEquipmentAmount)
        {
            Popup.PopupClient(Loc.GetString("mech-equipment-slot-full-popup"), args.Args.User, args.Args.User);
            return;
        }

        Mech.InsertEquipment(args.Args.Target.Value, ent.Owner, equipmentComponent: ent.Comp);
        args.Handled = true;
    }
}
