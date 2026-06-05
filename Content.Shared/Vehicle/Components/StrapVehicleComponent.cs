// SPDX-FileCopyrightText: 2025 Fenriz <kastonbag552@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// A <see cref="VehicleComponent"/> whose operator must be buckled to it.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(VehicleSystem))]
public sealed partial class StrapVehicleComponent : Component;
