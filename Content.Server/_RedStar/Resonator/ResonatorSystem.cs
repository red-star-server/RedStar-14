// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Destructible;
using Content.Server.Gatherable;
using Content.Server.Gatherable.Components;
using Content.Server.PowerCell;
using Content.Shared._RedStar.Resonator.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mining;
using Content.Shared.Mining.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Resonator;

public sealed class ResonatorSystem : EntitySystem
{
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly GatherableSystem _gatherable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    private readonly Dictionary<TileKey, EntityUid> _activeFields = new();
    private readonly HashSet<TileKey> _rearmingTiles = new();
    private readonly List<EntityUid> _toBurst = new();
    private readonly Queue<TileKey> _chainQueue = new();
    private readonly HashSet<TileKey> _visitedTiles = new();
    private readonly HashSet<EntityUid> _visitedTargets = new();
    private readonly List<EntityUid> _chainTargets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResonatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ResonatorComponent, GetVerbsEvent<Verb>>(OnResonatorGetVerbs);
        SubscribeLocalEvent<ResonatorComponent, ComponentRemove>(OnResonatorRemove);

        SubscribeLocalEvent<ResonanceFieldComponent, ComponentRemove>(OnFieldRemove);
    }

    private void OnAfterInteract(Entity<ResonatorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (TryPlaceField(ent, args.User, args.Target))
            args.Handled = true;
    }

    private void OnResonatorGetVerbs(Entity<ResonatorComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("resonator-verb-toggle-mode"),
            Category = VerbCategory.Interaction,
            Act = () => ToggleMode(ent, user),
        });

        PruneFields(ent.Comp);
        if (ent.Comp.ActiveFields.Count > 0)
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("resonator-verb-detonate-fields"),
                Category = VerbCategory.Interaction,
                Act = () => DetonateActiveFields(ent),
            });
        }
    }

    private void ToggleMode(Entity<ResonatorComponent> ent, EntityUid user)
    {
        ent.Comp.Mode = ent.Comp.Mode == ResonatorDetonationMode.Timer
            ? ResonatorDetonationMode.Manual
            : ResonatorDetonationMode.Timer;

        var mode = Loc.GetString(GetModeLoc(ent.Comp.Mode));
        Popup(user, ent, "resonator-popup-mode-set", ("mode", mode));
    }

    private void DetonateActiveFields(Entity<ResonatorComponent> resonator)
    {
        PruneFields(resonator.Comp);
        if (resonator.Comp.ActiveFields.Count == 0)
            return;

        _toBurst.Clear();
        _toBurst.AddRange(resonator.Comp.ActiveFields);

        foreach (var fieldUid in _toBurst)
        {
            if (TryComp<ResonanceFieldComponent>(fieldUid, out var field))
                BurstField((fieldUid, field));
        }

        _toBurst.Clear();
    }

    private bool TryPlaceField(Entity<ResonatorComponent> resonator, EntityUid user, EntityUid? target)
    {
        if (!TryComp<UseDelayComponent>(resonator, out var useDelay))
            return false;

        if (_delay.IsDelayed((resonator.Owner, useDelay)))
            return false;

        if (target != null && TryComp<ResonanceFieldComponent>(target.Value, out var targetField))
        {
            BurstField((target.Value, targetField));
            ResetUseDelay(resonator.Owner, useDelay);
            return true;
        }

        if (!TryFindMineableTarget(target, out var mineable, out var tile))
            return false;

        if (tile is not { } tileRef)
            return false;

        var key = new TileKey(tileRef.GridUid, tileRef.GridIndices);
        if (_activeFields.TryGetValue(key, out var existing) && !TerminatingOrDeleted(existing))
        {
            if (TryComp<ResonanceFieldComponent>(existing, out var existingField))
            {
                BurstField((existing, existingField));
                ResetUseDelay(resonator.Owner, useDelay);
                return true;
            }

            _activeFields.Remove(key);
        }

        PruneFields(resonator.Comp);
        if (resonator.Comp.ActiveFields.Count >= resonator.Comp.MaxFields)
            return false;

        if (_rearmingTiles.Contains(key))
            return false;

        if (!_powerCell.TryUseActivatableCharge(resonator.Owner, user: user))
            return false;

        var coords = _turf.GetTileCenter(tileRef);
        var fieldUid = Spawn(resonator.Comp.FieldPrototype, coords);
        var field = EnsureComp<ResonanceFieldComponent>(fieldUid);
        field.Resonator = resonator;
        field.Creator = user;
        field.Target = mineable;
        field.TargetOre = Comp<OreVeinComponent>(mineable).CurrentOre;
        field.MaxChainTargets = resonator.Comp.MaxChainTargets;
        field.BurstSound = resonator.Comp.BurstSound;
        field.BurstEffectPrototype = resonator.Comp.BurstEffectPrototype;
        field.GridUid = tileRef.GridUid;
        field.GridIndices = tileRef.GridIndices;
        field.TileRearmDelay = resonator.Comp.TileRearmDelay;

        _activeFields[key] = fieldUid;
        resonator.Comp.ActiveFields.Add(fieldUid);

        if (resonator.Comp.PlaceSound != null)
            _audio.PlayPvs(resonator.Comp.PlaceSound, coords);

        if (resonator.Comp.Mode == ResonatorDetonationMode.Timer)
            Timer.Spawn(resonator.Comp.TimerDelay, () => TryBurstField(fieldUid));

        ResetUseDelay(resonator.Owner, useDelay);
        return true;
    }

    private bool TryFindMineableTarget(EntityUid? target, out EntityUid mineable, out TileRef? tile)
    {
        tile = null;
        mineable = EntityUid.Invalid;

        if (target == null ||
            !TryComp<OreVeinComponent>(target.Value, out var oreVein) ||
            oreVein.CurrentOre == null ||
            !IsMineable(target.Value))
        {
            return false;
        }

        mineable = target.Value;
        tile = _turf.GetTileRef(Transform(target.Value).Coordinates);
        return tile != null && !tile.Value.Tile.IsEmpty;
    }

    private bool IsMineable(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return false;

        return HasComp<OreVeinComponent>(uid) ||
               HasComp<GatherableComponent>(uid) &&
               TryComp<DamageableComponent>(uid, out var damageable) &&
               damageable.DamageModifierSetId == "Rock";
    }

    private void BurstField(Entity<ResonanceFieldComponent> field)
    {
        if (field.Comp.Bursting)
            return;

        field.Comp.Bursting = true;

        var coords = Transform(field).Coordinates;
        if (field.Comp.GridUid != null)
            RearmTile(new TileKey(field.Comp.GridUid.Value, field.Comp.GridIndices), field.Comp.TileRearmDelay);

        if (field.Comp.BurstSound != null)
            _audio.PlayPvs(field.Comp.BurstSound, coords);

        if (field.Comp.BurstEffectPrototype != null)
            Spawn(field.Comp.BurstEffectPrototype.Value, coords);

        GatherTargets(field);
        QueueDel(field);
    }

    private void TryBurstField(EntityUid uid)
    {
        if (TryComp<ResonanceFieldComponent>(uid, out var field))
            BurstField((uid, field));
    }

    private void RearmTile(TileKey key, TimeSpan delay)
    {
        _rearmingTiles.Add(key);
        Timer.Spawn(delay, () => _rearmingTiles.Remove(key));
    }

    private void GatherTargets(Entity<ResonanceFieldComponent> field)
    {
        _chainTargets.Clear();
        _visitedTargets.Clear();
        BuildChainTargets(field);

        foreach (var target in _chainTargets)
        {
            GatherTarget(field, target);
        }

        _chainTargets.Clear();
        _visitedTargets.Clear();
    }

    private void BuildChainTargets(Entity<ResonanceFieldComponent> field)
    {
        var limit = Math.Max(1, field.Comp.MaxChainTargets);

        if (field.Comp.TargetOre == null)
            return;

        if (field.Comp.Target is { } primaryTarget && IsMatchingVein(primaryTarget, field.Comp.TargetOre.Value))
            AddChainTarget(primaryTarget);

        if (field.Comp.GridUid == null ||
            !TryComp<MapGridComponent>(field.Comp.GridUid.Value, out var grid))
        {
            return;
        }

        _chainQueue.Clear();
        _visitedTiles.Clear();

        var start = new TileKey(field.Comp.GridUid.Value, field.Comp.GridIndices);
        _chainQueue.Enqueue(start);
        _visitedTiles.Add(start);

        while (_chainQueue.TryDequeue(out var tile) && _chainTargets.Count < limit)
        {
            if (!TryAddMineablesOnTile(tile, grid, field.Comp.TargetOre.Value, limit))
                continue;

            EnqueueNeighbor(tile, new Vector2i(1, 0));
            EnqueueNeighbor(tile, new Vector2i(-1, 0));
            EnqueueNeighbor(tile, new Vector2i(0, 1));
            EnqueueNeighbor(tile, new Vector2i(0, -1));
        }

        _chainQueue.Clear();
        _visitedTiles.Clear();
    }

    private bool TryAddMineablesOnTile(
        TileKey tile,
        MapGridComponent grid,
        ProtoId<OrePrototype> targetOre,
        int limit)
    {
        if (!_map.TryGetTileRef(tile.GridUid, grid, tile.Indices, out _))
            return false;

        var foundMineable = false;
        var anchored = _map.GetAnchoredEntitiesEnumerator(tile.GridUid, grid, tile.Indices);
        while (anchored.MoveNext(out var uid))
        {
            if (!IsMatchingVein(uid.Value, targetOre))
                continue;

            foundMineable = true;
            AddChainTarget(uid.Value);

            if (_chainTargets.Count >= limit)
                break;
        }

        return foundMineable;
    }

    private bool IsMatchingVein(EntityUid uid, ProtoId<OrePrototype> targetOre)
    {
        return IsMineable(uid) &&
               TryComp<OreVeinComponent>(uid, out var oreVein) &&
               oreVein.CurrentOre == targetOre;
    }

    private void AddChainTarget(EntityUid target)
    {
        if (_visitedTargets.Add(target))
            _chainTargets.Add(target);
    }

    private void EnqueueNeighbor(TileKey tile, Vector2i offset)
    {
        var neighbor = new TileKey(tile.GridUid, tile.Indices + offset);
        if (_visitedTiles.Add(neighbor))
            _chainQueue.Enqueue(neighbor);
    }

    private void GatherTarget(Entity<ResonanceFieldComponent> field, EntityUid target)
    {
        if (TryComp<GatherableComponent>(target, out var gatherable))
        {
            _gatherable.Gather(target, field.Comp.Creator, gatherable);
            return;
        }

        if (!HasComp<OreVeinComponent>(target))
            return;

        _destructible.DestroyEntity(target);
    }

    private void OnFieldRemove(Entity<ResonanceFieldComponent> field, ref ComponentRemove args)
    {
        if (field.Comp.GridUid != null)
            _activeFields.Remove(new TileKey(field.Comp.GridUid.Value, field.Comp.GridIndices));

        if (field.Comp.Resonator is { } resonator && TryComp<ResonatorComponent>(resonator, out var comp))
            comp.ActiveFields.Remove(field.Owner);
    }

    private void OnResonatorRemove(Entity<ResonatorComponent> resonator, ref ComponentRemove args)
    {
        PruneFields(resonator.Comp);

        _toBurst.Clear();
        _toBurst.AddRange(resonator.Comp.ActiveFields);
        resonator.Comp.ActiveFields.Clear();

        foreach (var field in _toBurst)
        {
            QueueDel(field);
        }

        _toBurst.Clear();
    }

    private void PruneFields(ResonatorComponent comp)
    {
        for (var i = comp.ActiveFields.Count - 1; i >= 0; i--)
        {
            if (TerminatingOrDeleted(comp.ActiveFields[i]))
                comp.ActiveFields.RemoveAt(i);
        }
    }

    private void ResetUseDelay(EntityUid uid, UseDelayComponent useDelay)
    {
        _delay.TryResetDelay((uid, useDelay));
    }

    private static string GetModeLoc(ResonatorDetonationMode mode)
    {
        return mode switch
        {
            ResonatorDetonationMode.Manual => "resonator-mode-manual",
            _ => "resonator-mode-timer",
        };
    }

    private void Popup(EntityUid user, EntityUid source, string message, params (string, object)[] args)
    {
        var text = Loc.GetString(message, args);
        _popup.PopupEntity(text, source, user);
    }

    private readonly record struct TileKey(EntityUid GridUid, Vector2i Indices);
}
