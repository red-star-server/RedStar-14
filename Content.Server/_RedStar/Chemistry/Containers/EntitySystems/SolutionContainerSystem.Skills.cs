using Content.Shared._RedStar.Skills; // RS14
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.Containers.EntitySystems;

public sealed partial class SolutionContainerSystem
{
    [Dependency] private readonly SharedSkillsSystem _skills = default!;

    private static readonly ProtoId<SkillPrototype> ChemistrySkill = "Chemistry";

    protected override bool CanUseSolutionScanner(EntityUid user)
    {
        return _skills.HasSkill(user, ChemistrySkill);
    }
}
