using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Resonator.Components;

[RegisterComponent]
public sealed partial class ResonanceFieldComponent : Component
{
    public EntityUid? Resonator;

    public EntityUid? Creator;

    public EntityUid? Target;

    public int MaxChainTargets;

    public SoundSpecifier? BurstSound;

    public EntProtoId? BurstEffectPrototype;

    public bool Bursting;

    public EntityUid? GridUid;

    public Vector2i GridIndices;

    public TimeSpan TileRearmDelay;
}
