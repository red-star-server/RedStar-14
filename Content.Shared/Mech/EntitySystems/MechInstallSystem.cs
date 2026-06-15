// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mech.Components;
using Content.Shared.Popups;
using Content.Shared.Vehicle;
using Content.Shared.Whitelist;

namespace Content.Shared.Mech.EntitySystems;

/// <summary>
/// Shared helper logic for installing mech equipment and passive modules.
/// </summary>
public abstract class MechInstallSystem : EntitySystem
{
    [Dependency] protected readonly EntityWhitelistSystem Whitelist = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected readonly SharedMechSystem Mech = default!;
    [Dependency] protected readonly SharedMechLockSystem MechLock = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly VehicleSystem Vehicle = default!;

    /// <summary>
    /// Common precondition checks before starting an install do-after.
    /// </summary>
    protected bool TryPrepareInstall(EntityUid user, EntityUid target, out MechComponent? mechComp)
    {
        if (!TryComp(target, out mechComp))
            return false;

        if (!MechLock.CheckAccessWithFeedback(target, user))
            return false;

        if (mechComp.Broken)
        {
            Popup.PopupClient(Loc.GetString("mech-cannot-insert-broken-popup"), user, user);
            return false;
        }

        if (Vehicle.HasOperator(target))
        {
            Popup.PopupClient(Loc.GetString("mech-cannot-modify-closed-popup"), user, user);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Rechecks install preconditions when the do-after completes.
    /// </summary>
    protected bool TryFinishInstall(
        EntityUid item,
        EntityUid user,
        EntityUid target,
        Func<MechComponent, IReadOnlyList<EntityUid>> getInstalled,
        out MechComponent? mechComp)
    {
        if (!TryPrepareInstall(user, target, out mechComp) || mechComp == null)
            return false;

        return !HasDuplicateInstalled(item, getInstalled(mechComp), user);
    }

    /// <summary>
    /// Checks duplicate installation by prototype id.
    /// </summary>
    protected bool HasDuplicateInstalled(EntityUid item, IReadOnlyList<EntityUid> installed, EntityUid user)
    {
        var prototype = MetaData(item).EntityPrototype?.ID;
        if (prototype == null)
            return false;

        foreach (var installedItem in installed)
        {
            if (MetaData(installedItem).EntityPrototype?.ID != prototype)
                continue;

            Popup.PopupClient(Loc.GetString("mech-duplicate-installed-popup"), user, user);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Starts the install do-after with the provided insert event.
    /// </summary>
    protected void StartInstallDoAfter(EntityUid user, EntityUid item, EntityUid mech, float duration, SimpleDoAfterEvent insertEvent)
    {
        Popup.PopupPredicted(Loc.GetString("mech-install-begin-popup",
                ("user", Identity.Entity(user, EntityManager)),
                ("item", item)),
            user,
            user);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, duration, insertEvent, item, target: mech, used: item)
        {
            BreakOnMove = true,
        };

        DoAfter.TryStartDoAfter(doAfterEventArgs);
    }
}
