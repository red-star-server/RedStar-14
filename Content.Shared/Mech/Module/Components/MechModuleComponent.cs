// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Mech.Module.Components;

[RegisterComponent]
public sealed partial class MechModuleComponent : Component
{
    /// <summary>
    /// How long it takes to install this passive module.
    /// </summary>
    [DataField]
    public float InstallDuration = 5f;

    /// <summary>
    /// Space units this module occupies in the mech.
    /// </summary>
    [DataField]
    public int Size = 1;

    /// <summary>
    /// The mech that the module is inside of.
    /// </summary>
    [ViewVariables]
    public EntityUid? ModuleOwner;
}

[Serializable, NetSerializable]
public sealed partial class InsertModuleEvent : SimpleDoAfterEvent;
