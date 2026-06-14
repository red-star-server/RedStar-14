// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Mech.Components;

/// <summary>
/// Stores mech recharge telemetry and the accumulated recharge rate for the current tick.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(EntitySystem))]
public sealed partial class MechEnergyAccumulatorComponent : Component
{
    /// <summary>
    /// Sum of recharge rates contributed this tick, in charge units per second.
    /// </summary>
    [DataField]
    public float PendingRechargeRate;

    /// <summary>
    /// Current recharge rate displayed by equipment UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Current;

    /// <summary>
    /// Maximum recharge rate displayed by equipment UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Max;
}
