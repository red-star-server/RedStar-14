// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

namespace Content.Server.Mech.Components;

[RegisterComponent]
public sealed partial class MechEnergyAccumulatorComponent : Component
{
    [DataField]
    public float Current;

    [DataField]
    public float Max;
}
