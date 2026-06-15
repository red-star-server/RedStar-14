// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Server.Mech.Equipment.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Server.Mech.Equipment.Components;

/// <summary>
/// Charges installed mech equipment from the mech's own battery.
/// </summary>
[RegisterComponent, Access(typeof(MechChargerSystem))]
public sealed partial class MechEquipmentChargerComponent : Component
{
    /// <summary>
    /// The container ID that holds the equipment being charged.
    /// </summary>
    [DataField(required: true)]
    public string SlotId = string.Empty;

    /// <summary>
    /// The charge rate in joules per second.
    /// </summary>
    [DataField]
    public float ChargeRate = 0.5f;

    /// <summary>
    /// Optional whitelist for entities that can be charged.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;
}
