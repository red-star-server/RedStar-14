// SPDX-FileCopyrightText: 2022 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <drsmugleaf@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 keronshb <54602815+keronshb@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 keronshb <keronshb@live.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.DoAfter;
using Content.Shared.Mech.Components;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Mech.Equipment.Components;

/// <summary>
/// A piece of equipment that can be installed into <see cref="MechComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechEquipmentComponent : Component
{
    /// <summary>
    /// How long does it take to install this piece of equipment.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float InstallDuration = 5;

    /// <summary>
    /// Space units this equipment occupies in the mech (for UI display).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Size = 1;

    /// <summary>
    /// The mech that the equipment is inside of.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? EquipmentOwner;

    /// <summary>
    /// If true, this equipment cannot be used outside of a mech.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BlockUseOutsideMech = true;
}

[Serializable, NetSerializable]
public sealed partial class GrabberDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class InsertEquipmentEvent : SimpleDoAfterEvent;
