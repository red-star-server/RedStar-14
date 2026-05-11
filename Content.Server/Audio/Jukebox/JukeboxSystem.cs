// SPDX-FileCopyrightText: 2024 12rabbits <53499656+12rabbits@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Alzore <140123969+Blackern5000@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 ArtisticRoomba <145879011+ArtisticRoomba@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Brandon Hu <103440971+Brandon-Huu@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Dimastra <65184747+Dimastra@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Dimastra <dimastra@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Emisse <99158783+Emisse@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Eoin Mcloughlin <helloworld@eoinrul.es>
// SPDX-FileCopyrightText: 2024 IProduceWidgets <107586145+IProduceWidgets@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 JIPDawg <51352440+JIPDawg@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 JIPDawg <JIPDawg93@gmail.com>
// SPDX-FileCopyrightText: 2024 Mervill <mervills.email@gmail.com>
// SPDX-FileCopyrightText: 2024 Moomoobeef <62638182+Moomoobeef@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 PJBot <pieterjan.briers+bot@gmail.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 PopGamer46 <yt1popgamer@gmail.com>
// SPDX-FileCopyrightText: 2024 PursuitInAshes <pursuitinashes@gmail.com>
// SPDX-FileCopyrightText: 2024 QueerNB <176353696+QueerNB@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Saphire Lattice <lattice@saphi.re>
// SPDX-FileCopyrightText: 2024 ShadowCommander <10494922+ShadowCommander@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Simon <63975668+Simyon264@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Spessmann <156740760+Spessmann@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2024 Thomas <87614336+Aeshus@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Winkarst <74284083+Winkarst-cpu@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2024 eoineoineoin <github@eoinrul.es>
// SPDX-FileCopyrightText: 2024 github-actions[bot] <41898282+github-actions[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 iNVERTED <alextjorgensen@gmail.com>
// SPDX-FileCopyrightText: 2024 lzk <124214523+lzk228@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 slarticodefast <161409025+slarticodefast@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 stellar-novas <stellar_novas@riseup.net>
// SPDX-FileCopyrightText: 2024 themias <89101928+themias@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Audio.Jukebox;
using Content.Shared.Audio.Jukebox;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using JukeboxComponent = Content.Shared.Audio.Jukebox.JukeboxComponent;

namespace Content.Server.Audio.Jukebox;


public sealed class JukeboxSystem : SharedJukeboxSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    // RS14-start
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly Dictionary<EntityUid, PlaybackHistoryState> _playbackStates = new();
    private List<JukeboxPrototype>? _cachedOrderedSongs;

    private sealed class PlaybackHistoryState
    {
        public readonly List<ProtoId<JukeboxPrototype>> History = new();
        public int HistoryIndex = -1;
    }
    // RS14-end
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, JukeboxSelectedMessage>(OnJukeboxSelected);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPauseMessage>(OnJukeboxPause);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetTimeMessage>(OnJukeboxSetTime);
        // RS14-start
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetVolumeMessage>(OnJukeboxSetVolume);
        SubscribeLocalEvent<JukeboxComponent, JukeboxNextMessage>(OnJukeboxNext);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPreviousMessage>(OnJukeboxPrevious);
        SubscribeLocalEvent<JukeboxComponent, JukeboxShuffleMessage>(OnJukeboxShuffle);
        SubscribeLocalEvent<JukeboxComponent, JukeboxRepeatMessage>(OnJukeboxRepeat);
        // RS14-end
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<JukeboxComponent, PowerChangedEvent>(OnPowerChanged);

        // RS14-start
        _protoManager.PrototypesReloaded += OnPrototypesReloaded;
        RebuildSongCache();
        // RS14-end
    }

    private void OnComponentInit(EntityUid uid, JukeboxComponent component, ComponentInit args)
    {
        if (HasComp<ApcPowerReceiverComponent>(uid))
        {
            TryUpdateVisualState(uid, component);
        }
    }

    private void OnJukeboxPlay(EntityUid uid, JukeboxComponent component, ref JukeboxPlayingMessage args)
    {
        if (Exists(component.AudioStream))
        {
            Audio.SetState(component.AudioStream, AudioState.Playing);
            // RS14-start
            Dirty(uid, component);
        }
        else
        {
            var selectedSong = component.SelectedSongId;

            if (!TryResolveSong(selectedSong, out _))
            {
                selectedSong = component.ShuffleEnabled
                    ? PickShuffledSong(component.SelectedSongId)
                    : GetAdjacentSong(component.SelectedSongId, 1);
            }

            if (selectedSong == null || !StartSong(uid, component, selectedSong.Value, updateHistory: true))
                return;
            // RS14-end
        }
    }

    private void OnJukeboxPause(Entity<JukeboxComponent> ent, ref JukeboxPauseMessage args)
    {
        Audio.SetState(ent.Comp.AudioStream, AudioState.Paused);
    }

    private void OnJukeboxSetTime(EntityUid uid, JukeboxComponent component, JukeboxSetTimeMessage args)
    {
        if (TryComp(args.Actor, out ActorComponent? actorComp))
        {
            var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
            Audio.SetPlaybackPosition(component.AudioStream, args.SongTime + offset);
        }
    }
    // RS14-start
    private void OnJukeboxSetVolume(EntityUid uid, JukeboxComponent component, JukeboxSetVolumeMessage args)
    {
        component.Volume = JukeboxVolume.Clamp(args.Volume);
        Dirty(uid, component);
    }

    private void OnJukeboxNext(EntityUid uid, JukeboxComponent component, ref JukeboxNextMessage args)
    {
        var startPlayback = HasActiveStream(component.AudioStream);
        var songId = ResolveNextSong(uid, component, out var fromHistory);
        if (songId == null)
            return;

        SelectOrPlaySong(uid, component, songId.Value, startPlayback, updateHistory: !fromHistory);
    }

    private void OnJukeboxPrevious(EntityUid uid, JukeboxComponent component, ref JukeboxPreviousMessage args)
    {
        var startPlayback = HasActiveStream(component.AudioStream);
        var songId = ResolvePreviousSong(uid, component, out var fromHistory);
        if (songId == null)
            return;

        SelectOrPlaySong(uid, component, songId.Value, startPlayback, updateHistory: !fromHistory);
    }

    private void OnJukeboxShuffle(EntityUid uid, JukeboxComponent component, ref JukeboxShuffleMessage args)
    {
        component.ShuffleEnabled = args.Enabled;
        Dirty(uid, component);
    }

    private void OnJukeboxRepeat(EntityUid uid, JukeboxComponent component, ref JukeboxRepeatMessage args)
    {
        component.RepeatEnabled = args.Enabled;
        Dirty(uid, component);
    }
    // RS14-end
    private void OnPowerChanged(Entity<JukeboxComponent> entity, ref PowerChangedEvent args)
    {
        TryUpdateVisualState(entity);

        if (!this.IsPowered(entity.Owner, EntityManager))
        {
            Stop(entity);
        }
    }

    private void OnJukeboxStop(Entity<JukeboxComponent> entity, ref JukeboxStopMessage args)
    {
        Stop(entity);
    }

    private void Stop(Entity<JukeboxComponent> entity)
    {
        Audio.SetState(entity.Comp.AudioStream, AudioState.Stopped);
        Dirty(entity);
    }
    // RS14-start
    private void OnJukeboxSelected(EntityUid uid, JukeboxComponent component, JukeboxSelectedMessage args)
    {
        if (HasActiveStream(component.AudioStream))
        {
            StartSong(uid, component, args.SongId, updateHistory: true);
            ShowSelectionVisual(uid, component);
            return;
        }

        SelectSong(uid, component, args.SongId);
    }
    // RS14-end
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<JukeboxComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Selecting)
            {
                comp.SelectAccumulator += frameTime;
                if (comp.SelectAccumulator >= 0.5f)
                {
                    comp.SelectAccumulator = 0f;
                    comp.Selecting = false;

                    TryUpdateVisualState(uid, comp);
                }
            }
            // RS14-start
            if (!TryGetSongLength(comp.SelectedSongId, out var length) ||
                !TryComp(comp.AudioStream, out AudioComponent? audio) ||
                audio.State != AudioState.Playing)
            {
                continue;
            }

            if (GetPlaybackPosition(audio) + 0.05f < length)
                continue;

            HandleSongFinished(uid, comp);
            // RS14-end
        }
    }

    private void OnComponentShutdown(EntityUid uid, JukeboxComponent component, ComponentShutdown args)
    {
        component.AudioStream = Audio.Stop(component.AudioStream);
        _playbackStates.Remove(uid); // RS14
    }

    // RS14-start
    public override void Shutdown()
    {
        base.Shutdown();
        _protoManager.PrototypesReloaded -= OnPrototypesReloaded;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<JukeboxPrototype>())
            RebuildSongCache();
    }

    private List<JukeboxPrototype> RebuildSongCache()
    {
        _cachedOrderedSongs = _protoManager
            .EnumeratePrototypes<JukeboxPrototype>()
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return _cachedOrderedSongs;
    }
    // RS14-end

    private void DirectSetVisualState(EntityUid uid, JukeboxVisualState state)
    {
        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, state);
    }

    private void TryUpdateVisualState(EntityUid uid, JukeboxComponent? jukeboxComponent = null)
    {
        if (!Resolve(uid, ref jukeboxComponent))
            return;

        var finalState = JukeboxVisualState.On;

        if (!this.IsPowered(uid, EntityManager))
        {
            finalState = JukeboxVisualState.Off;
        }

        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, finalState);
    }
    // RS14-start
    private void StopAndDirty(EntityUid uid, JukeboxComponent component)
    {
        component.AudioStream = Audio.Stop(component.AudioStream);
        Dirty(uid, component);
    }

    private void HandleSongFinished(EntityUid uid, JukeboxComponent component)
    {
        if (component.SelectedSongId == null)
        {
            StopAndDirty(uid, component);
            return;
        }

        if (component.RepeatEnabled)
        {
            StartSong(uid, component, component.SelectedSongId.Value, updateHistory: false);
            return;
        }

        var songId = ResolveNextSong(uid, component, out var fromHistory);
        if (songId == null)
        {
            StopAndDirty(uid, component);
            return;
        }

        StartSong(uid, component, songId.Value, updateHistory: !fromHistory);
    }

    private void SelectOrPlaySong(
        EntityUid uid,
        JukeboxComponent component,
        ProtoId<JukeboxPrototype> songId,
        bool startPlayback,
        bool updateHistory)
    {
        if (startPlayback)
        {
            StartSong(uid, component, songId, updateHistory);
            ShowSelectionVisual(uid, component);
            return;
        }

        SelectSong(uid, component, songId);
    }

    private bool StartSong(
        EntityUid uid,
        JukeboxComponent component,
        ProtoId<JukeboxPrototype> songId,
        bool updateHistory)
    {
        if (!_protoManager.Resolve(songId, out var jukeboxProto))
            return false;

        component.SelectedSongId = songId;
        component.AudioStream = Audio.Stop(component.AudioStream);
        component.AudioStream = Audio.PlayPvs(
            jukeboxProto.Path,
            uid,
            AudioParams.Default
                .WithMaxDistance(10f)
                .WithVolume(JukeboxVolume.ToDb(component.Volume)))?.Entity;

        if (updateHistory && component.AudioStream != null)
            PushHistory(uid, songId);

        Dirty(uid, component);
        return component.AudioStream != null;
    }

    private void SelectSong(EntityUid uid, JukeboxComponent component, ProtoId<JukeboxPrototype> songId)
    {
        component.SelectedSongId = songId;
        component.AudioStream = Audio.Stop(component.AudioStream);
        ShowSelectionVisual(uid, component);
        Dirty(uid, component);
    }

    private void ShowSelectionVisual(EntityUid uid, JukeboxComponent component)
    {
        DirectSetVisualState(uid, JukeboxVisualState.Select);
        component.Selecting = true;
        component.SelectAccumulator = 0f;
    }

    private bool HasActiveStream(EntityUid? audioStream)
    {
        return TryComp(audioStream, out AudioComponent? audio) && audio.State != AudioState.Stopped;
    }

    private ProtoId<JukeboxPrototype>? ResolveNextSong(EntityUid uid, JukeboxComponent component, out bool fromHistory)
    {
        if (TryMoveHistoryIndex(uid, 1, out var historySong))
        {
            fromHistory = true;
            return historySong;
        }

        fromHistory = false;
        return component.ShuffleEnabled
            ? PickShuffledSong(component.SelectedSongId)
            : GetAdjacentSong(component.SelectedSongId, 1);
    }

    private ProtoId<JukeboxPrototype>? ResolvePreviousSong(EntityUid uid, JukeboxComponent component, out bool fromHistory)
    {
        if (TryMoveHistoryIndex(uid, -1, out var historySong))
        {
            fromHistory = true;
            return historySong;
        }

        fromHistory = false;
        return GetAdjacentSong(component.SelectedSongId, -1);
    }

    private ProtoId<JukeboxPrototype>? GetAdjacentSong(ProtoId<JukeboxPrototype>? currentSongId, int direction)
    {
        var orderedSongs = GetOrderedSongs();
        if (orderedSongs.Count == 0)
            return null;

        if (currentSongId == null)
            return direction < 0 ? orderedSongs[^1].ID : orderedSongs[0].ID;

        var currentIndex = orderedSongs.FindIndex(proto => proto.ID == currentSongId.Value);
        if (currentIndex == -1)
            return direction < 0 ? orderedSongs[^1].ID : orderedSongs[0].ID;

        var nextIndex = (currentIndex + direction + orderedSongs.Count) % orderedSongs.Count;
        return orderedSongs[nextIndex].ID;
    }

    private ProtoId<JukeboxPrototype>? PickShuffledSong(ProtoId<JukeboxPrototype>? currentSongId)
    {
        var orderedSongs = GetOrderedSongs();
        if (orderedSongs.Count == 0)
            return null;

        if (orderedSongs.Count == 1)
            return orderedSongs[0].ID;

        var filtered = currentSongId != null
            ? orderedSongs.Where(proto => proto.ID != currentSongId.Value).ToList()
            : orderedSongs;

        return filtered.Count == 0 ? null : _random.Pick(filtered).ID;
    }

    private List<JukeboxPrototype> GetOrderedSongs()
    {
        return _cachedOrderedSongs ?? RebuildSongCache();
    }

    private bool TryGetSongLength(ProtoId<JukeboxPrototype>? songId, out float length)
    {
        length = 0f;
        if (!TryResolveSong(songId, out var proto))
            return false;

        length = (float) Audio.GetAudioLength(Audio.ResolveSound(proto.Path)).TotalSeconds;
        return true;
    }

    private bool TryResolveSong(ProtoId<JukeboxPrototype>? songId, out JukeboxPrototype proto)
    {
        if (songId != null && _protoManager.Resolve(songId.Value, out var resolvedProto))
        {
            proto = resolvedProto;
            return true;
        }

        proto = default!;
        return false;
    }

    private float GetPlaybackPosition(AudioComponent audio)
    {
        if (audio.State == AudioState.Paused)
            return Math.Max(0f, (float) ((audio.PauseTime ?? _timing.CurTime) - audio.AudioStart).TotalSeconds);

        if (audio.State == AudioState.Playing)
            return Math.Max(0f, (float) (_timing.CurTime - audio.AudioStart).TotalSeconds);

        return 0f;
    }

    private PlaybackHistoryState GetPlaybackState(EntityUid uid)
    {
        if (_playbackStates.TryGetValue(uid, out var state))
            return state;

        state = new PlaybackHistoryState();
        _playbackStates[uid] = state;
        return state;
    }

    private void PushHistory(EntityUid uid, ProtoId<JukeboxPrototype> songId)
    {
        var state = GetPlaybackState(uid);

        if (state.HistoryIndex >= 0 &&
            state.HistoryIndex < state.History.Count &&
            state.History[state.HistoryIndex] == songId)
        {
            return;
        }

        if (state.HistoryIndex < state.History.Count - 1)
        {
            state.History.RemoveRange(state.HistoryIndex + 1, state.History.Count - state.HistoryIndex - 1);
        }

        state.History.Add(songId);
        state.HistoryIndex = state.History.Count - 1;
    }

    private bool TryMoveHistoryIndex(EntityUid uid, int direction, out ProtoId<JukeboxPrototype> songId)
    {
        var state = GetPlaybackState(uid);
        var nextIndex = state.HistoryIndex + direction;

        if (nextIndex >= 0 && nextIndex < state.History.Count)
        {
            state.HistoryIndex = nextIndex;
            songId = state.History[state.HistoryIndex];
            return true;
        }

        songId = default;
        return false;
    }
    // RS14-end
}