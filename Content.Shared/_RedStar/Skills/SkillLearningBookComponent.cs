using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Skills;

[RegisterComponent]
public sealed partial class SkillLearningBookComponent : Component
{
    [DataField]
    public ProtoId<SkillPrototype> Skill = "Cooking";

    [DataField]
    public SoundSpecifier ReadSound = new SoundPathSpecifier("/Audio/_Goobstation/Items/handling/paper_use.ogg", AudioParams.Default.WithVolume(-2f));
}

[Serializable, NetSerializable]
public sealed partial class SkillLearningBookDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SkillTeachingDoAfterEvent : DoAfterEvent
{
    public NetEntity TargetEntity;
    public ProtoId<SkillPrototype> Skill;

    public SkillTeachingDoAfterEvent()
    {
    }

    public SkillTeachingDoAfterEvent(NetEntity target, ProtoId<SkillPrototype> skill)
    {
        TargetEntity = target;
        Skill = skill;
    }

    public override DoAfterEvent Clone()
    {
        return new SkillTeachingDoAfterEvent(TargetEntity, Skill);
    }
}
