// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Mech.Equipment.Components;

/// <summary>
/// A piece of mech equipment that grabs entities and stores them inside a container.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechGrabberComponent : Component
{
    [DataField, AutoNetworkedField]
    public float GrabEnergyDelta = -30;

    [DataField, AutoNetworkedField]
    public float GrabDelay = 2.5f;

    [DataField, AutoNetworkedField]
    public Vector2 DepositOffset = new(0, -1);

    [DataField, AutoNetworkedField]
    public int MaxContents = 10;

    [DataField]
    public SoundSpecifier GrabSound = new SoundPathSpecifier("/Audio/Mecha/sound_mecha_hydraulic.ogg");

    [DataField]
    public EntityWhitelist Blacklist = new();

    public EntityUid? AudioStream;

    [ViewVariables(VVAccess.ReadWrite)]
    public Container ItemContainer = default!;

    [NonSerialized, ViewVariables(VVAccess.ReadOnly)]
    public DoAfterId? DoAfter;
}
