// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Shared.Administration;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI;

public sealed partial class AdminAnnounceWindow
{
    private void TogglePreview()
    {
        var type = (AdminAnnounceType?) AnnounceMethod.SelectedMetadata;
        if (type != AdminAnnounceType.Station)
        {
            StopPreview();
            UpdateButtons();
            return;
        }

        if (IsStreamPlaying(_previewStream))
            StopPreview();
        else
        {
            var soundPath = AdminAnnounceHelpers.NormalizeSoundPath(SoundPath.Text);
            if (!string.IsNullOrEmpty(soundPath))
                _previewStream = _audio?.PlayGlobal(new SoundPathSpecifier(soundPath), Filter.Local(), false)?.Entity;
        }

        UpdateButtons();
    }

    private void StopPreview()
    {
        if (_previewStream == null)
            return;

        _audio?.Stop(_previewStream);
        _previewStream = null;
    }

    private bool IsStreamPlaying(EntityUid? stream)
    {
        return _audio?.IsPlaying(stream) ?? false;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var type = (AdminAnnounceType?) AnnounceMethod.SelectedMetadata;
        var canPreview = type == AdminAnnounceType.Station;

        if (!canPreview && _previewStream != null)
            StopPreview();

        var isPreviewing = canPreview && IsStreamPlaying(_previewStream);

        if (_previewStream != null && !isPreviewing)
        {
            _previewStream = null;
        }
        
        AnnounceButton.Disabled = string.IsNullOrWhiteSpace(Rope.Collapse(Announcement.TextRope));

        PlayAudio.Disabled = !canPreview;
        PlayAudio.Text = isPreviewing ? "⏹" : "▶";
    }
}
