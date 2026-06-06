// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Weapons.Ranged.Events;

// RS14-start
[ByRefEvent]
public record struct GetActiveWeaponEvent
{
    public EntityUid? Weapon;

    public bool Handled;
}
// RS14-end
