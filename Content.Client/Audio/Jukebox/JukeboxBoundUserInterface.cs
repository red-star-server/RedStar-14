// SPDX-FileCopyrightText: 2024 Kara <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 iNVERTED <alextjorgensen@gmail.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Audio.Jukebox;
using Robust.Client.Audio;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Audio.Jukebox;

public sealed class JukeboxBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    [ViewVariables]
    private JukeboxMenu? _menu;
    private bool _volumeStateCommitted; // RS14

    public JukeboxBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<JukeboxMenu>();
        _menu.SetJukebox(Owner); // RS14
        _menu.OnClose += CommitVolumeState; // RS14

        _menu.OnPlayPressed += args =>
        {
            if (args)
            {
                SendMessage(new JukeboxPlayingMessage());
            }
            else
            {
                SendMessage(new JukeboxPauseMessage());
            }
        };
        // RS14-start
        _menu.OnPreviousPressed += () =>
        {
            SendMessage(new JukeboxPreviousMessage());
        };

        _menu.OnNextPressed += () =>
        {
            SendMessage(new JukeboxNextMessage());
        };

        _menu.OnShuffleToggled += enabled =>
        {
            SendMessage(new JukeboxShuffleMessage(enabled));
        };

        _menu.OnRepeatToggled += enabled =>
        {
            SendMessage(new JukeboxRepeatMessage(enabled));
        };
        // RS14-end
        _menu.OnSongSelected += SelectSong;

        _menu.SetTime += SetTime;
        _menu.SetVolume += SetVolume; // RS14
        PopulateMusic();
        Reload();
    }
    // RS14-start
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_menu != null)
                _menu.OnClose -= CommitVolumeState;
            CommitVolumeState();
        }

        base.Dispose(disposing);
    }
    // RS14-end
    /// <summary>
    /// Reloads the attached menu if it exists.
    /// </summary>
    public void Reload()
    {
        if (_menu == null || !EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox))
            return;

        _menu.SetAudioStream(jukebox.AudioStream);
        // RS14-start
        _menu.SetShuffleEnabled(jukebox.ShuffleEnabled);
        _menu.SetRepeatEnabled(jukebox.RepeatEnabled);
        _menu.SetVolumeSlider(jukebox.Volume);
        // RS14-end

        if (_protoManager.Resolve(jukebox.SelectedSongId, out var songProto)) // RS14
        {
            var length = EntMan.System<AudioSystem>().GetAudioLength(songProto.Path.Path.ToString());
            _menu.SetSelectedSong(jukebox.SelectedSongId, songProto.Name, (float) length.TotalSeconds); // RS14
        }
        else
        {
            _menu.SetSelectedSong(null, string.Empty, 0f); // RS14
        }
    }

    public void PopulateMusic()
    {
        _menu?.Populate(_protoManager.EnumeratePrototypes<JukeboxPrototype>());
    }

    public void SelectSong(ProtoId<JukeboxPrototype> songid)
    {
        SendMessage(new JukeboxSelectedMessage(songid));
    }

    public void SetTime(float time)
    {
        var sentTime = time;

        // You may be wondering, what the fuck is this
        // Well we want to be able to predict the playback slider change, of which there are many ways to do it
        // We can't just use SendPredictedMessage because it will reset every tick and audio updates every frame
        // so it will go BRRRRT
        // Using ping gets us close enough that it SHOULD, MOST OF THE TIME, fall within the 0.1 second tolerance
        // that's still on engine so our playback position never gets corrected.
        if (EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox) &&
            EntMan.TryGetComponent(jukebox.AudioStream, out AudioComponent? audioComp))
        {
            audioComp.PlaybackPosition = time;
        }

        SendMessage(new JukeboxSetTimeMessage(sentTime));
    }
    // RS14-start
    public void SetVolume(float volume)
    {
        SendMessage(new JukeboxSetVolumeMessage(volume));
    }

    public bool TryGetLocalVolumeOverride(out float volume)
    {
        if (_menu != null)
            return _menu.TryGetLocalVolumeOverride(out volume);

        volume = default;
        return false;
    }

    private void CommitVolumeState()
    {
        if (_volumeStateCommitted || _menu == null)
            return;

        _volumeStateCommitted = true;
        _menu.CommitVolumeState();
    }
    // RS14-end
}
