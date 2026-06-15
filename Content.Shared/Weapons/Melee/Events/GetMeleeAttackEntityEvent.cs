// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Allows systems to override the physical entity used as the source of a melee attack.
/// </summary>
// RS14-start
[ByRefEvent]
public record struct GetMeleeAttackEntityEvent
{
    public EntityUid? AttackEntity;

    public bool Handled;
}
// RS14-end
