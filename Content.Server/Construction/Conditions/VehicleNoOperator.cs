// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.Vehicle;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions;

/// <summary>
/// Requires a vehicle to have no active operator.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class VehicleNoOperator : IGraphCondition
{
    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        var vehicle = entityManager.System<VehicleSystem>();
        return !vehicle.HasOperator(uid);
    }

    public bool DoExamine(ExaminedEvent args)
    {
        return false;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield break;
    }
}
