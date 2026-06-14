// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Mech.Module.Components;

[Serializable, NetSerializable]
public enum MechGenerationType : byte
{
    TeslaRelay,
    FuelGenerator
}

[RegisterComponent]
public sealed partial class MechGeneratorModuleComponent : Component
{
    [DataField]
    public MechGenerationType GenerationType;

    [DataField]
    public TeslaRelayGeneratorConfig? Tesla;
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class TeslaRelayGeneratorConfig
{
    [DataField]
    public float ChargeRate = 20f;

    [DataField]
    public float Radius = 3f;
}
