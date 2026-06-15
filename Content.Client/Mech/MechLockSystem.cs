// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Shared.Mech.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;

namespace Content.Client.Mech;

/// <summary>
/// Client-side mech lock state and predicted access feedback.
/// </summary>
public sealed class MechLockSystem : SharedMechLockSystem
{
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;

    protected override bool TryFindIdCard(EntityUid user, out Entity<IdCardComponent> idCard)
    {
        return _idCard.TryFindIdCard(user, out idCard);
    }
}
