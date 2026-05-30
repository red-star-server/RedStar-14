// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._RedStar.CCVar;

[CVarDefs]
public sealed class RedStarSkillsCVars
{
    /// <summary>
    /// Will skills be applied to players.
    /// </summary>
    public static readonly CVarDef<bool> SkillsEnabled =
        CVarDef.Create("redstar.skills.enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Minimum connected players required for skills to be applied.
    /// </summary>
    public static readonly CVarDef<int> SkillsMinimumPlayers =
        CVarDef.Create("redstar.skills.minimum_players", 10, CVar.SERVERONLY | CVar.ARCHIVE);
}
