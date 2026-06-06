// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// This is used for a vehicle which can only be operated when a specific key matching a whitelist is inserted.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleSystem))]
public sealed partial class GenericKeyedVehicleComponent : Component
{
    /// <summary>
    /// The ID corresponding to the container where the "key" must be inserted.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string ContainerId;

    /// <summary>
    /// A whitelist determining what qualifies as a valid key for this vehicle.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist KeyWhitelist = new();

    /// <summary>
    /// If true, prevents keys which do not pass the <see cref="KeyWhitelist"/> from being inserted into <see cref="ContainerId"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PreventInvalidInsertion = true;
}
