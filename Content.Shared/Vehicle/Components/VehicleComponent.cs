// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Vehicles are objects that have the behavior of moving when a player "operates" them.
/// The details of when the vehicle can operate and who the operator is are not defined here.
/// This simply contains the baseline behavior of the vehicle itself.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VehicleSystem), Other = AccessPermissions.ReadWriteExecute)] // RS14
public sealed partial class VehicleComponent : Component
{
    /// <summary>
    /// The driver of this vehicle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Operator;

    /// <summary>
    /// Simple whitelist for determining who can operate this vehicle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? OperatorWhitelist;

    /// <summary>
    /// If true, damage to the vehicle will be transferred to the operator.
    /// This damage is modified by <see cref="TransferDamageModifier"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TransferDamage = true;

    /// <summary>
    /// A damage modifier set that adjusts the damage passed from the vehicle to the operator.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageModifierSet? TransferDamageModifier;

    /// <summary>
    /// Whether the operator requires hands to operate this vehicle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresHands = true;

    // RS14-start
    /// <summary>
    /// If true, the vehicle itself cannot move unless it has an operator.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresOperator = true;

    [DataField]
    public EntityUid? HornAction;

    [DataField]
    public EntityUid? SirenAction;

    public bool SirenEnabled;

    public EntityUid? SirenStream;

    /// <summary>
    /// What sound to play when the operator presses the horn action.
    /// </summary>
    [DataField]
    public SoundSpecifier? HornSound;

    /// <summary>
    /// What sound to play when the operator toggles the siren action.
    /// </summary>
    [DataField]
    public SoundSpecifier? SirenSound;

    /// <summary>
    /// Directions where the vehicle sprite should render over mobs.
    /// </summary>
    [DataField]
    public VehicleRenderOver RenderOver = VehicleRenderOver.None;

    /// <summary>
    /// Name of the key container. Used by Goob key-eject behavior.
    /// </summary>
    [DataField]
    public string KeySlot = "key_slot";

    /// <summary>
    /// Prevent key removal while someone is operating the vehicle.
    /// </summary>
    [DataField]
    public bool PreventEjectOfKey = true;

    /// <summary>
    /// Goob breakage gate. Upstream movement still asks VehicleCanRunEvent.
    /// </summary>
    [DataField]
    public bool IsBroken;

    /// <summary>
    /// The entity prototype to spawn as an overlay on the operator.
    /// </summary>
    [DataField]
    public EntProtoId? OverlayPrototype;

    [ViewVariables]
    public EntityUid? ActiveOverlay;
    // RS14-end
}

[Serializable, NetSerializable]
public enum VehicleVisuals : byte
{
    HasOperator,    // The vehicle has a valid operator
    CanRun          // The vehicle can be moved by the operator (turned on :flushed:)
}

// RS14-start
[Serializable, NetSerializable, Flags]
public enum VehicleRenderOver
{
    None = 0,
    North = 1,
    NorthEast = 2,
    East = 4,
    SouthEast = 8,
    South = 16,
    SouthWest = 32,
    West = 64,
    NorthWest = 128,
}
// RS14-end

/// <summary>
/// Event raised on operator when they begin to operate a vehicle
/// Values are configured before this event is raised.
/// </summary>
[ByRefEvent, UsedImplicitly]
public readonly record struct OnVehicleEnteredEvent(Entity<VehicleComponent> Vehicle, EntityUid Operator);

/// <summary>
/// Event raised on operator when they stop operating a vehicle.
/// Values are configured after this event is raised.
/// </summary>
[ByRefEvent, UsedImplicitly]
public readonly record struct OnVehicleExitedEvent(Entity<VehicleComponent> Vehicle, EntityUid Operator);

/// <summary>
/// Event raised on the vehicle after an operator is set.
/// New operator can be null.
/// </summary>
[ByRefEvent, UsedImplicitly]
public readonly record struct VehicleOperatorSetEvent(EntityUid? NewOperator, EntityUid? OldOperator);

/// <summary>
/// Event raised on a vehicle to check if it can run/move around.
/// </summary>
[ByRefEvent, UsedImplicitly]
public readonly record struct VehicleCanRunEvent(Entity<VehicleComponent> Vehicle, bool CanRun = true);

// RS14-start
/// <summary>
/// Event raised on a vehicle after its final can-run state has been resolved.
/// </summary>
[ByRefEvent, UsedImplicitly]
public readonly record struct VehicleCanRunUpdatedEvent(Entity<VehicleComponent> Vehicle, bool CanRun);
// RS14-end
