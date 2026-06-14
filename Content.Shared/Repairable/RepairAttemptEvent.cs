// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

namespace Content.Shared.Repairable;

[ByRefEvent]
public record struct RepairAttemptEvent(EntityUid User, bool Cancelled = false);
