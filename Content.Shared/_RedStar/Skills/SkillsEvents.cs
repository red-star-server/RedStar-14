using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Skills;

public sealed class SkillGrantedEvent
{
    public EntityUid Entity;
    public ProtoId<SkillPrototype> Skill;
}

public sealed class SkillRevokedEvent
{
    public EntityUid Entity;
    public ProtoId<SkillPrototype> Skill;
}

[Serializable, NetSerializable]
public sealed class OpenAdminSkillsWindowEvent : EntityEventArgs
{
    public NetEntity Target;
    public List<ProtoId<SkillPrototype>> Skills = new();

    public OpenAdminSkillsWindowEvent()
    {
    }

    public OpenAdminSkillsWindowEvent(NetEntity target, List<ProtoId<SkillPrototype>> skills)
    {
        Target = target;
        Skills = skills;
    }
}

[Serializable, NetSerializable]
public sealed class AdminToggleSkillEvent : EntityEventArgs
{
    public NetEntity Target;
    public ProtoId<SkillPrototype> Skill;

    public AdminToggleSkillEvent()
    {
    }

    public AdminToggleSkillEvent(NetEntity target, ProtoId<SkillPrototype> skill)
    {
        Target = target;
        Skill = skill;
    }
}

[Serializable, NetSerializable]
public sealed class OpenTeachSkillsWindowEvent : EntityEventArgs
{
    public NetEntity Target;
    public List<ProtoId<SkillPrototype>> TeacherSkills = new();
    public List<ProtoId<SkillPrototype>> TargetSkills = new();

    public OpenTeachSkillsWindowEvent()
    {
    }

    public OpenTeachSkillsWindowEvent(
        NetEntity target,
        List<ProtoId<SkillPrototype>> teacherSkills,
        List<ProtoId<SkillPrototype>> targetSkills)
    {
        Target = target;
        TeacherSkills = teacherSkills;
        TargetSkills = targetSkills;
    }
}

[Serializable, NetSerializable]
public sealed class TeachSkillRequestEvent : EntityEventArgs
{
    public NetEntity Target;
    public ProtoId<SkillPrototype> Skill;

    public TeachSkillRequestEvent()
    {
    }

    public TeachSkillRequestEvent(NetEntity target, ProtoId<SkillPrototype> skill)
    {
        Target = target;
        Skill = skill;
    }
}

[Serializable, NetSerializable]
public sealed class RequestPlayerSkillsEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class UpdatePlayerSkillsEvent : EntityEventArgs
{
    public List<ProtoId<SkillPrototype>> Skills = new();

    public UpdatePlayerSkillsEvent()
    {
    }

    public UpdatePlayerSkillsEvent(List<ProtoId<SkillPrototype>> skills)
    {
        Skills = skills;
    }
}
