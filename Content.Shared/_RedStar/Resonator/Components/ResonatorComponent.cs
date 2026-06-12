// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Resonator.Components;

[RegisterComponent]
public sealed partial class ResonatorComponent : Component
{
    [DataField]
    public EntProtoId FieldPrototype = "ResonanceField";

    [DataField]
    public int MaxFields = 3;

    [DataField]
    public ResonatorDetonationMode Mode = ResonatorDetonationMode.Timer;

    [DataField]
    public TimeSpan TimerDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public int MaxChainTargets = 10;

    [DataField]
    public SoundSpecifier? PlaceSound;

    [DataField]
    public SoundSpecifier? BurstSound;

    [DataField]
    public EntProtoId? BurstEffectPrototype = "EffectGravityPulse";

    [DataField]
    public TimeSpan TileRearmDelay = TimeSpan.FromSeconds(0.35);

    public readonly List<EntityUid> ActiveFields = new();
}

public enum ResonatorDetonationMode : byte
{
    Timer,
    Manual
}
