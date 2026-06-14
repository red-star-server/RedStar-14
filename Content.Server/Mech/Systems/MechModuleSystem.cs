// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Module.Components;
using Content.Shared.Vehicle;
using Content.Shared.Whitelist;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Handles insertion of passive mech modules into mechs.
/// </summary>
public sealed class MechModuleSystem : EntitySystem
{
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly VehicleSystem _vehicle = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly MechLockSystem _mechLock = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MechModuleComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<MechModuleComponent, InsertModuleEvent>(OnInsert);
    }

    private void OnUsed(Entity<MechModuleComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var mech = args.Target.Value;
        if (!TryComp<MechComponent>(mech, out var mechComp))
            return;

        if (mechComp.Broken)
            return;

        if (!_mechLock.CheckAccessWithFeedback(mech, args.User))
            return;

        if (_vehicle.HasOperator(mech))
        {
            _popup.PopupEntity(Loc.GetString("mech-cannot-modify-closed-popup"), args.User, args.User);
            return;
        }

        if (GetUsedModuleSize(mechComp.ModuleContainer.ContainedEntities) + ent.Comp.Size > mechComp.MaxModuleAmount)
        {
            _popup.PopupEntity(Loc.GetString("mech-module-slot-full-popup"), args.User, args.User);
            return;
        }

        if (_whitelistSystem.IsWhitelistFail(mechComp.ModuleWhitelist, ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("mech-module-whitelist-fail-popup"), args.User, args.User);
            return;
        }

        if (HasDuplicateInstalled(ent.Owner, mechComp.ModuleContainer.ContainedEntities, args.User))
            return;

        _popup.PopupEntity(Loc.GetString("mech-module-begin-install", ("item", ent.Owner)), mech);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.InstallDuration, new InsertModuleEvent(), ent.Owner, target: mech, used: ent.Owner)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private bool HasDuplicateInstalled(EntityUid module, IReadOnlyList<EntityUid> installed, EntityUid user)
    {
        var prototype = MetaData(module).EntityPrototype?.ID;
        if (prototype == null)
            return false;

        foreach (var installedModule in installed)
        {
            if (MetaData(installedModule).EntityPrototype?.ID != prototype)
                continue;

            _popup.PopupEntity(Loc.GetString("mech-duplicate-installed-popup"), user, user);
            return true;
        }

        return false;
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

        _popup.PopupEntity(Loc.GetString("mech-module-finish-install", ("item", ent.Owner)), args.Args.Target.Value);
        _mech.InsertEquipment(args.Args.Target.Value, ent.Owner, moduleComponent: ent.Comp);

        args.Handled = true;
    }
}
