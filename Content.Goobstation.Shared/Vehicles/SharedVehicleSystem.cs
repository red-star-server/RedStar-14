// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 Scruq445 <storchdamien@gmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 fishbait <gnesse@gmail.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared._vg.TileMovement;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Movement.Components;
using Content.Shared.Vehicle.Components;
using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Vehicles;

public abstract partial class SharedVehicleSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId HornActionId = "ActionHorn";
    private static readonly EntProtoId SirenActionId = "ActionSiren";

    public override void Initialize()
    {
        base.Initialize();

        // RS14-start
        SubscribeLocalEvent<VehicleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VehicleComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<VehicleComponent, HornActionEvent>(OnHorn);
        SubscribeLocalEvent<VehicleComponent, SirenActionEvent>(OnSiren);
        SubscribeLocalEvent<VehicleComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEject);
        SubscribeLocalEvent<VehicleComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<VehicleComponent, DamageChangedEvent>(OnRepair);
        SubscribeLocalEvent<VehicleComponent, VehicleCanRunEvent>(OnCanRun);
        SubscribeLocalEvent<VehicleComponent, VehicleCanRunUpdatedEvent>(OnCanRunUpdated);
        SubscribeLocalEvent<VehicleComponent, VehicleOperatorSetEvent>(OnOperatorSet);
        // RS14-end
    }

    private void OnInit(Entity<VehicleComponent> ent, ref ComponentInit args)
    {
        _ambientSound.SetAmbience(ent, false);
    }

    private void OnStrapAttempt(Entity<VehicleComponent> ent, ref StrapAttemptEvent args)
    {
        if (ent.Comp.Operator != null)
        {
            args.Cancelled = true;
            return;
        }
    }

    private void OnOperatorSet(Entity<VehicleComponent> ent, ref VehicleOperatorSetEvent args)
    {
        if (args.OldOperator != null)
            CleanupOperator(args.OldOperator.Value, ent);

        if (args.NewOperator == null)
        {
            _appearance.SetData(ent, VehicleVisuals.HasOperator, false);
            return;
        }

        AddActions(args.NewOperator.Value, ent);
        SetupOverlay(ent);
        _appearance.SetData(ent, VehicleVisuals.HasOperator, true);

        if (HasComp<TileMovementComponent>(args.NewOperator.Value))
            EnsureComp<TileMovementComponent>(ent);
    }

    private void OnHorn(Entity<VehicleComponent> ent, ref HornActionEvent args)
    {
        if (args.Handled
            || ent.Comp.Operator != args.Performer
            || ent.Comp.HornSound == null)
            return;

        _audio.PlayPvs(ent.Comp.HornSound, ent);
        args.Handled = true;
    }

    private void OnSiren(Entity<VehicleComponent> ent, ref SirenActionEvent args)
    {
        if (args.Handled
            || ent.Comp.Operator != args.Performer
            || ent.Comp.SirenSound == null)
            return;

        ent.Comp.SirenStream = ent.Comp.SirenEnabled
            ? _audio.Stop(ent.Comp.SirenStream)
            : _audio.PlayPvs(ent.Comp.SirenSound, ent)?.Entity;
        ent.Comp.SirenEnabled = !ent.Comp.SirenEnabled;
        args.Handled = true;
    }

    private void OnItemSlotEject(Entity<VehicleComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (!ent.Comp.PreventEjectOfKey
            || ent.Comp.Operator == null
            || args.Slot.ID != ent.Comp.KeySlot
            || args.User == ent.Comp.Operator)
            return;

        args.Cancelled = true;
    }

    private void OnBreak(Entity<VehicleComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.IsBroken = true;
        _ambientSound.SetAmbience(ent, false);
        _actionBlocker.UpdateCanMove(ent);

        if (ent.Comp.Operator != null)
            _buckle.TryUnbuckle(ent.Comp.Operator.Value, ent.Comp.Operator.Value);
    }

    private void OnRepair(Entity<VehicleComponent> ent, ref DamageChangedEvent args)
    {
        if (!ent.Comp.IsBroken || args.Damageable.TotalDamage != FixedPoint2.Zero)
            return;

        ent.Comp.IsBroken = false;
        SetEngineAmbience(ent, CanRun(ent));
        _actionBlocker.UpdateCanMove(ent);
    }

    private void OnCanRun(Entity<VehicleComponent> ent, ref VehicleCanRunEvent args)
    {
        if (ent.Comp.IsBroken)
            args = args with { CanRun = false };
    }

    private void OnCanRunUpdated(Entity<VehicleComponent> ent, ref VehicleCanRunUpdatedEvent args)
    {
        SetEngineAmbience(ent, args.CanRun);
    }

    private bool CanRun(Entity<VehicleComponent> ent)
    {
        var ev = new VehicleCanRunEvent(ent);
        RaiseLocalEvent(ent, ref ev);
        return ev.CanRun;
    }

    private void SetEngineAmbience(Entity<VehicleComponent> ent, bool canRun)
    {
        // The legacy Goob vehicle system only started engine ambience after a valid key was inserted.
        _ambientSound.SetAmbience(ent, canRun && HasComp<GenericKeyedVehicleComponent>(ent));
    }

    private void AddActions(EntityUid operatorUid, Entity<VehicleComponent> vehicle)
    {
        if (vehicle.Comp.HornSound != null)
            _actions.AddAction(operatorUid, ref vehicle.Comp.HornAction, HornActionId, vehicle);
        if (vehicle.Comp.SirenSound != null)
            _actions.AddAction(operatorUid, ref vehicle.Comp.SirenAction, SirenActionId, vehicle);
    }

    private void SetupOverlay(Entity<VehicleComponent> ent)
    {
        if (ent.Comp.OverlayPrototype == null || ent.Comp.ActiveOverlay != null)
            return;

        var overlay = EntityManager.SpawnEntity(ent.Comp.OverlayPrototype, Transform(ent).Coordinates);
        _transform.SetParent(overlay, ent);
        _transform.SetLocalPosition(overlay, Vector2.Zero);
        _transform.SetLocalRotation(overlay, Angle.Zero);
        ent.Comp.ActiveOverlay = overlay;
    }

    private void CleanupOperator(EntityUid operatorUid, Entity<VehicleComponent> vehicle)
    {
        if (vehicle.Comp.SirenEnabled || vehicle.Comp.SirenStream != null)
        {
            vehicle.Comp.SirenStream = _audio.Stop(vehicle.Comp.SirenStream);
            vehicle.Comp.SirenEnabled = false;
        }

        if (vehicle.Comp.ActiveOverlay != null)
        {
            EntityManager.QueueDeleteEntity(vehicle.Comp.ActiveOverlay.Value);
            vehicle.Comp.ActiveOverlay = null;
        }

        RemoveOperatorAction(operatorUid, ref vehicle.Comp.HornAction);
        RemoveOperatorAction(operatorUid, ref vehicle.Comp.SirenAction);

        if (HasComp<TileMovementComponent>(vehicle))
            RemComp<TileMovementComponent>(vehicle);
    }

    private void RemoveOperatorAction(EntityUid operatorUid, ref EntityUid? action)
    {
        if (action is not { } actionUid)
            return;

        action = null;

        if (!TryComp<ActionComponent>(actionUid, out var actionComp) || actionComp.AttachedEntity != operatorUid)
            return;

        _actions.RemoveAction(operatorUid, (actionUid, actionComp));
    }
}
