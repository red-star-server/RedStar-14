// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Interaction.Events;

/// <summary>
/// Raised on an interaction target to allow a held item interaction even if the
/// held item cannot normally be used from hands.
/// </summary>
// RS14-start
[ByRefEvent]
public record struct UseHeldBypassAttemptEvent(EntityUid User)
{
    public bool Bypass;
}
// RS14-end
