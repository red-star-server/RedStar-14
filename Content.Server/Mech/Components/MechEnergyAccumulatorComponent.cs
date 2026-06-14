// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Server.Mech.Systems;

namespace Content.Server.Mech.Components;

[RegisterComponent, Access(typeof(MechFuelGeneratorSystem), typeof(MechTeslaRelaySystem), typeof(MechGeneratorModuleSystem))]
public sealed partial class MechEnergyAccumulatorComponent : Component
{
    [DataField]
    public float Current;

    [DataField]
    public float Max;
}
