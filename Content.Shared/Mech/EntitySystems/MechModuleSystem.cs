// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Shared.Interaction;
using Content.Shared.Mech.Module.Components;

namespace Content.Shared.Mech.EntitySystems;

/// <summary>
/// Handles installation of passive mech modules.
/// </summary>
public sealed class MechModuleSystem : MechInstallSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechModuleComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<MechModuleComponent, InsertModuleEvent>(OnInsert);
    }

    private void OnUsed(Entity<MechModuleComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var mech = args.Target.Value;
        if (!TryPrepareInstall(args.User, mech, out var mechComp) || mechComp == null)
            return;

        if (GetUsedModuleSize(mechComp.ModuleContainer.ContainedEntities) + ent.Comp.Size > mechComp.MaxModuleAmount)
        {
            Popup.PopupClient(Loc.GetString("mech-module-slot-full-popup"), args.User, args.User);
            return;
        }

        if (Whitelist.IsWhitelistFail(mechComp.ModuleWhitelist, ent.Owner))
        {
            Popup.PopupClient(Loc.GetString("mech-module-whitelist-fail-popup"), args.User, args.User);
            return;
        }

        if (HasDuplicateInstalled(ent.Owner, mechComp.ModuleContainer.ContainedEntities, args.User))
            return;

        StartInstallDoAfter(args.User, ent.Owner, mech, ent.Comp.InstallDuration, new InsertModuleEvent());
        args.Handled = true;
    }

    private int GetUsedModuleSize(IReadOnlyList<EntityUid> installed)
    {
        var usedModuleSize = 0;

        foreach (var installedModule in installed)
        {
            if (TryComp<MechModuleComponent>(installedModule, out var module))
                usedModuleSize += module.Size;
        }

        return usedModuleSize;
    }

    private void OnInsert(Entity<MechModuleComponent> ent, ref InsertModuleEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        if (!TryFinishInstall(ent.Owner,
                args.Args.User,
                args.Args.Target.Value,
                mech => mech.ModuleContainer.ContainedEntities,
                out var mechComp) ||
            mechComp == null)
            return;

        if (GetUsedModuleSize(mechComp.ModuleContainer.ContainedEntities) + ent.Comp.Size > mechComp.MaxModuleAmount)
        {
            Popup.PopupClient(Loc.GetString("mech-module-slot-full-popup"), args.Args.User, args.Args.User);
            return;
        }

        Mech.InsertEquipment(args.Args.Target.Value, ent.Owner, moduleComponent: ent.Comp);
        args.Handled = true;
    }
}
