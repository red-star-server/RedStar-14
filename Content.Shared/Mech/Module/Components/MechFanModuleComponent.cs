// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Atmos;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Mech.Module.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechFanModuleComponent : Component
{
    /// <summary>
    /// Whether the fan is currently active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsActive;

    /// <summary>
    /// Current fan state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MechFanState State = MechFanState.Off;

    /// <summary>
    /// How much energy the fan consumes per second when active.
    /// </summary>
    [DataField]
    public FixedPoint2 EnergyConsumption = 1.0f;

    /// <summary>
    /// How much gas the fan can process per second when active.
    /// </summary>
    [DataField]
    public float GasProcessingRate = 1f;

    /// <summary>
    /// Whether the attached filter should be active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FilterEnabled = true;

    /// <summary>
    /// Gases that will be filtered during fan operation.
    /// </summary>
    [DataField(required: true)]
    public HashSet<Gas> FilterGases = new();
}

/// <summary>
/// Current operating state of a mech fan module.
/// </summary>
[Serializable, NetSerializable]
public enum MechFanState : byte
{
    /// <summary>
    /// The fan is turned off.
    /// </summary>
    Off,

    /// <summary>
    /// The fan is actively moving gas.
    /// </summary>
    On,

    /// <summary>
    /// The fan is enabled but cannot currently move gas.
    /// </summary>
    Idle,

    /// <summary>
    /// The fan state is unavailable.
    /// </summary>
    Na
}
