using Content.Shared._RedStar.Skills;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Skills;

public sealed class SkillLearningBookSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SkillTeachingSystem _skillTeaching = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkillLearningBookComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<SkillLearningBookComponent, SkillLearningBookDoAfterEvent>(OnReadFinished);
    }

    private void OnUseInHand(Entity<SkillLearningBookComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (_skills.HasSkill(args.User, ent.Comp.Skill))
        {
            _audio.PlayPvs(ent.Comp.ReadSound, ent.Owner);
            return;
        }

        if (!_prototype.TryIndex(ent.Comp.Skill, out var skill))
            return;

        _audio.PlayPvs(ent.Comp.ReadSound, ent.Owner);

        var doAfter = new DoAfterArgs(
            EntityManager,
            args.User,
            _skillTeaching.GetLearningTime(args.User, skill.Difficulty),
            new SkillLearningBookDoAfterEvent(),
            ent.Owner,
            used: ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            NeedHand = true,
            RequireCanInteract = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnReadFinished(Entity<SkillLearningBookComponent> ent, ref SkillLearningBookDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        _skills.GrantSkill(args.User, ent.Comp.Skill);
        args.Handled = true;
    }
}
