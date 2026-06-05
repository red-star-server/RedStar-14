// SPDX-FileCopyrightText: 2026 Goob Station Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.Vehicles;

[RegisterComponent]
public sealed partial class ForkliftComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? LiftAction;


    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? UnliftAction;

    [DataField]
    public int ForkliftCapacity = 4;

    [DataField]
    public SoundSpecifier LiftSound;

    [DataField]
    public Robust.Shared.Prototypes.EntProtoId? OverlayPrototype;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ActiveOverlay;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? LiftSoundUid;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? LiftSoundEndTime;
}
