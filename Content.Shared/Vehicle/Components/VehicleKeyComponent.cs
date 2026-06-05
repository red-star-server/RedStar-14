// SPDX-FileCopyrightText: 2025 Fenriz <kastonbag552@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Tracking component for a key that has been bound to a keyed vehicle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleSystem))]
public sealed partial class VehicleKeyComponent : Component
{
    /// <summary>
    /// The vehicle this key is bound to.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? BoundVehicle;
}
