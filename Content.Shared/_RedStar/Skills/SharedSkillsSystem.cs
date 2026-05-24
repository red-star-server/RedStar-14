using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Skills;

public abstract class SharedSkillsSystem : EntitySystem
{
    public abstract bool HasSkill(EntityUid entity, ProtoId<SkillPrototype> skill);
}
