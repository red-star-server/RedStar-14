// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechCabinAirComponent : Component
{
    /// <summary>
    /// Target pressure for the mech cabin in kPa.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TargetPressure = Atmospherics.OneAtmosphere;

    /// <summary>
    /// Pressure used when metering a single breath, like a tank regulator.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RegulatorPressure = 16f;

    /// <summary>
    /// Internal cabin air mixture separate from any installed gas tank module.
    /// </summary>
    [DataField, AutoNetworkedField]
    public GasMixture Air { get; set; } = new(50f);
}
