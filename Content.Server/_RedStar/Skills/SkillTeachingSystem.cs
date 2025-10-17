using System.Linq;
using Content.Shared._RedStar.CCVar;
using Content.Shared._RedStar.Skills;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Skills;

public sealed class SkillTeachingSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;

    private bool _skillsEnabled = true;
    private const float LearningPenaltyPerKnownSkill = 0.15f;
    private const float MaxLearningPenalty = 4f;

    public override void Initialize()
    {
        base.Initialize();

        _skillsEnabled = _cfg.GetCVar(RedStarSkillsCVars.SkillsEnabled);
        Subs.CVar(_cfg, RedStarSkillsCVars.SkillsEnabled, value => _skillsEnabled = value);

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<SkillTeachingDoAfterEvent>(OnTeachingFinished);
        SubscribeNetworkEvent<TeachSkillRequestEvent>(OnTeachSkillRequest);
    }

    public TimeSpan GetLearningTime(EntityUid learner, SkillDifficulty difficulty)
    {
        var baseTime = SkillDifficultyLearningTime.GetLearningTime(difficulty);

        if (!_mind.TryGetMind(learner, out _, out var mind))
            return baseTime;

        var multiplier = MathF.Min(MaxLearningPenalty, 1f + mind.Skills.Count * LearningPenaltyPerKnownSkill);
        return TimeSpan.FromSeconds(baseTime.TotalSeconds * multiplier);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!_skillsEnabled
            || args.User == args.Target
            || !args.CanInteract
            || !TryComp(args.User, out ActorComponent? actor))
        {
            return;
        }

        if (!_mind.TryGetMind(args.User, out _, out var teacherMind)
            || !_mind.TryGetMind(args.Target, out _, out var targetMind)
            || !HasTeachableSkill(teacherMind.Skills, targetMind.Skills))
        {
            return;
        }

        var session = actor.PlayerSession;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("teach-skills-verb"),
            Category = VerbCategory.Interaction,
            Act = () => SendTeachSkillsWindow(session, args.User, args.Target),
            Impact = LogImpact.Low
        });
    }

    private void OnTeachSkillRequest(TeachSkillRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!_skillsEnabled)
            return;

        if (!_prototype.TryIndex(msg.Skill, out var skill))
            return;

        if (args.SenderSession.AttachedEntity is not { } teacher
            || !TryGetEntity(msg.Target, out var target)
            || target.Value == teacher
            || !CanTeachSkill(teacher, target.Value, msg.Skill))
        {
            return;
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            teacher,
            GetLearningTime(target.Value, skill.Difficulty),
            new SkillTeachingDoAfterEvent(GetNetEntity(target.Value), msg.Skill),
            null,
            target: target.Value,
            showTo: target.Value)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            Broadcast = true,
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            NeedHand = false,
            RequireCanInteract = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnTeachingFinished(SkillTeachingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (!_skillsEnabled
            || !TryGetEntity(args.TargetEntity, out var target)
            || !CanTeachSkill(args.User, target.Value, args.Skill))
        {
            return;
        }

        _skills.GrantSkill(target.Value, args.Skill);

        if (TryComp(args.User, out ActorComponent? actor))
            SendTeachSkillsWindow(actor.PlayerSession, args.User, target.Value);
    }

    private void SendTeachSkillsWindow(ICommonSession session, EntityUid teacher, EntityUid target)
    {
        if (!_mind.TryGetMind(teacher, out _, out var teacherMind)
            || !_mind.TryGetMind(target, out _, out var targetMind))
        {
            return;
        }

        RaiseNetworkEvent(new OpenTeachSkillsWindowEvent(
            GetNetEntity(target),
            teacherMind.Skills.ToList(),
            targetMind.Skills.ToList()), session);
    }

    private bool CanTeachSkill(EntityUid teacher, EntityUid target, ProtoId<SkillPrototype> skill)
    {
        if (!_prototype.HasIndex(skill)
            || !_interaction.InRangeUnobstructed(teacher, target)
            || !_mind.TryGetMind(teacher, out _, out var teacherMind)
            || !_mind.TryGetMind(target, out _, out var targetMind))
        {
            return false;
        }

        return teacherMind.Skills.Contains(skill) && !targetMind.Skills.Contains(skill);
    }

    private static bool HasTeachableSkill(
        IReadOnlySet<ProtoId<SkillPrototype>> teacherSkills,
        IReadOnlySet<ProtoId<SkillPrototype>> targetSkills)
    {
        return teacherSkills.Any(skill => !targetSkills.Contains(skill));
    }
}
