using Robust.Shared.Serialization;

namespace Content.Shared._Funkystation.Atmos.Visuals;

[Serializable, NetSerializable]
public enum ElectrolyzerVisualLayers : byte
{
    Main
}

[Serializable, NetSerializable]
public enum ElectrolyzerVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum ElectrolyzerState : byte
{
    Off,
    On,
}
