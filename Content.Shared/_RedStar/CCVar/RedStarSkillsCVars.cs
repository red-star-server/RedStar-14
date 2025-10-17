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
}
