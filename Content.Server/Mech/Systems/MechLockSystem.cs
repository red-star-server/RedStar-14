// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Server.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Server-side system for mech lock functionality
/// </summary>
public sealed class MechLockSystem : SharedMechLockSystem
{
    [Dependency] private readonly IdCardSystem _idCard = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, MechDnaLockRegisterMessage>(OnDnaLockRegister);
        SubscribeLocalEvent<MechComponent, MechDnaLockToggleMessage>(OnDnaLockToggle);
        SubscribeLocalEvent<MechComponent, MechDnaLockResetMessage>(OnDnaLockReset);
        SubscribeLocalEvent<MechComponent, MechCardLockRegisterMessage>(OnCardLockRegister);
        SubscribeLocalEvent<MechComponent, MechCardLockToggleMessage>(OnCardLockToggle);
        SubscribeLocalEvent<MechComponent, MechCardLockResetMessage>(OnCardLockReset);
    }

    /// <summary>
    /// Handles DNA lock registration
    /// </summary>
    private void OnDnaLockRegister(EntityUid uid, MechComponent component, MechDnaLockRegisterMessage args)
    {
        if (!TryComp<MechLockComponent>(uid, out var lockComp) || !CanUseLockControls(uid, args.Actor, lockComp))
            return;

        TryRegisterLock(uid, args.Actor, MechLockType.Dna, lockComp);
    }

    /// <summary>
    /// Handles DNA lock toggle
    /// </summary>
    private void OnDnaLockToggle(EntityUid uid, MechComponent component, MechDnaLockToggleMessage args)
    {
        if (!TryComp<MechLockComponent>(uid, out var lockComp) || !CanUseLockControls(uid, args.Actor, lockComp))
            return;

        if (TryToggleLock(uid, args.Actor, MechLockType.Dna, lockComp))
        {
            var (_, isActive, _) = GetLockState(MechLockType.Dna, lockComp);
            ShowLockMessage(uid, args.Actor, lockComp, isActive);
        }
    }

    /// <summary>
    /// Handles DNA lock reset
    /// </summary>
    private void OnDnaLockReset(EntityUid uid, MechComponent component, MechDnaLockResetMessage args)
    {
        if (!TryComp<MechLockComponent>(uid, out var lockComp) || !CanUseLockControls(uid, args.Actor, lockComp))
            return;

        TryResetLock(uid, args.Actor, MechLockType.Dna, lockComp);
    }

    /// <summary>
    /// Handles card lock registration
    /// </summary>
    private void OnCardLockRegister(EntityUid uid, MechComponent component, MechCardLockRegisterMessage args)
    {
        if (!TryComp<MechLockComponent>(uid, out var lockComp) || !CanUseLockControls(uid, args.Actor, lockComp))
            return;

        TryRegisterLock(uid, args.Actor, MechLockType.Card, lockComp);
    }

    /// <summary>
    /// Handles card lock toggle
    /// </summary>
    private void OnCardLockToggle(EntityUid uid, MechComponent component, MechCardLockToggleMessage args)
    {
        if (!TryComp<MechLockComponent>(uid, out var lockComp) || !CanUseLockControls(uid, args.Actor, lockComp))
            return;

        if (TryToggleLock(uid, args.Actor, MechLockType.Card, lockComp))
        {
            var (_, isActive, _) = GetLockState(MechLockType.Card, lockComp);
            ShowLockMessage(uid, args.Actor, lockComp, isActive);
        }
    }

    /// <summary>
    /// Handles card lock reset
    /// </summary>
    private void OnCardLockReset(EntityUid uid, MechComponent component, MechCardLockResetMessage args)
    {
        if (!TryComp<MechLockComponent>(uid, out var lockComp) || !CanUseLockControls(uid, args.Actor, lockComp))
            return;

        TryResetLock(uid, args.Actor, MechLockType.Card, lockComp);
    }

    protected override void UpdateMechUI(EntityUid uid)
    {
        var ev = new UpdateMechUiEvent();
        RaiseLocalEvent(uid, ev);
    }

    protected override bool TryFindIdCard(EntityUid user, out Entity<IdCardComponent> idCard)
    {
        return _idCard.TryFindIdCard(user, out idCard);
    }

    private bool CanUseLockControls(EntityUid mech, EntityUid user, MechLockComponent lockComp)
    {
        return user != EntityUid.Invalid && CheckLockControlAccessWithFeedback(mech, user, lockComp);
    }
}
