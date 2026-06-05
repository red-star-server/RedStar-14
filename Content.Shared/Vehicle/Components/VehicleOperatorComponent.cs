// SPDX-FileCopyrightText: 2025 Fenriz <kastonbag552@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Tracking component for handling the operator of a given <see cref="VehicleComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleSystem))]
public sealed partial class VehicleOperatorComponent : Component
{
    /// <summary>
    /// The vehicle we are currently operating.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Vehicle;
}