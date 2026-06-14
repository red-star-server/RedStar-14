// SPDX-FileCopyrightText: 2022 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;
using Content.Shared.Materials;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mech;

[Serializable, NetSerializable]
public enum MechUiKey : byte
{
    Key
}

/// <summary>
/// Event raised to collect BUI states for each of the mech's equipment items
/// </summary>
public sealed class MechEquipmentUiStateReadyEvent : EntityEventArgs
{
    public Dictionary<NetEntity, BoundUserInterfaceState> States = new();
}

/// <summary>
/// Event raised to relay an equipment ui message
/// </summary>
public sealed class MechEquipmentUiMessageRelayEvent : EntityEventArgs
{
    public MechEquipmentUiMessage Message;

    public MechEquipmentUiMessageRelayEvent(MechEquipmentUiMessage message)
    {
        Message = message;
    }
}

/// <summary>
/// UI event raised to remove a piece of equipment from a mech
/// </summary>
[Serializable, NetSerializable]
public sealed class MechEquipmentRemoveMessage : BoundUserInterfaceMessage
{
    public NetEntity Equipment;

    public MechEquipmentRemoveMessage(NetEntity equipment)
    {
        Equipment = equipment;
    }
}

// RS14-start
/// <summary>
/// UI event raised to remove a passive module from a mech.
/// </summary>
[Serializable, NetSerializable]
public sealed class MechModuleRemoveMessage : BoundUserInterfaceMessage
{
    public NetEntity Module;

    public MechModuleRemoveMessage(NetEntity module)
    {
        Module = module;
    }
}
// RS14-end

/// <summary>
/// base for all mech ui messages
/// </summary>
[Serializable, NetSerializable]
public abstract class MechEquipmentUiMessage : BoundUserInterfaceMessage
{
    public NetEntity Equipment;
}

/// <summary>
/// event raised for the grabber equipment to eject an item from it's storage
/// </summary>
[Serializable, NetSerializable]
public sealed class MechGrabberEjectMessage : MechEquipmentUiMessage
{
    public NetEntity Item;

    public MechGrabberEjectMessage(NetEntity equipment, NetEntity uid)
    {
        Equipment = equipment;
        Item = uid;
    }
}

/// <summary>
/// Event raised for the soundboard equipment to play a sound from its component
/// </summary>
[Serializable, NetSerializable]
public sealed class MechSoundboardPlayMessage : MechEquipmentUiMessage
{
    public int Sound;

    public MechSoundboardPlayMessage(NetEntity equipment, int sound)
    {
        Equipment = equipment;
        Sound = sound;
    }
}

/// <summary>
/// BUI state for mechs that also contains all equipment ui states.
/// </summary>
/// <remarks>
///    ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡠⢐⠤⢃⢰⠐⡄⣀⠀⠀
///    ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⠔⣨⠀⢁⠁⠐⡐⠠⠜⠐⠀
///    ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠔⠐⢀⡁⣀⠔⡌⠡⢀⢐⠁⠀
///    ⠀⠀⠀⠀⢀⠔⠀⡂⡄⠠⢀⡀⠀⣄⡀⠠⠤⠴⡋⠑⡠⠀⠔⠐⢂⠕⢀⡂⠀⠀
///    ⠀⠀⠀⡔⠁⠠⡐⠁⠀⠀⠀⢘⠀⠀⠀⠀⠠⠀⠈⠪⠀⠑⠡⣃⠈⠤⡈⠀⠀⠀
///    ⠀⠀⠨⠀⠄⡒⠀⡂⢈⠀⣀⢌⠀⠀⠁⡈⠀⢆⢀⠀⡀⠉⠒⢆⠑⠀⠀⠀⠀⠀
///    ⠀⠀⠀⡁⠐⠠⠐⡀⠀⢀⣀⠣⡀⠢⡀⠀⢀⡃⠰⠀⠈⠠⢁⠎⠀⠀⠀⠀⠀⠀
///    ⠀⠀⠀⠅⠒⣈⢣⠠⠈⠕⠁⠱⠄⢤⠈⠪⠡⠎⢘⠈⡁⢙⠈⠀⠀⠀⠀⠀⠀⠀
///    ⠀⠀⠀⠃⠀⢡⠀⠧⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⢕⡈⠌⠀⠀⠀⠀⠀⠀⠀⠀
///    ⠀⠀⠀⠀⠀⠀⠈⡀⡀⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⡰⠀⡐⠀⠀⠀⠀⠀⠀⠀⠀
///    ⠀⠀⠀⠀⠀⠀⠀⢈⢂⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠸⠀⡃⠀⠀⠀⠀⠀⠀⠀⠀
///    ⠀⠀⠀⠀⠀⠀⠀⠎⠐⢅⠀⠀⠀⠀⠀⠀⠀⠀⠀⢐⠅⠚⠄⠀⠀⠀⠀⠀⠀⠀
///    ⠀⠀⢈⠩⠈⠀⠐⠁⠀⢀⠀⠄⡂⠒⠐⠀⠆⠁⠰⠠⠀⢅⠈⠐⠄⢁⢡⠀⠀⠀
///    ⠀⠀⢈⡀⠰⡁⠀⠁⠴⠁⠔⠀⠀⠄⠄⡁⠀⠂⠀⠢⠠⠁⠀⠠⠈⠂⠬⠀⠀⠀
///    ⠀⠀⠠⡂⢄⠤⠒⣁⠐⢕⢀⡈⡐⡠⠄⢐⠀⠈⠠⠈⡀⠂⢀⣀⠰⠁⠠⠀⠀
/// trojan horse bui state⠀
/// </remarks>
[Serializable, NetSerializable]
public sealed class MechBoundUiState : BoundUserInterfaceState
{
    public Dictionary<NetEntity, BoundUserInterfaceState> EquipmentStates = new();
    // RS14-start
    public bool HasLock;
    public bool IsLocked;
    public bool DnaLockRegistered;
    public bool DnaLockActive;
    public string? DnaLockOwner;
    public bool CardLockRegistered;
    public bool CardLockActive;
    public string? CardLockOwner;
    // RS14-end
}

[Serializable, NetSerializable]
public sealed class MechGrabberUiState : BoundUserInterfaceState
{
    public List<NetEntity> Contents = new();
    public int MaxContents;
}

/// <summary>
/// List of sound collection ids to be localized and displayed.
/// </summary>
[Serializable, NetSerializable]
public sealed class MechSoundboardUiState : BoundUserInterfaceState
{
    public List<string> Sounds = new();
}

// RS14-start
[Serializable, NetSerializable]
public sealed class MechGeneratorUiState : BoundUserInterfaceState
{
    public float ChargeCurrent;
    public float ChargeMax;
    public bool HasFuel;
    public ProtoId<MaterialPrototype>? FuelName;
    public float FuelAmount;
    public float FuelCapacity;
}

[Serializable, NetSerializable]
public sealed class MechGeneratorEjectFuelMessage : MechEquipmentUiMessage
{
    public MechGeneratorEjectFuelMessage(NetEntity equipment)
    {
        Equipment = equipment;
    }
}

[Serializable, NetSerializable]
public sealed class MechDnaLockRegisterMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MechDnaLockToggleMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MechDnaLockResetMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MechCardLockRegisterMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MechCardLockToggleMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MechCardLockResetMessage : BoundUserInterfaceMessage;
// RS14-end
