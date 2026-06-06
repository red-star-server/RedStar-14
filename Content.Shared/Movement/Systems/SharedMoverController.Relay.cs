// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 0x6273 <0x40@keemail.me>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Plykiya <58439124+Plykiya@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 plykiya <plykiya@protonmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<RelayInputMoverComponent, ComponentShutdown>(OnRelayShutdown);
        SubscribeLocalEvent<MovementRelayTargetComponent, ComponentShutdown>(OnTargetRelayShutdown);
        SubscribeLocalEvent<MovementRelayTargetComponent, AfterAutoHandleStateEvent>(OnAfterRelayTargetState);
        SubscribeLocalEvent<RelayInputMoverComponent, AfterAutoHandleStateEvent>(OnAfterRelayState);
        // RS14-start
        SubscribeLocalEvent<RelayInputMoverComponent, CanMoveUpdatedEvent>(OnRelayCanMoveUpdated);
        SubscribeLocalEvent<InputMoverComponent, CanMoveUpdatedEvent>(OnInputMoverCanMoveUpdated);
        SubscribeLocalEvent<RelayInputMoverComponent, GetDoAfterUserEvent>(OnGetDoAfterUser);
        // RS14-end
    }

    private void OnAfterRelayTargetState(Entity<MovementRelayTargetComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
    }

    private void OnAfterRelayState(Entity<RelayInputMoverComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
    }

    // RS14-start
    private void OnRelayCanMoveUpdated(Entity<RelayInputMoverComponent> ent, ref CanMoveUpdatedEvent args)
    {
        if (MoverQuery.TryComp(ent.Comp.RelayEntity, out var targetMover))
        {
            if (targetMover.CanMove != args.CanMove)
            {
                targetMover.CanMove = args.CanMove;
                Dirty(ent.Comp.RelayEntity, targetMover);
            }

            if (!args.CanMove)
                SetMoveInput((ent.Comp.RelayEntity, targetMover), MoveButtons.None);

            var relayEvent = new CanMoveUpdatedEvent(args.CanMove);
            RaiseLocalEvent(ent.Comp.RelayEntity, ref relayEvent);
        }

        if (!args.CanMove && MoverQuery.TryComp(ent.Owner, out var sourceMover))
            SetMoveInput((ent.Owner, sourceMover), MoveButtons.None);
    }

    protected virtual void OnInputMoverCanMoveUpdated(Entity<InputMoverComponent> ent, ref CanMoveUpdatedEvent args)
    {
        if (!args.CanMove)
            SetMoveInput(ent, MoveButtons.None);
    }

    private void OnGetDoAfterUser(Entity<RelayInputMoverComponent> ent, ref GetDoAfterUserEvent args)
    {
        if (ent.Comp.RelayEntity.IsValid() && Exists(ent.Comp.RelayEntity))
            args.User = ent.Comp.RelayEntity;
    }
    // RS14-end

    /// <summary>
    ///     Sets the relay entity and marks the component as dirty. This only exists because people have previously
    ///     forgotten to Dirty(), so fuck you, you have to use this method now.
    /// </summary>
    public void SetRelay(EntityUid uid, EntityUid relayEntity)
    {
        if (uid == relayEntity)
        {
            Log.Error($"An entity attempted to relay movement to itself. Entity:{ToPrettyString(uid)}");
            return;
        }

        var component = EnsureComp<RelayInputMoverComponent>(uid);
        var oldEffectiveMover = GetEffectiveMover((uid, component)); // RS14
        if (component.RelayEntity == relayEntity)
            return;

        if (TryComp(component.RelayEntity, out MovementRelayTargetComponent? oldTarget))
        {
            oldTarget.Source = EntityUid.Invalid;
            RemComp(component.RelayEntity, oldTarget);
            PhysicsSystem.UpdateIsPredicted(component.RelayEntity);
        }

        var targetComp = EnsureComp<MovementRelayTargetComponent>(relayEntity);
        if (TryComp(targetComp.Source, out RelayInputMoverComponent? oldRelay))
        {
            var oldRelayEffectiveMover = GetEffectiveMover((targetComp.Source, oldRelay)); // RS14
            if (MoverQuery.TryComp(oldRelayEffectiveMover, out var oldRelayMover))
                SetMoveInput((oldRelayEffectiveMover, oldRelayMover), MoveButtons.None);

            oldRelay.RelayEntity = EntityUid.Invalid;
            RemComp(targetComp.Source, oldRelay);
            PhysicsSystem.UpdateIsPredicted(targetComp.Source);
            RaiseEffectiveMoverChanged(targetComp.Source, oldRelayEffectiveMover, targetComp.Source); // RS14
        }

        PhysicsSystem.UpdateIsPredicted(uid);
        PhysicsSystem.UpdateIsPredicted(relayEntity);
        component.RelayEntity = relayEntity;
        targetComp.Source = uid;
        Dirty(uid, component);
        Dirty(relayEntity, targetComp);
        _blocker.UpdateCanMove(uid);
        // RS14-start
        UpdateMoverStatus((relayEntity, null, targetComp));
        RaiseEffectiveMoverChanged(uid, oldEffectiveMover, relayEntity);
        // RS14-end
    }

    // RS14-start
    /// <summary>
    /// Returns the entity whose movement should be treated as the effective movement source for <paramref name="mover"/>.
    /// </summary>
    public EntityUid GetEffectiveMover(Entity<RelayInputMoverComponent?> mover)
    {
        if (RelayQuery.Resolve(mover.Owner, ref mover.Comp, false)
            && mover.Comp.RelayEntity.IsValid()
            && Exists(mover.Comp.RelayEntity))
        {
            return mover.Comp.RelayEntity;
        }

        return mover.Owner;
    }

    public EntityUid GetEffectiveMover(EntityUid uid)
    {
        return GetEffectiveMover((uid, null));
    }
    // RS14-end

    private void OnRelayShutdown(Entity<RelayInputMoverComponent> entity, ref ComponentShutdown args)
    {
        var oldEffectiveMover = entity.Comp.RelayEntity; // RS14
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
        if (oldEffectiveMover.IsValid())
            PhysicsSystem.UpdateIsPredicted(oldEffectiveMover);

        if (MoverQuery.TryComp(oldEffectiveMover, out var inputMover))
            SetMoveInput((oldEffectiveMover, inputMover), MoveButtons.None);

        if (Timing.ApplyingState)
            return;

        if (RelayTargetQuery.TryComp(oldEffectiveMover, out var target) && target.LifeStage <= ComponentLifeStage.Running)
            RemComp(oldEffectiveMover, target);

        _blocker.UpdateCanMove(entity.Owner);
        if (oldEffectiveMover.IsValid())
            RaiseEffectiveMoverChanged(entity.Owner, oldEffectiveMover, entity.Owner); // RS14
    }

    protected virtual void OnTargetRelayShutdown(Entity<MovementRelayTargetComponent> entity, ref ComponentShutdown args)
    {
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
        PhysicsSystem.UpdateIsPredicted(entity.Comp.Source);

        if (Timing.ApplyingState)
            return;

        // RS14-start
        if (MoverQuery.TryComp(entity.Owner, out var inputMover))
            SetMoveInput((entity.Owner, inputMover), MoveButtons.None);

        if (TryComp(entity.Comp.Source, out RelayInputMoverComponent? relay) && relay.LifeStage <= ComponentLifeStage.Running)
            RemComp(entity.Comp.Source, relay);
    }

    protected virtual void UpdateMoverStatus(Entity<InputMoverComponent?, MovementRelayTargetComponent?> ent) { }

    private void RaiseEffectiveMoverChanged(EntityUid uid, EntityUid oldMover, EntityUid newMover)
    {
        if (oldMover == newMover)
            return;

        var ev = new EffectiveMoverChangedEvent(oldMover, newMover);
        RaiseLocalEvent(uid, ref ev);
    }
    // RS14-end
}
